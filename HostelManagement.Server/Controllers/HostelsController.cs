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
[Route("api/hostels")]
public class HostelsController : ControllerBase
{
    private readonly AppDbContext _db;

    public HostelsController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<ActionResult<List<HostelResponse>>> List()
    {
        var hostels = await _db.Hostels
            .Select(h => new HostelResponse(h.Id, h.Name, h.Location))
            .ToListAsync();
        return Ok(hostels);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<HostelResponse>> Get(Guid id)
    {
        var hostel = await _db.Hostels.SingleOrDefaultAsync(h => h.Id == id);
        if (hostel is null)
        {
            return NotFound();
        }

        return Ok(new HostelResponse(hostel.Id, hostel.Name, hostel.Location));
    }

    [HttpPost]
    public async Task<ActionResult<HostelResponse>> Create(CreateHostelRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name) || string.IsNullOrWhiteSpace(request.Location))
        {
            return BadRequest("Name and Location are required.");
        }

        var hostel = new Hostel
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            Location = request.Location,
        };

        _db.Hostels.Add(hostel);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(Get), new { id = hostel.Id }, new HostelResponse(hostel.Id, hostel.Name, hostel.Location));
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<HostelResponse>> Update(Guid id, UpdateHostelRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name) || string.IsNullOrWhiteSpace(request.Location))
        {
            return BadRequest("Name and Location are required.");
        }

        // The tenant query filter means another org's hostel is simply not here,
        // so this is a 404 rather than a 403 - same as the Get above.
        var hostel = await _db.Hostels.SingleOrDefaultAsync(h => h.Id == id);
        if (hostel is null)
        {
            return NotFound();
        }

        hostel.Name = request.Name;
        hostel.Location = request.Location;
        await _db.SaveChangesAsync();

        return Ok(new HostelResponse(hostel.Id, hostel.Name, hostel.Location));
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var hostel = await _db.Hostels.SingleOrDefaultAsync(h => h.Id == id);
        if (hostel is null)
        {
            return NotFound();
        }

        // Guarded hard delete: refuse while anything still references the hostel
        // rather than cascading rooms, cots, residents and expenses away with it.
        if (await _db.Rooms.AnyAsync(r => r.HostelId == id))
        {
            return Conflict("This hostel still has rooms. Delete them first.");
        }

        if (await _db.Residents.AnyAsync(r => r.HostelId == id))
        {
            return Conflict("This hostel still has residents. Delete them first.");
        }

        if (await _db.Expenses.AnyAsync(e => e.HostelId == id))
        {
            return Conflict("This hostel still has expenses. Delete them first.");
        }

        _db.Hostels.Remove(hostel);
        await _db.SaveChangesAsync();

        return NoContent();
    }

    [HttpGet("{id:guid}/occupancy")]
    public async Task<ActionResult<HostelOccupancyResponse>> Occupancy(Guid id)
    {
        if (!await _db.Hostels.AnyAsync(h => h.Id == id))
        {
            return NotFound();
        }

        var statuses = await _db.Cots
            .Where(c => c.Room!.HostelId == id)
            .Select(c => c.Status)
            .ToListAsync();

        var occupied = statuses.Count(s => s == CotStatus.Occupied);
        return Ok(new HostelOccupancyResponse(statuses.Count, occupied, statuses.Count - occupied));
    }

    [HttpGet("{id:guid}/revenue")]
    public async Task<ActionResult<RevenueSummaryResponse>> Revenue(Guid id, [FromQuery] DateOnly from, [FromQuery] DateOnly to)
    {
        if (!await _db.Hostels.AnyAsync(h => h.Id == id))
        {
            return NotFound();
        }

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var residents = await _db.Residents.Where(r => r.HostelId == id).ToListAsync();

        decimal expected = 0;
        foreach (var resident in residents)
        {
            if (resident.BillingCycle == default)
            {
                continue;
            }

            var cutoff = resident.Status == ResidentStatus.CheckedOut ? resident.CheckoutDate!.Value : today;
            var effectiveTo = cutoff < to ? cutoff : to;
            if (effectiveTo < from)
            {
                continue;
            }

            var dueDatesInRange = DueDateCalculator
                .DueDatesUpTo(resident.MoveInDate, (int)resident.BillingCycle, effectiveTo)
                .Count(d => d >= from);
            expected += dueDatesInRange * resident.RentAmount;
        }

        var collected = await _db.Payments
            .Where(p => p.DuePeriod!.Resident!.HostelId == id && p.PaidDate >= from && p.PaidDate <= to)
            .SumAsync(p => (decimal?)p.Amount) ?? 0;

        return Ok(new RevenueSummaryResponse(expected, collected));
    }
}
