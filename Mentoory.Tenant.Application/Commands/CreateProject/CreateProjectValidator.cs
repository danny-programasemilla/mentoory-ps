using FluentValidation;

namespace Mentoory.Tenant.Application.Commands.CreateProject;

/// <summary>
/// Validator for the CreateProjectCommand.
/// </summary>
public class CreateProjectValidator : AbstractValidator<CreateProjectCommand>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CreateProjectValidator"/> class.
    /// </summary>
    public CreateProjectValidator()
    {
        RuleFor(x => x.IncubatorExternalId)
            .NotEmpty().WithMessage("IncubatorExternalId is required");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required")
            .MaximumLength(200).WithMessage("Name must not exceed 200 characters");
    }
}
