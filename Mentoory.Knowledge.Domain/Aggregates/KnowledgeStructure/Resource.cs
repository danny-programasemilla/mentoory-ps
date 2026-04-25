using Mentoory.Knowledge.Domain.Aggregates.KnowledgeStructureTemplate;
using Mentoory.Knowledge.Domain.Enums;
using Mentoory.Shared.Domain.SeedWork;

namespace Mentoory.Knowledge.Domain.Aggregates.KnowledgeStructure;

public class Resource : Entity
{
    private Resource()
    {
    }

    public Guid ExternalId { get; private set; }
    public Guid? SourceTemplateResourceExternalId { get; private set; }
    public string Title { get; private set; } = null!;
    public string? Description { get; private set; }
    public string Url { get; private set; } = null!;
    public ResourceType ResourceType { get; private set; }
    public int SortOrder { get; private set; }

    internal static Resource Create(
        string title,
        string? description,
        string url,
        ResourceType resourceType,
        int sortOrder)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        ArgumentException.ThrowIfNullOrWhiteSpace(url);

        return new Resource
        {
            ExternalId = Guid.NewGuid(),
            SourceTemplateResourceExternalId = null,
            Title = title,
            Description = description,
            Url = url,
            ResourceType = resourceType,
            SortOrder = sortOrder,
        };
    }

    internal static Resource CloneFromTemplate(ResourceTemplate template)
    {
        ArgumentNullException.ThrowIfNull(template);

        return new Resource
        {
            ExternalId = Guid.NewGuid(),
            SourceTemplateResourceExternalId = template.ExternalId,
            Title = template.Title,
            Description = template.Description,
            Url = template.Url,
            ResourceType = template.ResourceType,
            SortOrder = template.SortOrder,
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
