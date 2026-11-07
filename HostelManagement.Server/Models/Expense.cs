namespace HostelManagement.Server.Models;

public class Expense : ITenantScoped
{
    public Guid Id { get; set; }
    public Guid OwnerOrgId { get; set; }

    public Guid HostelId { get; set; }
    public Hostel? Hostel { get; set; }

    public ExpenseCategory Category { get; set; }
    public decimal Amount { get; set; }
    public DateOnly Date { get; set; }
    public string? Notes { get; set; }
}
