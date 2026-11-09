using HostelManagement.Server.Billing;
using HostelManagement.Server.Controllers.Dtos;
using HostelManagement.Server.Data;
using HostelManagement.Server.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HostelManagement.Server.Controllers;

[ApiController]
[Authorize]
public class DuesController : ControllerBase
{
    private readonly AppDbContext _db;

    public DuesController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet("api/residents/{residentId:guid}/dues")]
    public async Task<ActionResult<List<DuePeriodResponse>>> ListForResident(Guid residentId)
    {
        var resident = await _db.Residents.SingleOrDefaultAsync(r => r.Id == residentId);
        if (resident is null)
        {
            return NotFound();
        }

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        await DuePeriodMaterializer.MaterializeAsync(_db, resident, today);

        var duePeriods = await _db.DuePeriods
            .Where(d => d.ResidentId == residentId)
            .OrderBy(d => d.DueDate)
            .ToListAsync();

        return Ok(duePeriods.Select(d => ToResponse(d, today)).ToList());
    }

    [HttpPost("api/dues/{duePeriodId:guid}/payments")]
    public async Task<ActionResult<DuePeriodResponse>> LogPayment(Guid duePeriodId, LogPaymentRequest request)
    {
        var duePeriod = await _db.DuePeriods.SingleOrDefaultAsync(d => d.Id == duePeriodId);
        if (duePeriod is null)
        {
            return NotFound();
        }

        _db.Payments.Add(new Payment
        {
            Id = Guid.NewGuid(),
            Type = PaymentType.Rent,
            ResidentId = duePeriod.ResidentId,
            DuePeriodId = duePeriod.Id,
            Amount = request.Amount,
            PaidDate = request.Date,
        });

        duePeriod.AmountPaid += request.Amount;
        duePeriod.IsPaid = duePeriod.AmountPaid >= duePeriod.RentAmount;

        await _db.SaveChangesAsync();

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        return Ok(ToResponse(duePeriod, today));
    }

    private static DuePeriodResponse ToResponse(DuePeriod duePeriod, DateOnly today) => new(
        duePeriod.Id,
        duePeriod.ResidentId,
        duePeriod.DueDate,
        duePeriod.RentAmount,
        duePeriod.AmountPaid,
        duePeriod.IsPaid,
        !duePeriod.IsPaid && duePeriod.DueDate < today);
}
