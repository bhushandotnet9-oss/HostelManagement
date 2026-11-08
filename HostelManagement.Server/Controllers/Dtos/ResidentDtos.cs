using HostelManagement.Server.Models;

namespace HostelManagement.Server.Controllers.Dtos;

public record CreateResidentRequest(string Name, string ContactNumber, string AadhaarNumber, DateOnly MoveInDate);

/// <summary>
/// Terms (rent, deposit, billing cycle) are nullable so the controller can tell
/// "the owner submitted new terms" from "the owner left them alone" - submitting
/// terms for a resident with no assigned cot is rejected, and a plain KYC
/// correction must not have to echo them back.
/// </summary>
public record UpdateResidentRequest(
    string Name,
    string ContactNumber,
    string AadhaarNumber,
    DateOnly MoveInDate,
    decimal? RentAmount,
    decimal? DepositAmount,
    BillingCycle? BillingCycle);

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
