using Mentoory.Knowledge.Domain.Enums;
using Mentoory.Shared.Domain.SeedWork;

namespace Mentoory.Knowledge.Domain.Aggregates.KnowledgeStructureTemplate;

public class ResourceTemplate : Entity
{
    private ResourceTemplate()
    {
    }

    public Guid ExternalId { get; private set; }
    public string Title { get; private set; } = null!;
    public string? Description { get; private set; }
    public string Url { get; private set; } = null!;
    public ResourceType ResourceType { get; private set; }
    public int SortOrder { get; private set; }

    internal static ResourceTemplate Create(
        string title,
        string? description,
        string url,
        ResourceType resourceType,
        int sortOrder)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        ArgumentException.ThrowIfNullOrWhiteSpace(url);

        return new ResourceTemplate
        {
            ExternalId = Guid.NewGuid(),
            Title = title,
            Description = description,
            Url = url,
            ResourceType = resourceType,
            SortOrder = sortOrder,
        };
    }

    internal void UpdateDetails(string title, string? description, string url, ResourceType resourceType)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        ArgumentException.ThrowIfNullOrWhiteSpace(url);

        Title = title;
        Description = description;
        Url = url;
        ResourceType = resourceType;
    }

    internal void UpdateSortOrder(int sortOrder)
    {
        SortOrder = sortOrder;
    }
}
