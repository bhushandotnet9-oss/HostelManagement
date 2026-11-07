using HostelManagement.Server.Models;

namespace HostelManagement.Server.Controllers.Dtos;

public record CreateExpenseRequest(ExpenseCategory Category, decimal Amount, DateOnly Date, string? Notes);

public record ExpenseResponse(Guid Id, Guid HostelId, ExpenseCategory Category, decimal Amount, DateOnly Date, string? Notes);

public record ExpenseCategoryTotal(ExpenseCategory Category, decimal Total);
