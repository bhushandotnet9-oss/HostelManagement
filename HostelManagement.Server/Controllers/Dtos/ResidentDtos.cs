using HostelManagement.Server.Models;

namespace HostelManagement.Server.Controllers.Dtos;

public record CreateResidentRequest(string Name, string ContactNumber, string AadhaarNumber, DateOnly MoveInDate);

public record AssignCotRequest(Guid CotId, decimal RentAmount, decimal DepositAmount, BillingCycle BillingCycle, DateOnly MoveInDate);

public record RecordNoticeRequest(DateOnly NoticeDate, int NoticePeriodDays);

public record CheckoutRequest(decimal DepositRefundAmount);

public record ResidentResponse(
    Guid Id,
    string Name,
    string ContactNumber,
    string AadhaarNumber,
    DateOnly MoveInDate,
    ResidentStatus Status,
    Guid? CotId,
    decimal RentAmount,
    decimal DepositAmount,
    BillingCycle? BillingCycle,
    DateOnly? NoticeDate,
    int? NoticePeriodDays,
    DateOnly? CheckoutDate,
    decimal? DepositRefundAmount);
