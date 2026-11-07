namespace HostelManagement.Server.Controllers.Dtos;

public record LogPaymentRequest(decimal Amount, DateOnly Date);

public record DuePeriodResponse(
    Guid Id,
    Guid ResidentId,
    DateOnly DueDate,
    decimal RentAmount,
    decimal AmountPaid,
    bool IsPaid,
    bool IsOverdue);
