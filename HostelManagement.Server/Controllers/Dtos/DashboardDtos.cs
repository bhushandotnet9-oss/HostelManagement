namespace HostelManagement.Server.Controllers.Dtos;

public record OverdueResidentResponse(
    Guid ResidentId,
    string ResidentName,
    Guid HostelId,
    string HostelName,
    string CotLabel,
    decimal AmountOwed,
    int DaysOverdue);

public record UpcomingDueResponse(
    Guid ResidentId,
    string ResidentName,
    Guid HostelId,
    string HostelName,
    DateOnly DueDate,
    decimal AmountDue);

public record HostelOccupancySummary(
    Guid? HostelId,
    string? HostelName,
    int Total,
    int Occupied,
    int Vacant,
    double OccupancyPercent);

public record OccupancySummaryResponse(List<HostelOccupancySummary> PerHostel, HostelOccupancySummary Combined);

public record RevenueSummaryResponse(decimal Expected, decimal Collected);

public record HostelExpenseTotals(Guid HostelId, string HostelName, List<ExpenseCategoryTotal> Totals);
