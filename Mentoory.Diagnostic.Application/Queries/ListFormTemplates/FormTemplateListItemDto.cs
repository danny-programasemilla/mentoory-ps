namespace Mentoory.Diagnostic.Application.Queries.ListFormTemplates;

/// <summary>
/// Data transfer object representing a form template in the list view.
/// </summary>
/// <param name="ExternalId">The external GUID identifier for routing.</param>
/// <param name="Name">The template name.</param>
/// <param name="Description">The template description.</param>
/// <param name="SubscriptionTier">The subscription tier this template belongs to.</param>
/// <param name="Version">The template version number.</param>
/// <param name="IsActive">Whether the template is currently active.</param>
/// <param name="CreatedAtUtc">The template creation timestamp.</param>
public sealed record FormTemplateListItemDto(
    Guid ExternalId,
    string Name,
    string? Description,
    string? SubscriptionTier,
    int Version,
    bool IsActive,
    DateTime CreatedAtUtc);
