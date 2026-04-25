using Mentoory.Knowledge.Domain.Repositories;
using Mentoory.Shared.Application;
using Mentoory.Shared.Application.MediatR;

namespace Mentoory.Knowledge.Application.Commands.AddResourceTemplate;

/// <summary>
/// Handles the addition of a resource to a knowledge structure template.
/// </summary>
public class AddResourceTemplateHandler : BaseCommandHandler<AddResourceTemplateCommand, Guid>
{
    private readonly IKnowledgeStructureTemplateRepository _repository;

    /// <summary>
    /// Initializes a new instance of the <see cref="AddResourceTemplateHandler"/> class.
    /// </summary>
    /// <param name="repository">The knowledge structure template repository.</param>
    public AddResourceTemplateHandler(IKnowledgeStructureTemplateRepository repository)
    {
        _repository = repository;
    }

    /// <inheritdoc />
    public override async Task<Result<Guid>> Handle(
        AddResourceTemplateCommand request,
        CancellationToken cancellationToken)
    {
        var template = await _repository.GetByExternalIdWithFullTreeAsync(request.TemplateExternalId, cancellationToken);
        if (template is null)
        {
            return Failure(ResultErrorCodes.GenericError, ("Plantilla", "La plantilla no fue encontrada."));
        }

        try
        {
            var resource = template.AddResource(
                request.SubjectExternalId,
                request.Title,
                request.Description,
                request.Url,
                request.ResourceType,
                request.SortOrder);

            await _repository.UnitOfWork.SaveEntitiesAsync(cancellationToken);
            return Success(resource.ExternalId);
        }
        catch (InvalidOperationException ex)
        {
            return Failure(ResultErrorCodes.GenericError, ("Operación", ex.Message));
        }
    }
}
