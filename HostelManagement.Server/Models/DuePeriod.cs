namespace HostelManagement.Server.Models;

public class DuePeriod : ITenantScoped
{
    public Guid Id { get; set; }
    public Guid OwnerOrgId { get; set; }

    public Guid ResidentId { get; set; }
    public Resident? Resident { get; set; }

    public DateOnly DueDate { get; set; }
    public decimal RentAmount { get; set; }
    public decimal AmountPaid { get; set; }
    public bool IsPaid { get; set; }

    public List<Payment> Payments { get; set; } = [];
}
