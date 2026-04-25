using Mentoory.Knowledge.Domain.Aggregates.KnowledgeStructureTemplate;
using Mentoory.Knowledge.Domain.Enums;
using Mentoory.Knowledge.Domain.ValueObjects;
using Mentoory.Shared.Domain.SeedWork;

namespace Mentoory.Knowledge.Domain.Aggregates.KnowledgeStructure;

public class Topic : Entity
{
    private readonly List<Subject> _subjects = new();

    private Topic()
    {
    }

    public Guid ExternalId { get; private set; }
    public Guid? SourceTemplateTopicExternalId { get; private set; }
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

    public IReadOnlyCollection<Subject> Subjects => _subjects.AsReadOnly();

    /// <summary>
    /// Resolves the <see cref="Priority"/> band for a given score. Consumers (e.g. Mentoring Plan)
    /// call this to classify diagnostic results against the configured bands.
    /// </summary>
    public Priority ResolvePriority(decimal score)
    {
        if (HighRange is not null && HighRange.Contains(score))
        {
            return Priority.High;
        }

        if (MediumRange is not null && MediumRange.Contains(score))
        {
            return Priority.Medium;
        }

        if (LowRange is not null && LowRange.Contains(score))
        {
            return Priority.Low;
        }

        return Priority.NotApplicable;
    }

    internal static Topic Create(string name, string? description, int sortOrder)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        return new Topic
        {
            ExternalId = Guid.NewGuid(),
            SourceTemplateTopicExternalId = null,
            Name = name,
            Description = description,
            SortOrder = sortOrder,
        };
    }

    internal static Topic CloneFromTemplate(TopicTemplate template)
    {
        ArgumentNullException.ThrowIfNull(template);

        var topic = new Topic
        {
            ExternalId = Guid.NewGuid(),
            SourceTemplateTopicExternalId = template.ExternalId,
            Name = template.Name,
            Description = template.Description,
            SortOrder = template.SortOrder,
            HighRangeMin = template.HighRangeMin,
            HighRangeMax = template.HighRangeMax,
            MediumRangeMin = template.MediumRangeMin,
            MediumRangeMax = template.MediumRangeMax,
            LowRangeMin = template.LowRangeMin,
            LowRangeMax = template.LowRangeMax,
        };

        foreach (var subjectTemplate in template.Subjects.OrderBy(s => s.SortOrder))
        {
            topic._subjects.Add(Subject.CloneFromTemplate(subjectTemplate));
        }

        return topic;
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

    internal Subject AddSubject(string name, string? description, int sortOrder)
    {
        var subject = Subject.Create(name, description, sortOrder);
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

    internal Resource AddResource(
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

    internal void ApplyPartialSync(TopicTemplate template, PartialSyncResult result)
    {
        foreach (var tplSubject in template.Subjects.OrderBy(s => s.SortOrder))
        {
            var cloneSubject = _subjects.FirstOrDefault(s =>
                s.SourceTemplateSubjectExternalId == tplSubject.ExternalId);

            if (cloneSubject is null)
            {
                var maxSort = _subjects.Count > 0 ? _subjects.Max(s => s.SortOrder) : 0;
                cloneSubject = Subject.CloneFromTemplate(tplSubject);
                cloneSubject.UpdateSortOrder(maxSort + 1);
                _subjects.Add(cloneSubject);
                result.SubjectsAdded++;
                result.ResourcesAdded += tplSubject.Resources.Count;
                continue;
            }

            cloneSubject.ApplyPartialSync(tplSubject, result);
        }
    }

    private Subject RequireSubject(Guid subjectExternalId)
    {
        return _subjects.FirstOrDefault(s => s.ExternalId == subjectExternalId)
            ?? throw new InvalidOperationException("Materia no encontrada.");
    }
}
