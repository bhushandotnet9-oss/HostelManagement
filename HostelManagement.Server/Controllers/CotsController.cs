using HostelManagement.Server.Controllers.Dtos;
using HostelManagement.Server.Data;
using HostelManagement.Server.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HostelManagement.Server.Controllers;

[ApiController]
[Authorize]
[Route("api/rooms/{roomId:guid}/cots")]
public class CotsController : ControllerBase
{
    private readonly AppDbContext _db;

    public CotsController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<ActionResult<List<CotResponse>>> List(Guid roomId)
    {
        if (!await _db.Rooms.AnyAsync(r => r.Id == roomId))
        {
            return NotFound();
        }

        var cots = await _db.Cots
            .Where(c => c.RoomId == roomId)
            .Select(c => new CotResponse(c.Id, c.RoomId, c.Label, c.Status))
            .ToListAsync();
        return Ok(cots);
    }

    [HttpPost]
    public async Task<ActionResult<CotResponse>> Create(Guid roomId, CreateCotRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Label))
        {
            return BadRequest("Label is required.");
        }

        var room = await _db.Rooms.SingleOrDefaultAsync(r => r.Id == roomId);
        if (room is null)
        {
            return NotFound();
        }

        var cot = new Cot
        {
            Id = Guid.NewGuid(),
            RoomId = room.Id,
            Label = request.Label,
            Status = CotStatus.Vacant,
        };

        _db.Cots.Add(cot);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(List), new { roomId }, new CotResponse(cot.Id, cot.RoomId, cot.Label, cot.Status));
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<CotResponse>> Update(Guid roomId, Guid id, UpdateCotRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Label))
        {
            return BadRequest("Label is required.");
        }

        var cot = await _db.Cots.SingleOrDefaultAsync(c => c.Id == id && c.RoomId == roomId);
        if (cot is null)
        {
            return NotFound();
        }

        // Label only - Status is driven by resident assignment and checkout, so
        // it is deliberately not editable here.
        cot.Label = request.Label;
        await _db.SaveChangesAsync();

        return Ok(new CotResponse(cot.Id, cot.RoomId, cot.Label, cot.Status));
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid roomId, Guid id)
    {
        var cot = await _db.Cots.SingleOrDefaultAsync(c => c.Id == id && c.RoomId == roomId);
        if (cot is null)
        {
            return NotFound();
        }

        if (cot.Status == CotStatus.Occupied)
        {
            return Conflict("A resident is assigned to this cot. Check them out first.");
        }

        _db.Cots.Remove(cot);
        await _db.SaveChangesAsync();

        return NoContent();
    }
}
