using FluentValidation;

namespace Mentoory.Tenant.Application.Commands.CreateIncubator;

/// <summary>
/// Validator for the CreateIncubatorCommand.
/// </summary>
public class CreateIncubatorValidator : AbstractValidator<CreateIncubatorCommand>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CreateIncubatorValidator"/> class.
    /// </summary>
    public CreateIncubatorValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required")
            .MaximumLength(200).WithMessage("Name must not exceed 200 characters");
    }
}
