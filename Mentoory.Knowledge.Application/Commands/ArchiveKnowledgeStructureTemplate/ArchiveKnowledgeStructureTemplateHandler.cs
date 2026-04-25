using Mentoory.Knowledge.Domain.Repositories;
using Mentoory.Shared.Application;
using Mentoory.Shared.Application.MediatR;

namespace Mentoory.Knowledge.Application.Commands.ArchiveKnowledgeStructureTemplate;

/// <summary>
/// Handles archiving of a knowledge structure template.
/// </summary>
public class ArchiveKnowledgeStructureTemplateHandler
    : BaseCommandHandler<ArchiveKnowledgeStructureTemplateCommand>
{
    private readonly IKnowledgeStructureTemplateRepository _repository;

    /// <summary>
    /// Initializes a new instance of the <see cref="ArchiveKnowledgeStructureTemplateHandler"/> class.
    /// </summary>
    /// <param name="repository">The knowledge structure template repository.</param>
    public ArchiveKnowledgeStructureTemplateHandler(IKnowledgeStructureTemplateRepository repository)
    {
        _repository = repository;
    }

    /// <inheritdoc />
    public override async Task<Result> Handle(
        ArchiveKnowledgeStructureTemplateCommand request,
        CancellationToken cancellationToken)
    {
        var template = await _repository.GetByExternalIdAsync(request.ExternalId, cancellationToken);

        if (template is null)
        {
            return Failure(ResultErrorCodes.GenericError,
                ("Plantilla", "La plantilla no fue encontrada."));
        }

        template.Archive();

        await _repository.UnitOfWork.SaveEntitiesAsync(cancellationToken);

        return Success();
    }
}
