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
