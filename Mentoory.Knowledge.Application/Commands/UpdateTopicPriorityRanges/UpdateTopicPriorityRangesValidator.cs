using FluentValidation;

namespace Mentoory.Knowledge.Application.Commands.UpdateTopicPriorityRanges;

/// <summary>
/// Validator for the <see cref="UpdateTopicPriorityRangesCommand"/>.
/// </summary>
public class UpdateTopicPriorityRangesValidator : AbstractValidator<UpdateTopicPriorityRangesCommand>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UpdateTopicPriorityRangesValidator"/> class
    /// and configures validation rules including per-band bounds and cross-band overlap detection.
    /// </summary>
    public UpdateTopicPriorityRangesValidator()
    {
        RuleFor(x => x.StructureExternalId)
            .NotEmpty().WithMessage("El identificador de la estructura es requerido.");

        RuleFor(x => x.TopicExternalId)
            .NotEmpty().WithMessage("El identificador del tema es requerido.");

        When(x => x.High is not null, () =>
        {
            RuleFor(x => x.High!.Min)
                .LessThanOrEqualTo(x => x.High!.Max)
                .WithMessage("El mínimo del rango alto debe ser menor o igual al máximo.");
        });

        When(x => x.Medium is not null, () =>
        {
            RuleFor(x => x.Medium!.Min)
                .LessThanOrEqualTo(x => x.Medium!.Max)
                .WithMessage("El mínimo del rango medio debe ser menor o igual al máximo.");
        });

        When(x => x.Low is not null, () =>
        {
            RuleFor(x => x.Low!.Min)
                .LessThanOrEqualTo(x => x.Low!.Max)
                .WithMessage("El mínimo del rango bajo debe ser menor o igual al máximo.");
        });

        RuleFor(x => x)
            .Must(NotHaveOverlappingRanges)
            .WithMessage("Los rangos de prioridad se solapan.");
    }

    /// <summary>
    /// Returns true when no two configured bands share any point. Ranges are inclusive, so touching endpoints count as overlap.
    /// </summary>
    private static bool NotHaveOverlappingRanges(UpdateTopicPriorityRangesCommand cmd)
    {
        var ranges = new[] { cmd.High, cmd.Medium, cmd.Low };

        for (var i = 0; i < ranges.Length; i++)
        {
            var a = ranges[i];
            if (a is null || a.Min > a.Max)
            {
                continue;
            }

            for (var j = i + 1; j < ranges.Length; j++)
            {
                var b = ranges[j];
                if (b is null || b.Min > b.Max)
                {
                    continue;
                }

                if (a.Min <= b.Max && b.Min <= a.Max)
                {
                    return false;
                }
            }
        }

        return true;
    }
}
