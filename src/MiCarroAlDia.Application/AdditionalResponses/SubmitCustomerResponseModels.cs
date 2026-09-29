using MiCarroAlDia.Domain.Enums;

namespace MiCarroAlDia.Application.AdditionalResponses;

public class SubmitCustomerResponseCommand
{
    public required string Token { get; init; }
    public required string QuoteId { get; init; }
    public required IReadOnlyList<ItemDecisionInput> Decisions { get; init; }
}

public class ItemDecisionInput
{
    public required string QuoteItemId { get; init; }
    public CustomerDecision Decision { get; init; }
    public bool SecurityConfirmed { get; init; }
}

public class SubmitCustomerResponseResult
{
    public bool Success { get; init; }
    public string? ErrorMessage { get; init; }
    public string? QuoteId { get; init; }
    public string? SubmittedAtFormatted { get; init; }
    public decimal TotalAuthorizedAmount { get; init; }
    public string? TotalAuthorizedAmountFormatted { get; init; }
    public decimal TotalProposedAmount { get; init; }
    public string? TotalProposedAmountFormatted { get; init; }

    public static SubmitCustomerResponseResult Failure(string errorMessage) => new()
    {
        Success = false,
        ErrorMessage = errorMessage
    };

    public static SubmitCustomerResponseResult Ok(
        string quoteId,
        string submittedAtFormatted,
        decimal totalAuthorizedAmount,
        string totalAuthorizedAmountFormatted,
        decimal totalProposedAmount,
        string totalProposedAmountFormatted) => new()
    {
        Success = true,
        QuoteId = quoteId,
        SubmittedAtFormatted = submittedAtFormatted,
        TotalAuthorizedAmount = totalAuthorizedAmount,
        TotalAuthorizedAmountFormatted = totalAuthorizedAmountFormatted,
        TotalProposedAmount = totalProposedAmount,
        TotalProposedAmountFormatted = totalProposedAmountFormatted
    };
}
