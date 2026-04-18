using Mentoory.Diagnostic.Domain.Aggregates.FormTemplate;
using Mentoory.Diagnostic.Domain.Enums;
using Mentoory.Shared.Domain.SeedWork;

namespace Mentoory.Diagnostic.Domain.Aggregates.ProjectForm;

public class ProjectForm : Entity, IAggregateRoot
{
    private readonly List<Question> _questions = new();

    private ProjectForm()
    {
    }

    public Guid ExternalId { get; private set; }
    public long ProjectId { get; private set; }
    public long IncubatorId { get; private set; }
    public long? SourceTemplateId { get; private set; }
    public int? SourceTemplateVersion { get; private set; }
    public string Name { get; private set; } = null!;
    public SyncMode SyncMode { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }

    public IReadOnlyCollection<Question> Questions => _questions.AsReadOnly();

    /// <summary>
    /// Clones a form template into a project-scoped form, optionally rewriting template-topic ids
    /// to the matching project-topic ids in a project-specific knowledge structure.
    /// </summary>
    /// <remarks>
    /// Rewrite contract (FR-K22 / FR-K23): if <paramref name="topicIdRewriteMap"/> is null, every
    /// question's <c>TopicId</c> is copied from the template as-is. If the map is non-null, each
    /// template question's <c>TopicId</c> MUST be a key of the map; otherwise the factory throws
    /// <see cref="InvalidOperationException"/> with a message identifying the offending question.
    /// The map is the only external input besides the template — the rewrite is routed through the
    /// aggregate root (never exposed as a post-hoc mutator on <see cref="Question"/>), preserving
    /// the DDD invariant that only the aggregate root mutates its children.
    /// </remarks>
    public static ProjectForm CloneFromTemplate(
        FormTemplate.FormTemplate template,
        long projectId,
        long incubatorId,
        DateTime utcNow,
        IReadOnlyDictionary<long, long>? topicIdRewriteMap = null)
    {
        var form = new ProjectForm
        {
            ExternalId = Guid.NewGuid(),
            ProjectId = projectId,
            IncubatorId = incubatorId,
            SourceTemplateId = template.Id,
            SourceTemplateVersion = template.Version,
            Name = template.Name,
            SyncMode = SyncMode.Disconnected,
            CreatedAtUtc = utcNow,
        };

        foreach (var qt in template.Questions.OrderBy(q => q.SortOrder))
        {
            var topicIdOverride = ResolveTopicIdOverride(qt, topicIdRewriteMap);
            var question = Question.CloneFromTemplate(qt, topicIdOverride);
            form._questions.Add(question);
        }

        return form;
    }

    public static ProjectForm Create(
        string name,
        long projectId,
        long incubatorId,
        DateTime utcNow)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        return new ProjectForm
        {
            ExternalId = Guid.NewGuid(),
            ProjectId = projectId,
            IncubatorId = incubatorId,
            Name = name,
            SyncMode = SyncMode.Disconnected,
            CreatedAtUtc = utcNow,
        };
    }

    public Question AddQuestion(
        long topicId,
        string questionText,
        QuestionType questionType,
        StageApplicability stageApplicability,
        int sortOrder,
        string? blockGroup,
        bool isOptional)
    {
        var question = Question.Create(
            topicId, questionText, questionType, stageApplicability, sortOrder, blockGroup, isOptional);
        _questions.Add(question);
        return question;
    }

    public void RemoveQuestion(long questionId)
    {
        var question = _questions.FirstOrDefault(q => q.Id == questionId)
            ?? throw new InvalidOperationException("Question not found in form.");
        _questions.Remove(question);
    }

    public void ReorderQuestions(IReadOnlyList<long> questionIdsInOrder)
    {
        for (var i = 0; i < questionIdsInOrder.Count; i++)
        {
            var question = _questions.FirstOrDefault(q => q.Id == questionIdsInOrder[i]);
            question?.UpdateSortOrder(i + 1);
        }
    }

    public void EnablePartialSync()
    {
        if (SourceTemplateId is null)
        {
            throw new InvalidOperationException("Cannot enable sync for a form without a source template.");
        }

        SyncMode = SyncMode.PartialSync;
    }

    public void DisableSync()
    {
        SyncMode = SyncMode.Disconnected;
    }

    /// <summary>
    /// Adds any questions present in the template but missing from this form. Honors the same
    /// rewrite contract as <see cref="CloneFromTemplate"/>: when <paramref name="topicIdRewriteMap"/>
    /// is provided, each template question's <c>TopicId</c> is rewritten to the matching project-topic
    /// id; duplicate detection compares against the REWRITTEN id so a sync after a knowledge binding
    /// does not double-insert questions.
    /// </summary>
    public void SyncNewQuestionsFromTemplate(
        FormTemplate.FormTemplate template,
        IReadOnlyDictionary<long, long>? topicIdRewriteMap = null)
    {
        if (SyncMode != SyncMode.PartialSync)
        {
            throw new InvalidOperationException("Form is not in partial sync mode.");
        }

        if (SourceTemplateId != template.Id)
        {
            throw new InvalidOperationException("Template does not match the source template.");
        }

        var maxSortOrder = _questions.Count > 0 ? _questions.Max(q => q.SortOrder) : 0;

        foreach (var qt in template.Questions.OrderBy(q => q.SortOrder))
        {
            // Only add questions that don't already exist (by topic + text match).
            // When a rewrite map is supplied, compare against the REWRITTEN topic id so we do not
            // double-insert questions whose template-topic id resolves to an existing project-topic id.
            var topicIdOverride = ResolveTopicIdOverride(qt, topicIdRewriteMap);
            var comparisonTopicId = topicIdOverride ?? qt.TopicId;

            var alreadyExists = _questions.Any(q =>
                q.TopicId == comparisonTopicId && q.QuestionText == qt.QuestionText);

            if (!alreadyExists)
            {
                maxSortOrder++;
                var question = Question.CloneFromTemplate(qt, topicIdOverride);
                question.UpdateSortOrder(maxSortOrder);
                _questions.Add(question);
            }
        }

        SourceTemplateVersion = template.Version;
    }

    private static long? ResolveTopicIdOverride(
        FormTemplate.QuestionTemplate qt,
        IReadOnlyDictionary<long, long>? topicIdRewriteMap)
    {
        if (topicIdRewriteMap is null)
        {
            return null;
        }

        if (!topicIdRewriteMap.TryGetValue(qt.TopicId, out var newTopicId))
        {
            throw new InvalidOperationException(
                $"Cannot rewrite TopicId {qt.TopicId} for question '{qt.QuestionText}' — not present in the rewrite map.");
        }

        return newTopicId;
    }
}
