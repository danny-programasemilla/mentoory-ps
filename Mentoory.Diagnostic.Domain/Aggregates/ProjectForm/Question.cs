using Mentoory.Diagnostic.Domain.Aggregates.FormTemplate;
using Mentoory.Diagnostic.Domain.Enums;
using Mentoory.Shared.Domain.SeedWork;

namespace Mentoory.Diagnostic.Domain.Aggregates.ProjectForm;

public class Question : Entity
{
    private readonly List<AnswerOption> _answerOptions = new();
    private readonly List<FollowUpQuestion> _followUpQuestions = new();

    private Question()
    {
    }

    public Guid ExternalId { get; private set; }
    public long TopicId { get; private set; }
    public string QuestionText { get; private set; } = null!;
    public QuestionType QuestionType { get; private set; }
    public StageApplicability StageApplicability { get; private set; }
    public int SortOrder { get; private set; }
    public string? BlockGroup { get; private set; }
    public bool IsOptional { get; private set; }

    public IReadOnlyCollection<AnswerOption> AnswerOptions => _answerOptions.AsReadOnly();
    public IReadOnlyCollection<FollowUpQuestion> FollowUpQuestions => _followUpQuestions.AsReadOnly();

    internal static Question Create(
        long topicId,
        string questionText,
        QuestionType questionType,
        StageApplicability stageApplicability,
        int sortOrder,
        string? blockGroup,
        bool isOptional)
    {
        return new Question
        {
            ExternalId = Guid.NewGuid(),
            TopicId = topicId,
            QuestionText = questionText,
            QuestionType = questionType,
            StageApplicability = stageApplicability,
            SortOrder = sortOrder,
            BlockGroup = blockGroup,
            IsOptional = isOptional,
        };
    }

    internal static Question CloneFromTemplate(QuestionTemplate template)
    {
        var question = new Question
        {
            ExternalId = Guid.NewGuid(),
            TopicId = template.TopicId,
            QuestionText = template.QuestionText,
            QuestionType = template.QuestionType,
            StageApplicability = template.StageApplicability,
            SortOrder = template.SortOrder,
            BlockGroup = template.BlockGroup,
            IsOptional = template.IsOptional,
        };

        foreach (var ao in template.AnswerOptions.OrderBy(a => a.SortOrder))
        {
            question._answerOptions.Add(AnswerOption.CloneFromTemplate(ao));
        }

        return question;
    }

    internal AnswerOption AddAnswerOption(
        string optionText,
        decimal score,
        SwotClassification swotClassification,
        OdsrOrientation odsrOrientation,
        int sortOrder)
    {
        var option = AnswerOption.Create(optionText, score, swotClassification, odsrOrientation, sortOrder);
        _answerOptions.Add(option);
        return option;
    }

    internal FollowUpQuestion AddFollowUpQuestion(string questionText, int sortOrder)
    {
        var followUp = FollowUpQuestion.Create(questionText, sortOrder);
        _followUpQuestions.Add(followUp);
        return followUp;
    }

    internal void UpdateText(string questionText)
    {
        QuestionText = questionText;
    }

    internal void UpdateSortOrder(int sortOrder)
    {
        SortOrder = sortOrder;
    }
}
