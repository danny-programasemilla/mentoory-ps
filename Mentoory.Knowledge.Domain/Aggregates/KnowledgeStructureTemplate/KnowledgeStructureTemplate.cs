using Mentoory.Knowledge.Domain.Enums;
using Mentoory.Knowledge.Domain.ValueObjects;
using Mentoory.Shared.Domain.SeedWork;

namespace Mentoory.Knowledge.Domain.Aggregates.KnowledgeStructureTemplate;

public class KnowledgeStructureTemplate : Entity, IAggregateRoot
{
    private readonly List<ModuleTemplate> _modules = new();

    private KnowledgeStructureTemplate()
    {
    }

    public Guid ExternalId { get; private set; }
    public string Name { get; private set; } = null!;
    public string? Description { get; private set; }
    public bool IsArchived { get; private set; }
    public int Version { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }

    public IReadOnlyCollection<ModuleTemplate> Modules => _modules.AsReadOnly();

    public static KnowledgeStructureTemplate Create(string name, string? description, DateTime utcNow)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        return new KnowledgeStructureTemplate
        {
            ExternalId = Guid.NewGuid(),
            Name = name,
            Description = description,
            IsArchived = false,
            Version = 1,
            CreatedAtUtc = utcNow,
        };
    }

    public void UpdateDetails(string name, string? description)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        Name = name;
        Description = description;
        Version++;
    }

    public void Archive()
    {
        IsArchived = true;
        Version++;
    }

    public void Unarchive()
    {
        IsArchived = false;
        Version++;
    }

    public ModuleTemplate AddModule(string name, string? description, int sortOrder)
    {
        var module = ModuleTemplate.Create(name, description, sortOrder);
        _modules.Add(module);
        Version++;
        return module;
    }

    public void UpdateModule(Guid moduleExternalId, string name, string? description)
    {
        var module = RequireModule(moduleExternalId);
        module.UpdateDetails(name, description);
        Version++;
    }

    public void RemoveModule(Guid moduleExternalId)
    {
        var module = RequireModule(moduleExternalId);
        _modules.Remove(module);
        Version++;
    }

    public void ReorderModules(IReadOnlyList<Guid> moduleExternalIdsInOrder)
    {
        AssertReorderMatches(_modules.Select(m => m.ExternalId), moduleExternalIdsInOrder, "módulos");

        for (var i = 0; i < moduleExternalIdsInOrder.Count; i++)
        {
            var module = _modules.First(m => m.ExternalId == moduleExternalIdsInOrder[i]);
            module.UpdateSortOrder(i + 1);
        }

        Version++;
    }

    public TopicTemplate AddTopic(Guid moduleExternalId, string name, string? description, int sortOrder)
    {
        var module = RequireModule(moduleExternalId);
        var topic = module.AddTopic(name, description, sortOrder);
        Version++;
        return topic;
    }

    public void UpdateTopic(Guid topicExternalId, string name, string? description)
    {
        var (module, _) = RequireTopic(topicExternalId);
        module.UpdateTopic(topicExternalId, name, description);
        Version++;
    }

    public void RemoveTopic(Guid topicExternalId)
    {
        var (module, _) = RequireTopic(topicExternalId);
        module.RemoveTopic(topicExternalId);
        Version++;
    }

    public void ReorderTopics(Guid moduleExternalId, IReadOnlyList<Guid> topicExternalIdsInOrder)
    {
        var module = RequireModule(moduleExternalId);
        module.ReorderTopics(topicExternalIdsInOrder);
        Version++;
    }

    public void UpdateTopicPriorityRanges(
        Guid topicExternalId,
        PriorityRange? high,
        PriorityRange? medium,
        PriorityRange? low)
    {
        var (module, _) = RequireTopic(topicExternalId);
        module.UpdateTopicPriorityRanges(topicExternalId, high, medium, low);
        Version++;
    }

    public SubjectTemplate AddSubject(Guid topicExternalId, string name, string? description, int sortOrder)
    {
        var (module, _) = RequireTopic(topicExternalId);
        var subject = module.AddSubject(topicExternalId, name, description, sortOrder);
        Version++;
        return subject;
    }

    public void UpdateSubject(Guid subjectExternalId, string name, string? description)
    {
        var (module, topic, _) = RequireSubject(subjectExternalId);
        module.UpdateSubject(topic.ExternalId, subjectExternalId, name, description);
        Version++;
    }

    public void RemoveSubject(Guid subjectExternalId)
    {
        var (module, topic, _) = RequireSubject(subjectExternalId);
        module.RemoveSubject(topic.ExternalId, subjectExternalId);
        Version++;
    }

    public void ReorderSubjects(Guid topicExternalId, IReadOnlyList<Guid> subjectExternalIdsInOrder)
    {
        var (module, _) = RequireTopic(topicExternalId);
        module.ReorderSubjects(topicExternalId, subjectExternalIdsInOrder);
        Version++;
    }

    public ResourceTemplate AddResource(
        Guid subjectExternalId,
        string title,
        string? description,
        string url,
        ResourceType type,
        int sortOrder)
    {
        var (module, topic, _) = RequireSubject(subjectExternalId);
        var resource = module.AddResource(topic.ExternalId, subjectExternalId, title, description, url, type, sortOrder);
        Version++;
        return resource;
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
        Version++;
    }

    public void RemoveResource(Guid resourceExternalId)
    {
        var (module, topic, subject, _) = RequireResource(resourceExternalId);
        module.RemoveResource(topic.ExternalId, subject.ExternalId, resourceExternalId);
        Version++;
    }

    public void ReorderResources(Guid subjectExternalId, IReadOnlyList<Guid> resourceExternalIdsInOrder)
    {
        var (module, topic, _) = RequireSubject(subjectExternalId);
        module.ReorderResources(topic.ExternalId, subjectExternalId, resourceExternalIdsInOrder);
        Version++;
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

    private ModuleTemplate RequireModule(Guid moduleExternalId)
    {
        return _modules.FirstOrDefault(m => m.ExternalId == moduleExternalId)
            ?? throw new InvalidOperationException("Módulo no encontrado.");
    }

    private (ModuleTemplate Module, TopicTemplate Topic) RequireTopic(Guid topicExternalId)
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

    private (ModuleTemplate Module, TopicTemplate Topic, SubjectTemplate Subject) RequireSubject(Guid subjectExternalId)
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

    private (ModuleTemplate Module, TopicTemplate Topic, SubjectTemplate Subject, ResourceTemplate Resource) RequireResource(Guid resourceExternalId)
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
