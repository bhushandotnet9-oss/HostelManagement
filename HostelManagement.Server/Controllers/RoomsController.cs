using HostelManagement.Server.Controllers.Dtos;
using HostelManagement.Server.Data;
using HostelManagement.Server.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HostelManagement.Server.Controllers;

[ApiController]
[Authorize]
[Route("api/hostels/{hostelId:guid}/rooms")]
public class RoomsController : ControllerBase
{
    private readonly AppDbContext _db;

    public RoomsController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<ActionResult<List<RoomResponse>>> List(Guid hostelId)
    {
        if (!await _db.Hostels.AnyAsync(h => h.Id == hostelId))
        {
            return NotFound();
        }

        var rooms = await _db.Rooms
            .Where(r => r.HostelId == hostelId)
            .Select(r => new RoomResponse(r.Id, r.HostelId, r.Name))
            .ToListAsync();
        return Ok(rooms);
    }

    [HttpPost]
    public async Task<ActionResult<RoomResponse>> Create(Guid hostelId, CreateRoomRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return BadRequest("Name is required.");
        }

        var hostel = await _db.Hostels.SingleOrDefaultAsync(h => h.Id == hostelId);
        if (hostel is null)
        {
            return NotFound();
        }

        var room = new Room
        {
            Id = Guid.NewGuid(),
            HostelId = hostel.Id,
            Name = request.Name,
        };

        _db.Rooms.Add(room);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(List), new { hostelId }, new RoomResponse(room.Id, room.HostelId, room.Name));
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<RoomResponse>> Update(Guid hostelId, Guid id, UpdateRoomRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return BadRequest("Name is required.");
        }

        // Matching the parent id here keeps a room addressed through the wrong
        // hostel a 404 rather than a silent cross-hostel edit.
        var room = await _db.Rooms.SingleOrDefaultAsync(r => r.Id == id && r.HostelId == hostelId);
        if (room is null)
        {
            return NotFound();
        }

        room.Name = request.Name;
        await _db.SaveChangesAsync();

        return Ok(new RoomResponse(room.Id, room.HostelId, room.Name));
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid hostelId, Guid id)
    {
        var room = await _db.Rooms.SingleOrDefaultAsync(r => r.Id == id && r.HostelId == hostelId);
        if (room is null)
        {
            return NotFound();
        }

        if (await _db.Cots.AnyAsync(c => c.RoomId == id))
        {
            return Conflict("This room still has cots. Delete them first.");
        }

        _db.Rooms.Remove(room);
        await _db.SaveChangesAsync();

        return NoContent();
    }
}
