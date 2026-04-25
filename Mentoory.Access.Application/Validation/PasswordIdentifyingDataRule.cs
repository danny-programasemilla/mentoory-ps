using FluentValidation;

namespace Mentoory.Access.Application.Validation;

public static class PasswordIdentifyingDataRule
{
    public const int MinimumSubstringLength = 4;

    public const string Message =
        "La contraseña no puede contener su correo electrónico ni su número de identificación.";

    public static IRuleBuilderOptions<T, string> MustNotContainIdentifyingData<T>(
        this IRuleBuilder<T, string> builder,
        Func<T, string> emailSelector,
        Func<T, string> nationalIdSelector)
    {
        ArgumentNullException.ThrowIfNull(emailSelector);
        ArgumentNullException.ThrowIfNull(nationalIdSelector);

        return builder
            .Must((command, password) => !Contains(password, emailSelector(command), nationalIdSelector(command)))
            .WithMessage(Message);
    }

    internal static bool Contains(string password, string email, string nationalId)
    {
        if (string.IsNullOrEmpty(password))
        {
            return false;
        }

        email ??= string.Empty;
        nationalId ??= string.Empty;

        var pwLower = password.ToLowerInvariant();
        var emailLower = email.ToLowerInvariant();

        if (emailLower.Length > 0 && pwLower.Contains(emailLower, StringComparison.Ordinal))
        {
            return true;
        }

        var atIndex = emailLower.IndexOf('@', StringComparison.Ordinal);
        var localPart = atIndex > 0 ? emailLower[..atIndex] : emailLower;
        if (localPart.Length >= MinimumSubstringLength
            && pwLower.Contains(localPart, StringComparison.Ordinal))
        {
            return true;
        }

        if (nationalId.Length >= MinimumSubstringLength
            && password.Contains(nationalId, StringComparison.Ordinal))
        {
            return true;
        }

        var stripped = StripNonAlphanumeric(nationalId);
        if (stripped.Length >= MinimumSubstringLength
            && stripped != nationalId
            && password.Contains(stripped, StringComparison.Ordinal))
        {
            return true;
        }

        return false;
    }

    private static string StripNonAlphanumeric(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        Span<char> buffer = stackalloc char[value.Length];
        var length = 0;
        foreach (var ch in value)
        {
            if (char.IsLetterOrDigit(ch))
            {
                buffer[length++] = ch;
            }
        }

        return length == value.Length ? value : new string(buffer[..length]);
    }
}
