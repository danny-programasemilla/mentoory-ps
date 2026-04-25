using Mentoory.Knowledge.Domain.Enums;
using Mentoory.Knowledge.Domain.ValueObjects;
using Mentoory.Shared.Domain.SeedWork;

namespace Mentoory.Knowledge.Domain.Aggregates.KnowledgeStructureTemplate;

public class TopicTemplate : Entity
{
    private readonly List<SubjectTemplate> _subjects = new();

    private TopicTemplate()
    {
    }

    public Guid ExternalId { get; private set; }
    public string Name { get; private set; } = null!;
    public string? Description { get; private set; }
    public int SortOrder { get; private set; }

    public decimal? HighRangeMin { get; private set; }
    public decimal? HighRangeMax { get; private set; }
    public decimal? MediumRangeMin { get; private set; }
    public decimal? MediumRangeMax { get; private set; }
    public decimal? LowRangeMin { get; private set; }
    public decimal? LowRangeMax { get; private set; }

    public PriorityRange? HighRange =>
        HighRangeMin.HasValue && HighRangeMax.HasValue ? PriorityRange.Create(HighRangeMin.Value, HighRangeMax.Value) : null;

    public PriorityRange? MediumRange =>
        MediumRangeMin.HasValue && MediumRangeMax.HasValue ? PriorityRange.Create(MediumRangeMin.Value, MediumRangeMax.Value) : null;

    public PriorityRange? LowRange =>
        LowRangeMin.HasValue && LowRangeMax.HasValue ? PriorityRange.Create(LowRangeMin.Value, LowRangeMax.Value) : null;

    public IReadOnlyCollection<SubjectTemplate> Subjects => _subjects.AsReadOnly();

    internal static TopicTemplate Create(string name, string? description, int sortOrder)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        return new TopicTemplate
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

    internal void UpdatePriorityRanges(PriorityRange? high, PriorityRange? medium, PriorityRange? low)
    {
        // Defensive invariant guard: no two configured bands may overlap.
        // The application-layer validator also enforces this; the domain layers it for safety.
        if (high is not null && medium is not null && high.OverlapsWith(medium))
        {
            throw new InvalidOperationException("Los rangos de prioridad se solapan.");
        }

        if (high is not null && low is not null && high.OverlapsWith(low))
        {
            throw new InvalidOperationException("Los rangos de prioridad se solapan.");
        }

        if (medium is not null && low is not null && medium.OverlapsWith(low))
        {
            throw new InvalidOperationException("Los rangos de prioridad se solapan.");
        }

        HighRangeMin = high?.Min;
        HighRangeMax = high?.Max;
        MediumRangeMin = medium?.Min;
        MediumRangeMax = medium?.Max;
        LowRangeMin = low?.Min;
        LowRangeMax = low?.Max;
    }

    internal SubjectTemplate AddSubject(string name, string? description, int sortOrder)
    {
        var subject = SubjectTemplate.Create(name, description, sortOrder);
        _subjects.Add(subject);
        return subject;
    }

    internal void UpdateSubject(Guid subjectExternalId, string name, string? description)
    {
        var subject = RequireSubject(subjectExternalId);
        subject.UpdateDetails(name, description);
    }

    internal void RemoveSubject(Guid subjectExternalId)
    {
        var subject = RequireSubject(subjectExternalId);
        _subjects.Remove(subject);
    }

    internal void ReorderSubjects(IReadOnlyList<Guid> subjectExternalIdsInOrder)
    {
        for (var i = 0; i < subjectExternalIdsInOrder.Count; i++)
        {
            var subject = _subjects.FirstOrDefault(s => s.ExternalId == subjectExternalIdsInOrder[i]);
            subject?.UpdateSortOrder(i + 1);
        }
    }

    internal ResourceTemplate AddResource(
        Guid subjectExternalId,
        string title,
        string? description,
        string url,
        ResourceType type,
        int sortOrder)
    {
        var subject = RequireSubject(subjectExternalId);
        return subject.AddResource(title, description, url, type, sortOrder);
    }

    internal void UpdateResource(
        Guid subjectExternalId,
        Guid resourceExternalId,
        string title,
        string? description,
        string url,
        ResourceType type)
    {
        var subject = RequireSubject(subjectExternalId);
        subject.UpdateResource(resourceExternalId, title, description, url, type);
    }

    internal void RemoveResource(Guid subjectExternalId, Guid resourceExternalId)
    {
        var subject = RequireSubject(subjectExternalId);
        subject.RemoveResource(resourceExternalId);
    }

    internal void ReorderResources(Guid subjectExternalId, IReadOnlyList<Guid> resourceExternalIdsInOrder)
    {
        var subject = RequireSubject(subjectExternalId);
        subject.ReorderResources(resourceExternalIdsInOrder);
    }

    private SubjectTemplate RequireSubject(Guid subjectExternalId)
    {
        return _subjects.FirstOrDefault(s => s.ExternalId == subjectExternalId)
            ?? throw new InvalidOperationException("Materia no encontrada.");
    }
}
