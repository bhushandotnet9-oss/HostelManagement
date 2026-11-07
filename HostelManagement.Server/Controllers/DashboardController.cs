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
[Route("api/dashboard")]
public class DashboardController : ControllerBase
{
    private readonly AppDbContext _db;

    public DashboardController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet("overdue")]
    public async Task<ActionResult<List<OverdueResidentResponse>>> Overdue()
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var residents = await _db.Residents
            .Where(r => r.Status != ResidentStatus.CheckedOut && r.CotId != null)
            .Include(r => r.Cot!.Room!.Hostel)
            .ToListAsync();

        var results = new List<OverdueResidentResponse>();
        foreach (var resident in residents)
        {
            await DuePeriodMaterializer.MaterializeAsync(_db, resident, today);

            var overduePeriods = await _db.DuePeriods
                .Where(d => d.ResidentId == resident.Id && !d.IsPaid && d.DueDate < today)
                .ToListAsync();

            if (overduePeriods.Count == 0)
            {
                continue;
            }

            var amountOwed = overduePeriods.Sum(d => d.RentAmount - d.AmountPaid);
            var daysOverdue = (today.ToDateTime(TimeOnly.MinValue) - overduePeriods.Min(d => d.DueDate).ToDateTime(TimeOnly.MinValue)).Days;

            results.Add(new OverdueResidentResponse(
                resident.Id,
                resident.Name,
                resident.Cot!.Room!.HostelId,
                resident.Cot.Room.Hostel!.Name,
                resident.Cot.Label,
                amountOwed,
                daysOverdue));
        }

        return Ok(results);
    }

    [HttpGet("upcoming-dues")]
    public async Task<ActionResult<List<UpcomingDueResponse>>> UpcomingDues([FromQuery] int days = 7)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var windowEnd = today.AddDays(days);

        var residents = await _db.Residents
            .Where(r => r.Status != ResidentStatus.CheckedOut && r.CotId != null)
            .Include(r => r.Cot!.Room!.Hostel)
            .ToListAsync();

        var results = new List<UpcomingDueResponse>();
        foreach (var resident in residents)
        {
            var cycleMonths = (int)resident.BillingCycle;
            var currentDue = DueDateCalculator.CurrentDueDate(resident.MoveInDate, cycleMonths, today);
            var nextDue = DueDateCalculator.NextDueDate(currentDue, cycleMonths);

            if (nextDue <= windowEnd)
            {
                results.Add(new UpcomingDueResponse(
                    resident.Id,
                    resident.Name,
                    resident.Cot!.Room!.HostelId,
                    resident.Cot.Room.Hostel!.Name,
                    nextDue,
                    resident.RentAmount));
            }
        }

        return Ok(results);
    }

    [HttpGet("occupancy")]
    public async Task<ActionResult<OccupancySummaryResponse>> Occupancy()
    {
        var hostels = await _db.Hostels.ToListAsync();
        var perHostel = new List<HostelOccupancySummary>();
        int combinedTotal = 0, combinedOccupied = 0;

        foreach (var hostel in hostels)
        {
            var statuses = await _db.Cots
                .Where(c => c.Room!.HostelId == hostel.Id)
                .Select(c => c.Status)
                .ToListAsync();

            var total = statuses.Count;
            var occupied = statuses.Count(s => s == CotStatus.Occupied);
            combinedTotal += total;
            combinedOccupied += occupied;

            perHostel.Add(new HostelOccupancySummary(
                hostel.Id,
                hostel.Name,
                total,
                occupied,
                total - occupied,
                total == 0 ? 0 : Math.Round(100.0 * occupied / total, 1)));
        }

        var combined = new HostelOccupancySummary(
            null,
            null,
            combinedTotal,
            combinedOccupied,
            combinedTotal - combinedOccupied,
            combinedTotal == 0 ? 0 : Math.Round(100.0 * combinedOccupied / combinedTotal, 1));

        return Ok(new OccupancySummaryResponse(perHostel, combined));
    }

    [HttpGet("expenses")]
    public async Task<ActionResult<List<HostelExpenseTotals>>> ExpenseTotals([FromQuery] DateOnly from, [FromQuery] DateOnly to)
    {
        var hostels = await _db.Hostels.ToListAsync();
        var results = new List<HostelExpenseTotals>();

        foreach (var hostel in hostels)
        {
            var totals = await _db.Expenses
                .Where(e => e.HostelId == hostel.Id && e.Date >= from && e.Date <= to)
                .GroupBy(e => e.Category)
                .Select(g => new ExpenseCategoryTotal(g.Key, g.Sum(e => e.Amount)))
                .ToListAsync();

            results.Add(new HostelExpenseTotals(hostel.Id, hostel.Name, totals));
        }

        return Ok(results);
    }
}
