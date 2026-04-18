using Mentoory.Knowledge.Domain.Enums;
using Mentoory.Knowledge.Domain.ValueObjects;
using Mentoory.Shared.Domain.SeedWork;

namespace Mentoory.Knowledge.Domain.Aggregates.KnowledgeStructureTemplate;

public class ModuleTemplate : Entity
{
    private readonly List<TopicTemplate> _topics = new();

    private ModuleTemplate()
    {
    }

    public Guid ExternalId { get; private set; }
    public string Name { get; private set; } = null!;
    public string? Description { get; private set; }
    public int SortOrder { get; private set; }

    public IReadOnlyCollection<TopicTemplate> Topics => _topics.AsReadOnly();

    internal static ModuleTemplate Create(string name, string? description, int sortOrder)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        return new ModuleTemplate
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

    internal TopicTemplate AddTopic(string name, string? description, int sortOrder)
    {
        var topic = TopicTemplate.Create(name, description, sortOrder);
        _topics.Add(topic);
        return topic;
    }

    internal void UpdateTopic(Guid topicExternalId, string name, string? description)
    {
        var topic = RequireTopic(topicExternalId);
        topic.UpdateDetails(name, description);
    }

    internal void RemoveTopic(Guid topicExternalId)
    {
        var topic = RequireTopic(topicExternalId);
        _topics.Remove(topic);
    }

    internal void ReorderTopics(IReadOnlyList<Guid> topicExternalIdsInOrder)
    {
        for (var i = 0; i < topicExternalIdsInOrder.Count; i++)
        {
            var topic = _topics.FirstOrDefault(t => t.ExternalId == topicExternalIdsInOrder[i]);
            topic?.UpdateSortOrder(i + 1);
        }
    }

    internal void UpdateTopicPriorityRanges(
        Guid topicExternalId,
        PriorityRange? high,
        PriorityRange? medium,
        PriorityRange? low)
    {
        var topic = RequireTopic(topicExternalId);
        topic.UpdatePriorityRanges(high, medium, low);
    }

    internal SubjectTemplate AddSubject(Guid topicExternalId, string name, string? description, int sortOrder)
    {
        var topic = RequireTopic(topicExternalId);
        return topic.AddSubject(name, description, sortOrder);
    }

    internal void UpdateSubject(Guid topicExternalId, Guid subjectExternalId, string name, string? description)
    {
        var topic = RequireTopic(topicExternalId);
        topic.UpdateSubject(subjectExternalId, name, description);
    }

    internal void RemoveSubject(Guid topicExternalId, Guid subjectExternalId)
    {
        var topic = RequireTopic(topicExternalId);
        topic.RemoveSubject(subjectExternalId);
    }

    internal void ReorderSubjects(Guid topicExternalId, IReadOnlyList<Guid> subjectExternalIdsInOrder)
    {
        var topic = RequireTopic(topicExternalId);
        topic.ReorderSubjects(subjectExternalIdsInOrder);
    }

    internal ResourceTemplate AddResource(
        Guid topicExternalId,
        Guid subjectExternalId,
        string title,
        string? description,
        string url,
        ResourceType type,
        int sortOrder)
    {
        var topic = RequireTopic(topicExternalId);
        return topic.AddResource(subjectExternalId, title, description, url, type, sortOrder);
    }

    internal void UpdateResource(
        Guid topicExternalId,
        Guid subjectExternalId,
        Guid resourceExternalId,
        string title,
        string? description,
        string url,
        ResourceType type)
    {
        var topic = RequireTopic(topicExternalId);
        topic.UpdateResource(subjectExternalId, resourceExternalId, title, description, url, type);
    }

    internal void RemoveResource(Guid topicExternalId, Guid subjectExternalId, Guid resourceExternalId)
    {
        var topic = RequireTopic(topicExternalId);
        topic.RemoveResource(subjectExternalId, resourceExternalId);
    }

    internal void ReorderResources(
        Guid topicExternalId,
        Guid subjectExternalId,
        IReadOnlyList<Guid> resourceExternalIdsInOrder)
    {
        var topic = RequireTopic(topicExternalId);
        topic.ReorderResources(subjectExternalId, resourceExternalIdsInOrder);
    }

    private TopicTemplate RequireTopic(Guid topicExternalId)
    {
        return _topics.FirstOrDefault(t => t.ExternalId == topicExternalId)
            ?? throw new InvalidOperationException("Tema no encontrado.");
    }
}
