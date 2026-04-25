using Mentoory.Knowledge.Domain.Repositories;
using Mentoory.Shared.Application;
using Mentoory.Shared.Application.MediatR;

namespace Mentoory.Knowledge.Application.Commands.AddSubjectTemplate;

/// <summary>
/// Handles the addition of a subject to a knowledge structure template.
/// </summary>
public class AddSubjectTemplateHandler : BaseCommandHandler<AddSubjectTemplateCommand, Guid>
{
    private readonly IKnowledgeStructureTemplateRepository _repository;

    /// <summary>
    /// Initializes a new instance of the <see cref="AddSubjectTemplateHandler"/> class.
    /// </summary>
    /// <param name="repository">The knowledge structure template repository.</param>
    public AddSubjectTemplateHandler(IKnowledgeStructureTemplateRepository repository)
    {
        _repository = repository;
    }

    /// <inheritdoc />
    public override async Task<Result<Guid>> Handle(
        AddSubjectTemplateCommand request,
        CancellationToken cancellationToken)
    {
        var template = await _repository.GetByExternalIdWithFullTreeAsync(request.TemplateExternalId, cancellationToken);
        if (template is null)
        {
            return Failure(ResultErrorCodes.GenericError, ("Plantilla", "La plantilla no fue encontrada."));
        }

        try
        {
            var subject = template.AddSubject(request.TopicExternalId, request.Name, request.Description, request.SortOrder);
            await _repository.UnitOfWork.SaveEntitiesAsync(cancellationToken);
            return Success(subject.ExternalId);
        }
        catch (InvalidOperationException ex)
        {
            return Failure(ResultErrorCodes.GenericError, ("Operación", ex.Message));
        }
    }
}
