using Mentoory.Diagnostic.Domain.Enums;
using Mentoory.Shared.Domain.SeedWork;

namespace Mentoory.Diagnostic.Domain.Aggregates.FormTemplate;

public class AnswerOptionTemplate : Entity
{
    private AnswerOptionTemplate()
    {
    }

    public string OptionText { get; private set; } = null!;
    public decimal Score { get; private set; }
    public SwotClassification SwotClassification { get; private set; }
    public OdsrOrientation OdsrOrientation { get; private set; }
    public int SortOrder { get; private set; }

    internal static AnswerOptionTemplate Create(
        string optionText,
        decimal score,
        SwotClassification swotClassification,
        OdsrOrientation odsrOrientation,
        int sortOrder)
    {
        return new AnswerOptionTemplate
        {
            OptionText = optionText,
            Score = score,
            SwotClassification = swotClassification,
            OdsrOrientation = odsrOrientation,
            SortOrder = sortOrder,
        };
    }
}
