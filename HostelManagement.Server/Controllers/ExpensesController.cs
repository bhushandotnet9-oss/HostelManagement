using HostelManagement.Server.Controllers.Dtos;
using HostelManagement.Server.Data;
using HostelManagement.Server.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HostelManagement.Server.Controllers;

[ApiController]
[Authorize]
[Route("api/hostels/{hostelId:guid}/expenses")]
public class ExpensesController : ControllerBase
{
    private readonly AppDbContext _db;

    public ExpensesController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<ActionResult<List<ExpenseResponse>>> List(Guid hostelId)
    {
        if (!await _db.Hostels.AnyAsync(h => h.Id == hostelId))
        {
            return NotFound();
        }

        var expenses = await _db.Expenses
            .Where(e => e.HostelId == hostelId)
            .Select(e => new ExpenseResponse(e.Id, e.HostelId, e.Category, e.Amount, e.Date, e.Notes))
            .ToListAsync();
        return Ok(expenses);
    }

    [HttpPost]
    public async Task<ActionResult<ExpenseResponse>> Create(Guid hostelId, CreateExpenseRequest request)
    {
        if (request.Amount <= 0)
        {
            return BadRequest("Amount must be positive.");
        }

        var hostel = await _db.Hostels.SingleOrDefaultAsync(h => h.Id == hostelId);
        if (hostel is null)
        {
            return NotFound();
        }

        var expense = new Expense
        {
            Id = Guid.NewGuid(),
            HostelId = hostel.Id,
            Category = request.Category,
            Amount = request.Amount,
            Date = request.Date,
            Notes = request.Notes,
        };

        _db.Expenses.Add(expense);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(List), new { hostelId },
            new ExpenseResponse(expense.Id, expense.HostelId, expense.Category, expense.Amount, expense.Date, expense.Notes));
    }

    [HttpGet("breakdown")]
    public async Task<ActionResult<List<ExpenseCategoryTotal>>> Breakdown(Guid hostelId, DateOnly from, DateOnly to)
    {
        if (!await _db.Hostels.AnyAsync(h => h.Id == hostelId))
        {
            return NotFound();
        }

        var totals = await _db.Expenses
            .Where(e => e.HostelId == hostelId && e.Date >= from && e.Date <= to)
            .GroupBy(e => e.Category)
            .Select(g => new ExpenseCategoryTotal(g.Key, g.Sum(e => e.Amount)))
            .ToListAsync();

        return Ok(totals);
    }
}
