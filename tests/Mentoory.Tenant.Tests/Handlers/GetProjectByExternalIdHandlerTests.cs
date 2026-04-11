using FluentAssertions;
using Mentoory.Shared.Application;
using Mentoory.Tenant.Application.Queries.GetProjectByExternalId;
using Mentoory.Tenant.Domain.Aggregates.Project;
using Mentoory.Tenant.Domain.Repositories;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Mentoory.Tenant.Tests.Handlers;

public class GetProjectByExternalIdHandlerTests
{
    private static readonly DateTime UtcNow = new(2026, 1, 15, 10, 0, 0, DateTimeKind.Utc);

    private readonly Mock<IProjectRepository> _projectRepo = new();

    [Fact]
    public async Task Handle_WhenIncubatorIdMismatch_ReturnsFailure()
    {
        var project = Project.Create(1, "Test Project", null, UtcNow);
        _projectRepo.Setup(r => r.GetByExternalIdAsync(project.ExternalId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(project);

        var handler = new GetProjectByExternalIdHandler(
            NullLogger<GetProjectByExternalIdHandler>.Instance,
            _projectRepo.Object);

        var result = await handler.Handle(
            new GetProjectByExternalIdQuery(project.ExternalId, 999L), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.ErrorCode.Should().Be(ResultErrorCodes.GenericError);
        result.ErrorMessages.Should().Contain(m => m.Message.Contains("autorización"));
    }

    [Fact]
    public async Task Handle_WhenIncubatorIdMatches_ReturnsSuccess()
    {
        var project = Project.Create(1, "Test Project", null, UtcNow);
        _projectRepo.Setup(r => r.GetByExternalIdAsync(project.ExternalId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(project);

        var handler = new GetProjectByExternalIdHandler(
            NullLogger<GetProjectByExternalIdHandler>.Instance,
            _projectRepo.Object);

        var result = await handler.Handle(
            new GetProjectByExternalIdQuery(project.ExternalId, 1L), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Name.Should().Be("Test Project");
    }

    [Fact]
    public async Task Handle_WhenCallerIncubatorIdIsNull_ReturnsSuccess()
    {
        var project = Project.Create(1, "Test Project", null, UtcNow);
        _projectRepo.Setup(r => r.GetByExternalIdAsync(project.ExternalId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(project);

        var handler = new GetProjectByExternalIdHandler(
            NullLogger<GetProjectByExternalIdHandler>.Instance,
            _projectRepo.Object);

        var result = await handler.Handle(
            new GetProjectByExternalIdQuery(project.ExternalId, null), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
    }
}
