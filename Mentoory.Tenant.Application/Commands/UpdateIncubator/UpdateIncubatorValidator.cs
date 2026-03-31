using FluentValidation;

namespace Mentoory.Tenant.Application.Commands.UpdateIncubator;

/// <summary>
/// Validator for the UpdateIncubatorCommand.
/// </summary>
public class UpdateIncubatorValidator : AbstractValidator<UpdateIncubatorCommand>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UpdateIncubatorValidator"/> class.
    /// </summary>
    public UpdateIncubatorValidator()
    {
        RuleFor(x => x.ExternalId)
            .NotEmpty().WithMessage("ExternalId is required");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required")
            .MaximumLength(200).WithMessage("Name must not exceed 200 characters");
    }
}
