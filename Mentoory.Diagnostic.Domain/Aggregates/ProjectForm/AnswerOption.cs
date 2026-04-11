using Mentoory.Diagnostic.Domain.Aggregates.FormTemplate;
using Mentoory.Diagnostic.Domain.Enums;
using Mentoory.Shared.Domain.SeedWork;

namespace Mentoory.Diagnostic.Domain.Aggregates.ProjectForm;

public class AnswerOption : Entity
{
    private AnswerOption()
    {
    }

    public string OptionText { get; private set; } = null!;
    public decimal Score { get; private set; }
    public SwotClassification SwotClassification { get; private set; }
    public OdsrOrientation OdsrOrientation { get; private set; }
    public int SortOrder { get; private set; }

    internal static AnswerOption Create(
        string optionText,
        decimal score,
        SwotClassification swotClassification,
        OdsrOrientation odsrOrientation,
        int sortOrder)
    {
        return new AnswerOption
        {
            OptionText = optionText,
            Score = score,
            SwotClassification = swotClassification,
            OdsrOrientation = odsrOrientation,
            SortOrder = sortOrder,
        };
    }

    internal static AnswerOption CloneFromTemplate(AnswerOptionTemplate template)
    {
        return new AnswerOption
        {
            OptionText = template.OptionText,
            Score = template.Score,
            SwotClassification = template.SwotClassification,
            OdsrOrientation = template.OdsrOrientation,
            SortOrder = template.SortOrder,
        };
    }
}
