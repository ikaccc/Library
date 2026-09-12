using FluentValidation;
using Library.Lending.Application.Common;

namespace Library.Lending.Application.Analytics;

internal static class TimeRangeRules
{
    public static IRuleBuilderOptions<T, TimeRange> BeWellOrdered<T>(this IRuleBuilder<T, TimeRange> rule) =>
        rule.Must(range => range is null || range.IsWellOrdered)
            .WithMessage("'to' must be later than 'from'.");
}
