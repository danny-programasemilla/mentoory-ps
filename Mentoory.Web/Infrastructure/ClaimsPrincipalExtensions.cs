using System.Security.Claims;

namespace Mentoory.Web.Infrastructure;

public static class ClaimsPrincipalExtensions
{
    public static long GetActiveIncubatorId(this ClaimsPrincipal user)
    {
        var claim = user.FindFirst("ActiveIncubatorId")?.Value;
        return long.TryParse(claim, out var id) ? id : 0;
    }

    public static bool HasValidIncubatorContext(this ClaimsPrincipal user)
    {
        return long.TryParse(user.FindFirst("ActiveIncubatorId")?.Value, out var id) && id > 0;
    }

    public static long? GetActiveProjectId(this ClaimsPrincipal user)
    {
        return long.TryParse(user.FindFirst("ActiveProjectId")?.Value, out var id) ? id : null;
    }

    public static string? GetActiveRole(this ClaimsPrincipal user)
    {
        return user.FindFirst("ActiveRole")?.Value;
    }

    /// <summary>
    /// Whether the user can switch their active context. Defaults to clickable
    /// when the claim is absent (pre-change sessions) so multi-context users are
    /// never wrongly locked into a static card.
    /// </summary>
    public static bool CanSwitchContext(this ClaimsPrincipal user)
    {
        return user.FindFirst("CanSwitchContext")?.Value != "false";
    }

    public static long? GetUserId(this ClaimsPrincipal user)
    {
        return long.TryParse(user.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var id) ? id : null;
    }

    public static long? GetActiveIncubatorIdOrNull(this ClaimsPrincipal user)
    {
        var id = user.GetActiveIncubatorId();
        return id > 0 ? id : null;
    }
}
