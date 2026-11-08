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
        return Ok(residents.Select(ToResponse).ToList());
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ResidentResponse>> Get(Guid id)
    {
        var resident = await _db.Residents.SingleOrDefaultAsync(r => r.Id == id);
        if (resident is null)
        {
            return NotFound();
        }

        return Ok(ToResponse(resident));
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

        return CreatedAtAction(nameof(Get), new { id = resident.Id }, ToResponse(resident));
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

        return Ok(ToResponse(resident));
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
        if (await _db.Payments.AnyAsync(p => p.DuePeriod!.ResidentId == id))
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

        return Ok(ToResponse(resident));
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

        return Ok(ToResponse(resident));
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

        return Ok(ToResponse(resident));
    }

    private static ResidentResponse ToResponse(Resident resident) => new(
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
        resident.DepositRefundAmount);
}
