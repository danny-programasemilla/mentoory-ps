using Mentoory.Shared.Domain.SeedWork;

namespace Mentoory.Diagnostic.Domain.Aggregates.FormTemplate;

public class FormTemplate : Entity, IAggregateRoot
{
    private readonly List<QuestionTemplate> _questions = new();

    private FormTemplate()
    {
    }

    public Guid ExternalId { get; private set; }
    public string Name { get; private set; } = null!;
    public string? Description { get; private set; }
    public string? SubscriptionTier { get; private set; }
    public int Version { get; private set; }
    public bool IsActive { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }

    public IReadOnlyCollection<QuestionTemplate> Questions => _questions.AsReadOnly();

    public static FormTemplate Create(
        string name,
        string? description,
        string? subscriptionTier,
        DateTime utcNow)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        return new FormTemplate
        {
            ExternalId = Guid.NewGuid(),
            Name = name,
            Description = description,
            SubscriptionTier = subscriptionTier,
            Version = 1,
            IsActive = true,
            CreatedAtUtc = utcNow,
        };
    }

    public void Update(string name, string? description, string? subscriptionTier)
    {
        Name = name;
        Description = description;
        SubscriptionTier = subscriptionTier;
        Version++;
    }

    public void Activate()
    {
        IsActive = true;
    }

    public void Deactivate()
    {
        IsActive = false;
    }

    public QuestionTemplate AddQuestion(
        long topicId,
        string questionText,
        Enums.QuestionType questionType,
        Enums.StageApplicability stageApplicability,
        int sortOrder,
        string? blockGroup,
        bool isOptional)
    {
        var question = QuestionTemplate.Create(
            topicId, questionText, questionType, stageApplicability, sortOrder, blockGroup, isOptional);
        _questions.Add(question);
        return question;
    }

    public void RemoveQuestion(long questionId)
    {
        var question = _questions.FirstOrDefault(q => q.Id == questionId)
            ?? throw new InvalidOperationException("Question not found in template.");
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
}
