using Mentoory.Shared.Domain.SeedWork;

namespace Mentoory.Access.Domain.ValueObjects;

public class HashedPassword : ValueObject
{
    public HashedPassword(string algorithm, int iterations, string salt, string hash)
    {
        Algorithm = algorithm;
        Iterations = iterations;
        Salt = salt;
        Hash = hash;
    }

    private HashedPassword()
    {
        Algorithm = null!;
        Salt = null!;
        Hash = null!;
    }

    public string Algorithm { get; private set; }
    public int Iterations { get; private set; }
    public string Salt { get; private set; }
    public string Hash { get; private set; }

    public static HashedPassword Parse(string storedFormat)
    {
        // Format: algorithm$iterations$salt$hash
        var parts = storedFormat.Split('$');
        if (parts.Length != 4)
        {
            throw new FormatException("Invalid hashed password format.");
        }

        return new HashedPassword(parts[0], int.Parse(parts[1]), parts[2], parts[3]);
    }

    public override string ToString() => $"{Algorithm}${Iterations}${Salt}${Hash}";

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Algorithm;
        yield return Iterations;
        yield return Salt;
        yield return Hash;
    }
}
