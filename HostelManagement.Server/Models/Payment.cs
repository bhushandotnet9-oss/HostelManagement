namespace HostelManagement.Server.Models;

public class Payment : ITenantScoped
{
    public Guid Id { get; set; }
    public Guid OwnerOrgId { get; set; }

    public PaymentType Type { get; set; }

    // Set on every payment, rent included, even though a rent payment could
    // reach its resident through DuePeriod. A deposit has no due period, so
    // this is the one path that works for both types - it keeps the
    // resident-delete guard and the deposit sums single-table queries instead
    // of a union. Deliberately has no default so a new insert site has to
    // think about it: an unset Guid would silently become Guid.Empty.
    public Guid ResidentId { get; set; }
    public Resident? Resident { get; set; }

    // Null for a Deposit payment: the deposit is owed from move-in, not on the
    // billing schedule, and may be collected before any period exists.
    public Guid? DuePeriodId { get; set; }
    public DuePeriod? DuePeriod { get; set; }

    public decimal Amount { get; set; }
    public DateOnly PaidDate { get; set; }
}
