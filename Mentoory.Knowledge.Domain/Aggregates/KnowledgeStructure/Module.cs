using Mentoory.Knowledge.Domain.Aggregates.KnowledgeStructureTemplate;
using Mentoory.Knowledge.Domain.Enums;
using Mentoory.Knowledge.Domain.ValueObjects;
using Mentoory.Shared.Domain.SeedWork;

namespace Mentoory.Knowledge.Domain.Aggregates.KnowledgeStructure;

public class Module : Entity
{
    private readonly List<Topic> _topics = new();

    private Module()
    {
    }

    public Guid ExternalId { get; private set; }
    public Guid? SourceTemplateModuleExternalId { get; private set; }
    public string Name { get; private set; } = null!;
    public string? Description { get; private set; }
    public int SortOrder { get; private set; }

    public IReadOnlyCollection<Topic> Topics => _topics.AsReadOnly();

    internal static Module Create(string name, string? description, int sortOrder)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        return new Module
        {
            ExternalId = Guid.NewGuid(),
            SourceTemplateModuleExternalId = null,
            Name = name,
            Description = description,
            SortOrder = sortOrder,
        };
    }

    internal static Module CloneFromTemplate(ModuleTemplate template)
    {
        ArgumentNullException.ThrowIfNull(template);

        var module = new Module
        {
            ExternalId = Guid.NewGuid(),
            SourceTemplateModuleExternalId = template.ExternalId,
            Name = template.Name,
            Description = template.Description,
            SortOrder = template.SortOrder,
        };

        foreach (var topicTemplate in template.Topics.OrderBy(t => t.SortOrder))
        {
            module._topics.Add(Topic.CloneFromTemplate(topicTemplate));
        }

        return module;
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

    internal Topic AddTopic(string name, string? description, int sortOrder)
    {
        var topic = Topic.Create(name, description, sortOrder);
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

    internal Subject AddSubject(Guid topicExternalId, string name, string? description, int sortOrder)
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

    internal Resource AddResource(
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

    internal void ApplyPartialSync(ModuleTemplate template, PartialSyncResult result)
    {
        foreach (var tplTopic in template.Topics.OrderBy(t => t.SortOrder))
        {
            var cloneTopic = _topics.FirstOrDefault(t =>
                t.SourceTemplateTopicExternalId == tplTopic.ExternalId);

            if (cloneTopic is null)
            {
                var maxSort = _topics.Count > 0 ? _topics.Max(t => t.SortOrder) : 0;
                cloneTopic = Topic.CloneFromTemplate(tplTopic);
                cloneTopic.UpdateSortOrder(maxSort + 1);
                _topics.Add(cloneTopic);
                result.TopicsAdded++;
                result.SubjectsAdded += tplTopic.Subjects.Count;
                result.ResourcesAdded += tplTopic.Subjects.SelectMany(s => s.Resources).Count();
                continue;
            }

            cloneTopic.ApplyPartialSync(tplTopic, result);
        }
    }

    private Topic RequireTopic(Guid topicExternalId)
    {
        return _topics.FirstOrDefault(t => t.ExternalId == topicExternalId)
            ?? throw new InvalidOperationException("Tema no encontrado.");
    }
}
