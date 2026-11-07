namespace HostelManagement.Server.Models;

public enum CotStatus
{
    Vacant,
    Occupied
}

public enum BillingCycle
{
    Monthly = 1,
    Quarterly = 3,
    HalfYearly = 6,
    Yearly = 12
}

public enum ResidentStatus
{
    Active,
    ServingNotice,
    CheckedOut
}

public enum ExpenseCategory
{
    Electricity,
    Wifi,
    Maintenance,
    Staff,
    Other
}
