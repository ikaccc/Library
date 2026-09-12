using Library.Lending.Application.Common;
using Library.Lending.Grpc.Errors;
using V1 = Library.Lending.Contracts.V1;

namespace Library.Lending.Grpc.Mapping;

internal static class RequestMapping
{
    public static Guid ParseId(string value, string field) =>
        Guid.TryParse(value, out var id) && id != Guid.Empty
            ? id
            : throw GrpcErrors.InvalidField(field, $"'{value}' is not a valid identifier.");

    public static Guid? ParseOptionalId(bool hasValue, string value, string field) =>
        hasValue ? ParseId(value, field) : null;

    public static int PageOrDefault(int page) => page == 0 ? 1 : page;

    public static int PageSizeOrDefault(int pageSize) => pageSize == 0 ? Paging.DefaultPageSize : pageSize;

    public static int TopOrDefault(int top) => top == 0 ? Ranking.DefaultTop : top;

    public static TimeRange ToTimeRange(this V1.TimeRange? range) =>
        range is null
            ? TimeRange.AllTime
            : new TimeRange(range.From?.ToDateTimeOffset(), range.To?.ToDateTimeOffset());

    public static Application.Loans.LoanStatus? ToApplication(this V1.LoanStatus status) => status switch
    {
        V1.LoanStatus.Unspecified => null,
        V1.LoanStatus.Open => Application.Loans.LoanStatus.Open,
        V1.LoanStatus.Overdue => Application.Loans.LoanStatus.Overdue,
        V1.LoanStatus.Returned => Application.Loans.LoanStatus.Returned,
        _ => throw GrpcErrors.InvalidField("status", $"'{status}' is not a known loan status."),
    };

    public static string? OptionalString(bool hasValue, string value) =>
        hasValue && !string.IsNullOrWhiteSpace(value) ? value : null;
}
