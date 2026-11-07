namespace HostelManagement.Server.Models;

public class Payment : ITenantScoped
{
    public Guid Id { get; set; }
    public Guid OwnerOrgId { get; set; }

    public Guid DuePeriodId { get; set; }
    public DuePeriod? DuePeriod { get; set; }

    public decimal Amount { get; set; }
    public DateOnly PaidDate { get; set; }
}
