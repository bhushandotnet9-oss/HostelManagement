namespace HostelManagement.Server.Models;

public class Resident : ITenantScoped
{
    public Guid Id { get; set; }
    public Guid OwnerOrgId { get; set; }

    public string Name { get; set; } = string.Empty;
    public string ContactNumber { get; set; } = string.Empty;
    public string AadhaarNumber { get; set; } = string.Empty;
    public DateOnly MoveInDate { get; set; }

    public ResidentStatus Status { get; set; } = ResidentStatus.Active;

    public Guid? CotId { get; set; }
    public Cot? Cot { get; set; }

    // Set when a resident is assigned to a cot and kept thereafter - unlike
    // CotId, this is not cleared at checkout, so historical reporting (e.g.
    // revenue by hostel) still knows which hostel a checked-out resident
    // belonged to.
    public Guid? HostelId { get; set; }

    public decimal RentAmount { get; set; }
    public decimal DepositAmount { get; set; }
    public BillingCycle BillingCycle { get; set; }

    public DateOnly? NoticeDate { get; set; }
    public int? NoticePeriodDays { get; set; }

    public DateOnly? CheckoutDate { get; set; }
    public decimal? DepositRefundAmount { get; set; }

    public List<DuePeriod> DuePeriods { get; set; } = [];
}
