using Mentoory.Knowledge.Domain.Enums;
using Mentoory.Knowledge.Domain.ValueObjects;
using Mentoory.Shared.Domain.SeedWork;
using TemplateRoot = Mentoory.Knowledge.Domain.Aggregates.KnowledgeStructureTemplate.KnowledgeStructureTemplate;

namespace Mentoory.Knowledge.Domain.Aggregates.KnowledgeStructure;

public class KnowledgeStructure : Entity, IAggregateRoot
{
    private readonly List<Module> _modules = new();

    private KnowledgeStructure()
    {
    }

    public Guid ExternalId { get; private set; }
    public long ProjectId { get; private set; }
    public long IncubatorId { get; private set; }
    public string Name { get; private set; } = null!;
    public string? Description { get; private set; }
    public long? SourceTemplateId { get; private set; }
    public int? SourceTemplateVersion { get; private set; }
    public SyncMode SyncMode { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }

    public IReadOnlyCollection<Module> Modules => _modules.AsReadOnly();

    public static KnowledgeStructure CloneFromTemplate(
        TemplateRoot template,
        long projectId,
        long incubatorId,
        DateTime utcNow)
    {
        ArgumentNullException.ThrowIfNull(template);

        var clone = new KnowledgeStructure
        {
            ExternalId = Guid.NewGuid(),
            ProjectId = projectId,
            IncubatorId = incubatorId,
            Name = template.Name,
            Description = template.Description,
            SourceTemplateId = template.Id,
            SourceTemplateVersion = template.Version,
            SyncMode = SyncMode.Disconnected,
            CreatedAtUtc = utcNow,
        };

        foreach (var moduleTemplate in template.Modules.OrderBy(m => m.SortOrder))
        {
            clone._modules.Add(Module.CloneFromTemplate(moduleTemplate));
        }

        return clone;
    }

    /// <summary>
    /// Reserved for future "create-from-scratch" workflow. In v1, clones are always created via
    /// <see cref="CloneFromTemplate"/>; callers must use that factory.
    /// </summary>
    public static KnowledgeStructure Create(
        string name,
        string? description,
        long projectId,
        long incubatorId,
        DateTime utcNow)
    {
        _ = name;
        _ = description;
        _ = projectId;
        _ = incubatorId;
        _ = utcNow;
        throw new NotSupportedException("La creación desde cero no está implementada; clone desde una plantilla.");
    }

    public void UpdateDetails(string name, string? description)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        Name = name;
        Description = description;
    }

    public void SetSyncMode(SyncMode mode)
    {
        if (mode == SyncMode.PartialSync && SourceTemplateId is null)
        {
            throw new InvalidOperationException(
                "No se puede habilitar la sincronización parcial sin una plantilla de origen.");
        }

        SyncMode = mode;
    }

    public Module AddModule(string name, string? description, int sortOrder)
    {
        var module = Module.Create(name, description, sortOrder);
        _modules.Add(module);
        return module;
    }

    public void UpdateModule(Guid moduleExternalId, string name, string? description)
    {
        var module = RequireModule(moduleExternalId);
        module.UpdateDetails(name, description);
    }

    public void RemoveModule(Guid moduleExternalId)
    {
        var module = RequireModule(moduleExternalId);
        _modules.Remove(module);
    }

    public void ReorderModules(IReadOnlyList<Guid> moduleExternalIdsInOrder)
    {
        AssertReorderMatches(_modules.Select(m => m.ExternalId), moduleExternalIdsInOrder, "módulos");

        for (var i = 0; i < moduleExternalIdsInOrder.Count; i++)
        {
            var module = _modules.First(m => m.ExternalId == moduleExternalIdsInOrder[i]);
            module.UpdateSortOrder(i + 1);
        }
    }

    public Topic AddTopic(Guid moduleExternalId, string name, string? description, int sortOrder)
    {
        var module = RequireModule(moduleExternalId);
        return module.AddTopic(name, description, sortOrder);
    }

    public void UpdateTopic(Guid topicExternalId, string name, string? description)
    {
        var (module, _) = RequireTopic(topicExternalId);
        module.UpdateTopic(topicExternalId, name, description);
    }

    public void RemoveTopic(Guid topicExternalId)
    {
        var (module, _) = RequireTopic(topicExternalId);
        module.RemoveTopic(topicExternalId);
    }

    public void ReorderTopics(Guid moduleExternalId, IReadOnlyList<Guid> topicExternalIdsInOrder)
    {
        var module = RequireModule(moduleExternalId);
        module.ReorderTopics(topicExternalIdsInOrder);
    }

    public void UpdateTopicPriorityRanges(
        Guid topicExternalId,
        PriorityRange? high,
        PriorityRange? medium,
        PriorityRange? low)
    {
        // Event emission is deferred to US4 — the `TopicPriorityRangesChanged` integration event
        // will be added when the application handler is introduced.
        var (module, _) = RequireTopic(topicExternalId);
        module.UpdateTopicPriorityRanges(topicExternalId, high, medium, low);
    }

    public Subject AddSubject(Guid topicExternalId, string name, string? description, int sortOrder)
    {
        var (module, _) = RequireTopic(topicExternalId);
        return module.AddSubject(topicExternalId, name, description, sortOrder);
    }

    public void UpdateSubject(Guid subjectExternalId, string name, string? description)
    {
        var (module, topic, _) = RequireSubject(subjectExternalId);
        module.UpdateSubject(topic.ExternalId, subjectExternalId, name, description);
    }

    public void RemoveSubject(Guid subjectExternalId)
    {
        var (module, topic, _) = RequireSubject(subjectExternalId);
        module.RemoveSubject(topic.ExternalId, subjectExternalId);
    }

    public void ReorderSubjects(Guid topicExternalId, IReadOnlyList<Guid> subjectExternalIdsInOrder)
    {
        var (module, _) = RequireTopic(topicExternalId);
        module.ReorderSubjects(topicExternalId, subjectExternalIdsInOrder);
    }

    public Resource AddResource(
        Guid subjectExternalId,
        string title,
        string? description,
        string url,
        ResourceType type,
        int sortOrder)
    {
        var (module, topic, _) = RequireSubject(subjectExternalId);
        return module.AddResource(topic.ExternalId, subjectExternalId, title, description, url, type, sortOrder);
    }

    public void UpdateResource(
        Guid resourceExternalId,
        string title,
        string? description,
        string url,
        ResourceType type)
    {
        var (module, topic, subject, _) = RequireResource(resourceExternalId);
        module.UpdateResource(topic.ExternalId, subject.ExternalId, resourceExternalId, title, description, url, type);
    }

    public void RemoveResource(Guid resourceExternalId)
    {
        var (module, topic, subject, _) = RequireResource(resourceExternalId);
        module.RemoveResource(topic.ExternalId, subject.ExternalId, resourceExternalId);
    }

    public void ReorderResources(Guid subjectExternalId, IReadOnlyList<Guid> resourceExternalIdsInOrder)
    {
        var (module, topic, _) = RequireSubject(subjectExternalId);
        module.ReorderResources(topic.ExternalId, subjectExternalId, resourceExternalIdsInOrder);
    }

    /// <summary>
    /// Applies a partial synchronization from the source template: appends any template-side
    /// modules/topics/subjects/resources that are missing from this clone while leaving all
    /// locally-added or locally-renamed items untouched. Requires <see cref="SyncMode.PartialSync"/>.
    /// </summary>
    /// <param name="template">The source template (must match <see cref="SourceTemplateId"/>).</param>
    /// <returns>Counts of items appended at each level.</returns>
    public PartialSyncResult ApplyPartialSync(TemplateRoot template)
    {
        ArgumentNullException.ThrowIfNull(template);

        if (SyncMode != Enums.SyncMode.PartialSync)
        {
            throw new InvalidOperationException(
                "La estructura no está en modo de sincronización parcial.");
        }

        if (SourceTemplateId is null || template.Id != SourceTemplateId.Value)
        {
            throw new InvalidOperationException(
                "La plantilla proporcionada no coincide con la plantilla de origen.");
        }

        var result = new PartialSyncResult();

        foreach (var tplModule in template.Modules.OrderBy(m => m.SortOrder))
        {
            var cloneModule = _modules.FirstOrDefault(m =>
                m.SourceTemplateModuleExternalId == tplModule.ExternalId);

            if (cloneModule is null)
            {
                // Entire module (and all descendants) new — append at max+1.
                var maxSort = _modules.Count > 0 ? _modules.Max(m => m.SortOrder) : 0;
                cloneModule = Module.CloneFromTemplate(tplModule);
                cloneModule.UpdateSortOrder(maxSort + 1);
                _modules.Add(cloneModule);
                result.ModulesAdded++;
                result.TopicsAdded += tplModule.Topics.Count;
                result.SubjectsAdded += tplModule.Topics.SelectMany(t => t.Subjects).Count();
                result.ResourcesAdded += tplModule.Topics
                    .SelectMany(t => t.Subjects)
                    .SelectMany(s => s.Resources)
                    .Count();
                continue;
            }

            cloneModule.ApplyPartialSync(tplModule, result);
        }

        SourceTemplateVersion = template.Version;
        return result;
    }

    private static void AssertReorderMatches(
        IEnumerable<Guid> current,
        IReadOnlyList<Guid> requested,
        string childLabel)
    {
        var currentSet = current.ToHashSet();
        var requestedSet = requested.ToHashSet();

        if (requestedSet.Count != requested.Count
            || requestedSet.Count != currentSet.Count
            || !requestedSet.SetEquals(currentSet))
        {
            throw new InvalidOperationException(
                $"La lista de reordenamiento no coincide con los {childLabel} actuales.");
        }
    }

    private Module RequireModule(Guid moduleExternalId)
    {
        return _modules.FirstOrDefault(m => m.ExternalId == moduleExternalId)
            ?? throw new InvalidOperationException("Módulo no encontrado.");
    }

    private (Module Module, Topic Topic) RequireTopic(Guid topicExternalId)
    {
        foreach (var module in _modules)
        {
            var topic = module.Topics.FirstOrDefault(t => t.ExternalId == topicExternalId);
            if (topic is not null)
            {
                return (module, topic);
            }
        }

        throw new InvalidOperationException("Tema no encontrado.");
    }

    private (Module Module, Topic Topic, Subject Subject) RequireSubject(Guid subjectExternalId)
    {
        foreach (var module in _modules)
        {
            foreach (var topic in module.Topics)
            {
                var subject = topic.Subjects.FirstOrDefault(s => s.ExternalId == subjectExternalId);
                if (subject is not null)
                {
                    return (module, topic, subject);
                }
            }
        }

        throw new InvalidOperationException("Materia no encontrada.");
    }

    private (Module Module, Topic Topic, Subject Subject, Resource Resource) RequireResource(Guid resourceExternalId)
    {
        foreach (var module in _modules)
        {
            foreach (var topic in module.Topics)
            {
                foreach (var subject in topic.Subjects)
                {
                    var resource = subject.Resources.FirstOrDefault(r => r.ExternalId == resourceExternalId);
                    if (resource is not null)
                    {
                        return (module, topic, subject, resource);
                    }
                }
            }
        }

        throw new InvalidOperationException("Recurso no encontrado.");
    }
}
