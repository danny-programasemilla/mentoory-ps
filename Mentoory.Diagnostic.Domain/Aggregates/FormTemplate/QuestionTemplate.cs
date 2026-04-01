using Mentoory.Diagnostic.Domain.Enums;
using Mentoory.Shared.Domain.SeedWork;

namespace Mentoory.Diagnostic.Domain.Aggregates.FormTemplate;

public class QuestionTemplate : Entity
{
    private readonly List<AnswerOptionTemplate> _answerOptions = new();

    private QuestionTemplate()
    {
    }

    public long TopicId { get; private set; }
    public string QuestionText { get; private set; } = null!;
    public QuestionType QuestionType { get; private set; }
    public StageApplicability StageApplicability { get; private set; }
    public int SortOrder { get; private set; }
    public string? BlockGroup { get; private set; }
    public bool IsOptional { get; private set; }

    public IReadOnlyCollection<AnswerOptionTemplate> AnswerOptions => _answerOptions.AsReadOnly();

    internal static QuestionTemplate Create(
        long topicId,
        string questionText,
        QuestionType questionType,
        StageApplicability stageApplicability,
        int sortOrder,
        string? blockGroup,
        bool isOptional)
    {
        return new QuestionTemplate
        {
            TopicId = topicId,
            QuestionText = questionText,
            QuestionType = questionType,
            StageApplicability = stageApplicability,
            SortOrder = sortOrder,
            BlockGroup = blockGroup,
            IsOptional = isOptional,
        };
    }

    internal AnswerOptionTemplate AddAnswerOption(
        string optionText,
        decimal score,
        SwotClassification swotClassification,
        OdsrOrientation odsrOrientation,
        int sortOrder)
    {
        var option = AnswerOptionTemplate.Create(optionText, score, swotClassification, odsrOrientation, sortOrder);
        _answerOptions.Add(option);
        return option;
    }

    internal void UpdateSortOrder(int sortOrder)
    {
        SortOrder = sortOrder;
    }
}
