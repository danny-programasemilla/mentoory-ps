using FluentAssertions;
using Mentoory.Shared.Application;
using Mentoory.Tenant.Application.Queries.GetIncubatorByExternalId;
using Mentoory.Tenant.Domain.Aggregates.Incubator;
using Mentoory.Tenant.Domain.Repositories;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Mentoory.Tenant.Tests.Handlers;

public class GetIncubatorByExternalIdHandlerTests
{
    private static readonly DateTime UtcNow = new(2026, 1, 15, 10, 0, 0, DateTimeKind.Utc);

    private readonly Mock<IIncubatorRepository> _incubatorRepo = new();

    [Fact]
    public async Task Handle_WhenIncubatorIdMismatch_ReturnsFailure()
    {
        var incubator = Incubator.Create("Test Incubator", null, UtcNow);
        SetEntityId(incubator, 1);
        _incubatorRepo.Setup(r => r.GetByExternalIdAsync(incubator.ExternalId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(incubator);

        var handler = new GetIncubatorByExternalIdHandler(
            NullLogger<GetIncubatorByExternalIdHandler>.Instance,
            _incubatorRepo.Object);

        var result = await handler.Handle(
            new GetIncubatorByExternalIdQuery(incubator.ExternalId, 999L), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.ErrorCode.Should().Be(ResultErrorCodes.GenericError);
        result.ErrorMessages.Should().Contain(m => m.Message.Contains("autorización"));
    }

    [Fact]
    public async Task Handle_WhenIncubatorIdMatches_ReturnsSuccess()
    {
        var incubator = Incubator.Create("Test Incubator", null, UtcNow);
        SetEntityId(incubator, 1);
        _incubatorRepo.Setup(r => r.GetByExternalIdAsync(incubator.ExternalId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(incubator);

        var handler = new GetIncubatorByExternalIdHandler(
            NullLogger<GetIncubatorByExternalIdHandler>.Instance,
            _incubatorRepo.Object);

        var result = await handler.Handle(
            new GetIncubatorByExternalIdQuery(incubator.ExternalId, 1L), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Name.Should().Be("Test Incubator");
    }

    [Fact]
    public async Task Handle_WhenCallerIncubatorIdIsNull_ReturnsSuccess()
    {
        var incubator = Incubator.Create("Test Incubator", null, UtcNow);
        SetEntityId(incubator, 1);
        _incubatorRepo.Setup(r => r.GetByExternalIdAsync(incubator.ExternalId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(incubator);

        var handler = new GetIncubatorByExternalIdHandler(
            NullLogger<GetIncubatorByExternalIdHandler>.Instance,
            _incubatorRepo.Object);

        var result = await handler.Handle(
            new GetIncubatorByExternalIdQuery(incubator.ExternalId, null), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
    }

    private static void SetEntityId(Mentoory.Shared.Domain.SeedWork.Entity entity, long id)
    {
        typeof(Mentoory.Shared.Domain.SeedWork.Entity)
            .GetProperty(nameof(Mentoory.Shared.Domain.SeedWork.Entity.Id))!
            .SetValue(entity, id);
    }
}
