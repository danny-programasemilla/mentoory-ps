using Mentoory.Knowledge.Domain.Aggregates.KnowledgeStructureTemplate;
using Mentoory.Knowledge.Domain.Enums;
using Mentoory.Shared.Domain.SeedWork;

namespace Mentoory.Knowledge.Domain.Aggregates.KnowledgeStructure;

public class Subject : Entity
{
    private readonly List<Resource> _resources = new();

    private Subject()
    {
    }

    public Guid ExternalId { get; private set; }
    public Guid? SourceTemplateSubjectExternalId { get; private set; }
    public string Name { get; private set; } = null!;
    public string? Description { get; private set; }
    public int SortOrder { get; private set; }

    public IReadOnlyCollection<Resource> Resources => _resources.AsReadOnly();

    internal static Subject Create(string name, string? description, int sortOrder)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        return new Subject
        {
            ExternalId = Guid.NewGuid(),
            SourceTemplateSubjectExternalId = null,
            Name = name,
            Description = description,
            SortOrder = sortOrder,
        };
    }

    internal static Subject CloneFromTemplate(SubjectTemplate template)
    {
        ArgumentNullException.ThrowIfNull(template);

        var subject = new Subject
        {
            ExternalId = Guid.NewGuid(),
            SourceTemplateSubjectExternalId = template.ExternalId,
            Name = template.Name,
            Description = template.Description,
            SortOrder = template.SortOrder,
        };

        foreach (var resourceTemplate in template.Resources.OrderBy(r => r.SortOrder))
        {
            subject._resources.Add(Resource.CloneFromTemplate(resourceTemplate));
        }

        return subject;
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

    internal Resource AddResource(
        string title,
        string? description,
        string url,
        ResourceType type,
        int sortOrder)
    {
        var resource = Resource.Create(title, description, url, type, sortOrder);
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

    internal void ApplyPartialSync(SubjectTemplate template, PartialSyncResult result)
    {
        foreach (var tplResource in template.Resources.OrderBy(r => r.SortOrder))
        {
            var cloneResource = _resources.FirstOrDefault(r =>
                r.SourceTemplateResourceExternalId == tplResource.ExternalId);

            if (cloneResource is null)
            {
                var maxSort = _resources.Count > 0 ? _resources.Max(r => r.SortOrder) : 0;
                cloneResource = Resource.CloneFromTemplate(tplResource);
                cloneResource.UpdateSortOrder(maxSort + 1);
                _resources.Add(cloneResource);
                result.ResourcesAdded++;
            }
        }
    }
}
