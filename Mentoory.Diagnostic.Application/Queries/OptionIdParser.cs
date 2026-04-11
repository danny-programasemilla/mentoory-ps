namespace Mentoory.Diagnostic.Application.Queries;

internal static class OptionIdParser
{
    internal static IReadOnlyList<long> Parse(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return [];
        }

        return raw.Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(long.Parse)
            .ToList();
    }
}
