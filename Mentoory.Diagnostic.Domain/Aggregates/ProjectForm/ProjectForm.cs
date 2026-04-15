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

    public static ProjectForm CloneFromTemplate(
        FormTemplate.FormTemplate template,
        long projectId,
        long incubatorId,
        DateTime utcNow)
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
            var question = Question.CloneFromTemplate(qt);
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
        int sortOrder,
        string? blockGroup,
        bool isOptional)
    {
        var question = Question.Create(
            topicId, questionText, questionType, sortOrder, blockGroup, isOptional);
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

    public void SyncNewQuestionsFromTemplate(FormTemplate.FormTemplate template)
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
            // Only add questions that don't already exist (by topic + text match)
            var alreadyExists = _questions.Any(q =>
                q.TopicId == qt.TopicId && q.QuestionText == qt.QuestionText);

            if (!alreadyExists)
            {
                maxSortOrder++;
                var question = Question.CloneFromTemplate(qt);
                question.UpdateSortOrder(maxSortOrder);
                _questions.Add(question);
            }
        }

        SourceTemplateVersion = template.Version;
    }
}
