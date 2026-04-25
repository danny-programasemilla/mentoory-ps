using Mentoory.Knowledge.Domain.Aggregates.KnowledgeStructureTemplate;
using Mentoory.Knowledge.Domain.Repositories;
using Mentoory.Shared.Application;
using Mentoory.Shared.Application.MediatR;
using Mentoory.Shared.Application.TimeProvider;
using Microsoft.Extensions.Logging;

namespace Mentoory.Knowledge.Application.Commands.CreateKnowledgeStructureTemplate;

/// <summary>
/// Handles the creation of a new knowledge structure template.
/// </summary>
public partial class CreateKnowledgeStructureTemplateHandler
    : BaseCommandHandler<CreateKnowledgeStructureTemplateCommand, Guid>
{
    private readonly IKnowledgeStructureTemplateRepository _repository;
    private readonly ITimeProvider _timeProvider;
    private readonly ILogger<CreateKnowledgeStructureTemplateHandler> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="CreateKnowledgeStructureTemplateHandler"/> class.
    /// </summary>
    /// <param name="repository">The knowledge structure template repository.</param>
    /// <param name="timeProvider">The time provider for obtaining current UTC time.</param>
    /// <param name="logger">The logger instance.</param>
    public CreateKnowledgeStructureTemplateHandler(
        IKnowledgeStructureTemplateRepository repository,
        ITimeProvider timeProvider,
        ILogger<CreateKnowledgeStructureTemplateHandler> logger)
    {
        _repository = repository;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    /// <inheritdoc />
    public override async Task<Result<Guid>> Handle(
        CreateKnowledgeStructureTemplateCommand request,
        CancellationToken cancellationToken)
    {
        var template = KnowledgeStructureTemplate.Create(
            request.Name,
            request.Description,
            _timeProvider.UtcNow);

        _repository.Add(template);
        await _repository.UnitOfWork.SaveEntitiesAsync(cancellationToken);

        LogTemplateCreated(template.ExternalId);

        return Success(template.ExternalId);
    }

    /// <summary>
    /// Logs when a knowledge structure template is successfully created.
    /// </summary>
    [LoggerMessage(Level = LogLevel.Information, Message = "Knowledge structure template {ExternalId} created.")]
    partial void LogTemplateCreated(Guid externalId);
}
