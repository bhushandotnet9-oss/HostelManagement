using HostelManagement.Server.Controllers.Dtos;
using HostelManagement.Server.Data;
using HostelManagement.Server.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HostelManagement.Server.Controllers;

[ApiController]
[Authorize]
[Route("api/residents")]
public class ResidentsController : ControllerBase
{
    private readonly AppDbContext _db;

    public ResidentsController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<ActionResult<List<ResidentResponse>>> List()
    {
        var residents = await _db.Residents.ToListAsync();

        // One grouped query for every resident's deposit total, not one per
        // resident - this endpoint returns the whole org's residents.
        var depositTotals = await _db.Payments
            .Where(p => p.Type == PaymentType.Deposit)
            .GroupBy(p => p.ResidentId)
            .Select(g => new { ResidentId = g.Key, Collected = g.Sum(p => p.Amount) })
            .ToDictionaryAsync(x => x.ResidentId, x => x.Collected);

        return Ok(residents
            .Select(r => ToResponse(r, depositTotals.GetValueOrDefault(r.Id)))
            .ToList());
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ResidentResponse>> Get(Guid id)
    {
        var resident = await _db.Residents.SingleOrDefaultAsync(r => r.Id == id);
        if (resident is null)
        {
            return NotFound();
        }

        return Ok(ToResponse(resident, await DepositCollectedAsync(resident.Id)));
    }

    [HttpPost]
    public async Task<ActionResult<ResidentResponse>> Create(CreateResidentRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return BadRequest("Name is required.");
        }

        if (string.IsNullOrWhiteSpace(request.ContactNumber))
        {
            return BadRequest("ContactNumber is required.");
        }

        if (string.IsNullOrWhiteSpace(request.AadhaarNumber))
        {
            return BadRequest("AadhaarNumber is required.");
        }

        var resident = new Resident
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            ContactNumber = request.ContactNumber,
            AadhaarNumber = request.AadhaarNumber,
            MoveInDate = request.MoveInDate,
            Status = ResidentStatus.Active,
        };

        _db.Residents.Add(resident);
        await _db.SaveChangesAsync();

        // A resident created a moment ago cannot have a payment, so there is
        // nothing to sum.
        return CreatedAtAction(nameof(Get), new { id = resident.Id }, ToResponse(resident, 0m));
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ResidentResponse>> Update(Guid id, UpdateResidentRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return BadRequest("Name is required.");
        }

        if (string.IsNullOrWhiteSpace(request.ContactNumber))
        {
            return BadRequest("ContactNumber is required.");
        }

        if (string.IsNullOrWhiteSpace(request.AadhaarNumber))
        {
            return BadRequest("AadhaarNumber is required.");
        }

        var resident = await _db.Residents.SingleOrDefaultAsync(r => r.Id == id);
        if (resident is null)
        {
            return NotFound();
        }

        var termsSubmitted = request.RentAmount.HasValue
            || request.DepositAmount.HasValue
            || request.BillingCycle.HasValue;
        if (termsSubmitted && !resident.CotId.HasValue)
        {
            return BadRequest("Assign the resident to a cot before setting rent, deposit, or billing cycle.");
        }

        // The move-in date anchors the whole billing schedule (DueDateCalculator
        // derives every due date from it), so once any period exists, shifting it
        // would either mint extra back-dated periods or orphan the existing ones.
        if (request.MoveInDate != resident.MoveInDate
            && await _db.DuePeriods.AnyAsync(d => d.ResidentId == id))
        {
            return Conflict("Billing has already started for this resident, so the move-in date can no longer be changed.");
        }

        resident.Name = request.Name;
        resident.ContactNumber = request.ContactNumber;
        resident.AadhaarNumber = request.AadhaarNumber;
        resident.MoveInDate = request.MoveInDate;

        // Deliberately no DuePeriod rewrite here. The materializer only ever
        // creates rows up to today, so every stored period is already due and
        // must keep its original amount; the next materialization reads these
        // new terms straight off the resident. See design.md - Decisions.
        if (request.RentAmount.HasValue)
        {
            resident.RentAmount = request.RentAmount.Value;
        }

        if (request.DepositAmount.HasValue)
        {
            resident.DepositAmount = request.DepositAmount.Value;
        }

        if (request.BillingCycle.HasValue)
        {
            resident.BillingCycle = request.BillingCycle.Value;
        }

        await _db.SaveChangesAsync();

        return Ok(ToResponse(resident, await DepositCollectedAsync(resident.Id)));
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var resident = await _db.Residents.SingleOrDefaultAsync(r => r.Id == id);
        if (resident is null)
        {
            return NotFound();
        }

        // Payments are money that changed hands, so they block the delete. Due
        // periods do not: they are derived from the resident's terms and would
        // make a resident undeletable the moment anyone opened their dues.
        //
        // Matched on Payment.ResidentId, not through DuePeriod: a deposit payment
        // has no due period, so the old `p.DuePeriod!.ResidentId == id` form would
        // quietly stop seeing it and let a resident with a collected deposit be
        // deleted.
        if (await _db.Payments.AnyAsync(p => p.ResidentId == id))
        {
            return Conflict("This resident has recorded payments and cannot be deleted.");
        }

        var duePeriods = await _db.DuePeriods.Where(d => d.ResidentId == id).ToListAsync();
        _db.DuePeriods.RemoveRange(duePeriods);

        if (resident.CotId.HasValue)
        {
            var cot = await _db.Cots.SingleOrDefaultAsync(c => c.Id == resident.CotId.Value);
            if (cot is not null)
            {
                cot.Status = CotStatus.Vacant;
            }

            resident.CotId = null;
        }

        _db.Residents.Remove(resident);
        await _db.SaveChangesAsync();

        return NoContent();
    }

    [HttpPost("{id:guid}/assign-cot")]
    public async Task<ActionResult<ResidentResponse>> AssignCot(Guid id, AssignCotRequest request)
    {
        var resident = await _db.Residents.SingleOrDefaultAsync(r => r.Id == id);
        if (resident is null)
        {
            return NotFound();
        }

        var cot = await _db.Cots.Include(c => c.Room).SingleOrDefaultAsync(c => c.Id == request.CotId);
        if (cot is null)
        {
            return NotFound();
        }

        if (cot.Status == CotStatus.Occupied)
        {
            return Conflict("This cot is already occupied.");
        }

        resident.CotId = cot.Id;
        resident.HostelId = cot.Room!.HostelId;
        resident.RentAmount = request.RentAmount;
        resident.DepositAmount = request.DepositAmount;
        resident.BillingCycle = request.BillingCycle;
        resident.MoveInDate = request.MoveInDate;
        cot.Status = CotStatus.Occupied;

        await _db.SaveChangesAsync();

        return Ok(ToResponse(resident, await DepositCollectedAsync(resident.Id)));
    }

    [HttpPost("{id:guid}/notice")]
    public async Task<ActionResult<ResidentResponse>> RecordNotice(Guid id, RecordNoticeRequest request)
    {
        var resident = await _db.Residents.SingleOrDefaultAsync(r => r.Id == id);
        if (resident is null)
        {
            return NotFound();
        }

        resident.NoticeDate = request.NoticeDate;
        resident.NoticePeriodDays = request.NoticePeriodDays;
        resident.Status = ResidentStatus.ServingNotice;

        await _db.SaveChangesAsync();

        return Ok(ToResponse(resident, await DepositCollectedAsync(resident.Id)));
    }

    [HttpGet("{id:guid}/deposit-payments")]
    public async Task<ActionResult<List<DepositPaymentResponse>>> ListDepositPayments(Guid id)
    {
        var resident = await _db.Residents.SingleOrDefaultAsync(r => r.Id == id);
        if (resident is null)
        {
            return NotFound();
        }

        var payments = await DepositPaymentsQuery(id)
            .OrderBy(p => p.PaidDate)
            .ToListAsync();

        return Ok(payments
            .Select(p => new DepositPaymentResponse(p.Id, p.ResidentId, p.Amount, p.PaidDate))
            .ToList());
    }

    [HttpPost("{id:guid}/deposit-payments")]
    public async Task<ActionResult<DepositPaymentResponse>> LogDepositPayment(Guid id, LogDepositPaymentRequest request)
    {
        var resident = await _db.Residents.SingleOrDefaultAsync(r => r.Id == id);
        if (resident is null)
        {
            return NotFound();
        }

        if (request.Amount <= 0)
        {
            return BadRequest("Amount must be greater than zero.");
        }

        // Written against DepositAmount rather than CotId so it stays correct if
        // terms ever become settable before assignment. Today the two coincide:
        // Update refuses terms for a resident with no cot, so an unassigned
        // resident's agreed deposit is always zero.
        if (resident.DepositAmount <= 0)
        {
            return Conflict("No deposit is agreed for this resident, so a deposit payment cannot be recorded.");
        }

        // Read-then-write: two concurrent requests could in principle both pass
        // this check. Single-owner data entry has no such path, and the worst
        // case is an over-collected deposit the owner can see rather than
        // corrupted data - see design.md.
        var collected = await DepositPaymentsQuery(id).SumAsync(p => p.Amount);
        if (collected + request.Amount > resident.DepositAmount)
        {
            var outstanding = resident.DepositAmount - collected;
            return Conflict($"That exceeds the agreed deposit; only {outstanding} remains outstanding.");
        }

        var payment = new Payment
        {
            Id = Guid.NewGuid(),
            Type = PaymentType.Deposit,
            ResidentId = resident.Id,
            // No due period: the deposit is owed from move-in, not on the
            // billing schedule, and may be taken before the first period exists.
            DuePeriodId = null,
            Amount = request.Amount,
            PaidDate = request.Date,
        };

        _db.Payments.Add(payment);
        await _db.SaveChangesAsync();

        return Ok(new DepositPaymentResponse(payment.Id, payment.ResidentId, payment.Amount, payment.PaidDate));
    }

    [HttpPost("{id:guid}/checkout")]
    public async Task<ActionResult<ResidentResponse>> Checkout(Guid id, CheckoutRequest request)
    {
        var resident = await _db.Residents.SingleOrDefaultAsync(r => r.Id == id);
        if (resident is null)
        {
            return NotFound();
        }

        if (resident.NoticeDate is null)
        {
            return BadRequest("Notice must be recorded before checkout.");
        }

        var cot = resident.CotId.HasValue
            ? await _db.Cots.SingleOrDefaultAsync(c => c.Id == resident.CotId.Value)
            : null;

        resident.Status = ResidentStatus.CheckedOut;
        resident.CheckoutDate = DateOnly.FromDateTime(DateTime.UtcNow);
        resident.DepositRefundAmount = request.DepositRefundAmount;
        resident.CotId = null;

        if (cot is not null)
        {
            cot.Status = CotStatus.Vacant;
        }

        await _db.SaveChangesAsync();

        return Ok(ToResponse(resident, await DepositCollectedAsync(resident.Id)));
    }

    // The tenant query filter on Payments already scopes this to the current
    // org, so a resident id from another org yields no rows even if guessed.
    private IQueryable<Payment> DepositPaymentsQuery(Guid residentId) =>
        _db.Payments.Where(p => p.ResidentId == residentId && p.Type == PaymentType.Deposit);

    private Task<decimal> DepositCollectedAsync(Guid residentId) =>
        DepositPaymentsQuery(residentId).SumAsync(p => p.Amount);

    private static ResidentResponse ToResponse(Resident resident, decimal depositCollected) => new(
        resident.Id,
        resident.Name,
        resident.ContactNumber,
        resident.AadhaarNumber,
        resident.MoveInDate,
        resident.Status,
        resident.CotId,
        resident.RentAmount,
        resident.DepositAmount,
        resident.CotId.HasValue ? resident.BillingCycle : null,
        resident.NoticeDate,
        resident.NoticePeriodDays,
        resident.CheckoutDate,
        resident.DepositRefundAmount,
        depositCollected,
        // Floored because Update can lower the agreed deposit below what was
        // already collected; "outstanding" must keep meaning "still to collect".
        Math.Max(0m, resident.DepositAmount - depositCollected));
}
