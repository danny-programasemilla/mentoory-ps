using Mentoory.Knowledge.Domain.Enums;
using Mentoory.Shared.Domain.SeedWork;

namespace Mentoory.Knowledge.Domain.Aggregates.KnowledgeStructureTemplate;

public class SubjectTemplate : Entity
{
    private readonly List<ResourceTemplate> _resources = new();

    private SubjectTemplate()
    {
    }

    public Guid ExternalId { get; private set; }
    public string Name { get; private set; } = null!;
    public string? Description { get; private set; }
    public int SortOrder { get; private set; }

    public IReadOnlyCollection<ResourceTemplate> Resources => _resources.AsReadOnly();

    internal static SubjectTemplate Create(string name, string? description, int sortOrder)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        return new SubjectTemplate
        {
            ExternalId = Guid.NewGuid(),
            Name = name,
            Description = description,
            SortOrder = sortOrder,
        };
    }

    internal void UpdateDetails(string name, string? description)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        Name = name;
        Description = description;
    }

    internal void UpdateSortOrder(int sortOrder)
    {
        SortOrder = sortOrder;
    }

    internal ResourceTemplate AddResource(
        string title,
        string? description,
        string url,
        ResourceType type,
        int sortOrder)
    {
        var resource = ResourceTemplate.Create(title, description, url, type, sortOrder);
        _resources.Add(resource);
        return resource;
    }

    internal void UpdateResource(
        Guid resourceExternalId,
        string title,
        string? description,
        string url,
        ResourceType type)
    {
        var resource = _resources.FirstOrDefault(r => r.ExternalId == resourceExternalId)
            ?? throw new InvalidOperationException("Recurso no encontrado.");
        resource.UpdateDetails(title, description, url, type);
    }

    internal void RemoveResource(Guid resourceExternalId)
    {
        var resource = _resources.FirstOrDefault(r => r.ExternalId == resourceExternalId)
            ?? throw new InvalidOperationException("Recurso no encontrado.");
        _resources.Remove(resource);
    }

    internal void ReorderResources(IReadOnlyList<Guid> resourceExternalIdsInOrder)
    {
        for (var i = 0; i < resourceExternalIdsInOrder.Count; i++)
        {
            var resource = _resources.FirstOrDefault(r => r.ExternalId == resourceExternalIdsInOrder[i]);
            resource?.UpdateSortOrder(i + 1);
        }
    }
}
