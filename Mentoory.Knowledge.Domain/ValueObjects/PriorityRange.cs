namespace Mentoory.Knowledge.Domain.ValueObjects;

/// <summary>
/// Value object representing an inclusive [Min, Max] decimal range used for topic priority bands.
/// Stored on the parent entity as two nullable decimal columns; a band is "configured" iff both columns are non-null.
/// </summary>
public sealed record PriorityRange
{
    private PriorityRange(decimal min, decimal max)
    {
        Min = min;
        Max = max;
    }

    public decimal Min { get; }

    public decimal Max { get; }

    /// <summary>
    /// Creates a new <see cref="PriorityRange"/> ensuring <paramref name="min"/> &lt;= <paramref name="max"/>.
    /// </summary>
    /// <exception cref="ArgumentException">Thrown when <paramref name="min"/> is greater than <paramref name="max"/>.</exception>
    public static PriorityRange Create(decimal min, decimal max)
    {
        if (min > max)
        {
            throw new ArgumentException("Min must be less than or equal to Max.", nameof(min));
        }

        return new PriorityRange(min, max);
    }

    /// <summary>
    /// Returns true when <paramref name="score"/> is within the inclusive bounds [Min, Max].
    /// </summary>
    public bool Contains(decimal score) => score >= Min && score <= Max;

    /// <summary>
    /// Returns true when this range shares any point with <paramref name="other"/>.
    /// Touching endpoints count as overlap (e.g. [0,10] overlaps [10,20]).
    /// </summary>
    public bool OverlapsWith(PriorityRange? other)
        => other is not null && Min <= other.Max && other.Min <= Max;
}
