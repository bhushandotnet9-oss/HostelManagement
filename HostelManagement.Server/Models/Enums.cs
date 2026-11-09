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

// Rent is deliberately 0 so existing payment rows, which are all rent, fall on
// it when the column is added with a zero default - see the backfill in
// Data/Migrations/..._AddPaymentTypeAndDepositPayments.cs.
public enum PaymentType
{
    Rent,
    Deposit
}

public enum ExpenseCategory
{
    Electricity,
    Wifi,
    Maintenance,
    Staff,
    Other
}
