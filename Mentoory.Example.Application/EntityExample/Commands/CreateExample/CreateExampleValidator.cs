using FluentValidation;

namespace Mentoory.Example.Application.EntityExample.Commands.CreateExample;

/// <summary>
/// Validator for the CreateExampleCommand that ensures the command data is valid.
/// </summary>
public class CreateExampleValidator : AbstractValidator<CreateExampleCommand>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CreateExampleValidator"/> class
    /// and configures validation rules.
    /// </summary>
    public CreateExampleValidator()
    {
        RuleFor(x => x.Title).NotEmpty().WithMessage("Title is required");
    }
}
