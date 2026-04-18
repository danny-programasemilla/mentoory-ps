using FluentValidation;

namespace Mentoory.Shared.Application.Queries.Audit;

/// <summary>
/// Validates pagination bounds and the freeform filter dictionary on
/// <see cref="GetAuditLogPagedQuery"/>. Semantic filter values that fail validation
/// (unknown outcome, inverted date range, unparseable dates) are reported so the
/// client cannot trigger a full-table scan via malformed input.
/// </summary>
public sealed class GetAuditLogPagedQueryValidator : AbstractValidator<GetAuditLogPagedQuery>
{
    private static readonly HashSet<string> AllowedOutcomes =
        new(StringComparer.OrdinalIgnoreCase) { "Success", "Failure" };

    public GetAuditLogPagedQueryValidator()
    {
        RuleFor(q => q.Request.Start)
            .GreaterThanOrEqualTo(0)
            .WithMessage("Start debe ser mayor o igual que 0.");

        RuleFor(q => q.Request.Length)
            .InclusiveBetween(1, 100)
            .WithMessage("Length debe estar entre 1 y 100.");

        RuleFor(q => q.Request.Filters)
            .Custom((filters, context) =>
            {
                if (filters is null || filters.Count == 0)
                {
                    return;
                }

                if (filters.TryGetValue("outcome", out var outcome)
                    && !string.IsNullOrWhiteSpace(outcome)
                    && !AllowedOutcomes.Contains(outcome))
                {
                    context.AddFailure("Filters.Outcome", "Outcome debe ser 'Success' o 'Failure'.");
                }

                DateTime? from = TryParseUtc(filters, "fromUtc", context, "Filters.FromUtc");
                DateTime? to = TryParseUtc(filters, "toUtc", context, "Filters.ToUtc");

                if (from is not null && to is not null && from > to)
                {
                    context.AddFailure("Filters.FromUtc", "FromUtc debe ser menor o igual a ToUtc.");
                }
            });
    }

    private static DateTime? TryParseUtc(
        Dictionary<string, string> filters,
        string key,
        FluentValidation.ValidationContext<GetAuditLogPagedQuery> context,
        string propertyName)
    {
        if (!filters.TryGetValue(key, out var value) || string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        if (DateTime.TryParse(
                value,
                System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.AssumeUniversal | System.Globalization.DateTimeStyles.AdjustToUniversal,
                out var parsed))
        {
            return parsed;
        }

        context.AddFailure(propertyName, $"{key} no es una fecha válida.");
        return null;
    }
}
