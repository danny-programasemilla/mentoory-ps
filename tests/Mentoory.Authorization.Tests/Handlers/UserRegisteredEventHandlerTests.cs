using FluentAssertions;
using Mentoory.Authorization.Application.IntegrationEvents.Handlers;
using Mentoory.Authorization.Domain.ReadModels;
using Mentoory.Authorization.Domain.Repositories;
using Mentoory.Identity.Application.IntegrationEvents;
using Mentoory.Shared.Domain.SeedWork;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Mentoory.Authorization.Tests.Handlers;

public class UserRegisteredEventHandlerTests
{
    private static readonly DateTime UtcNow = new(2026, 1, 15, 10, 0, 0, DateTimeKind.Utc);

    private readonly Mock<IUserProfileRepository> _repo = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly UserRegisteredEventHandler _handler;

    public UserRegisteredEventHandlerTests()
    {
        _repo.Setup(r => r.UnitOfWork).Returns(_unitOfWork.Object);
        _unitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        _handler = new UserRegisteredEventHandler(
            _repo.Object,
            NullLogger<UserRegisteredEventHandler>.Instance);
    }

    [Fact]
    public async Task Handle_WithNewUser_CreatesUserProfile()
    {
        _repo.Setup(r => r.GetByUserIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserProfile?)null);

        var @event = new UserRegisteredEvent(
            UserId: 1,
            UserExternalId: Guid.NewGuid(),
            Email: "test@test.com",
            FirstName: "Juan",
            LastName: "Pérez",
            AccountStatus: "PendingVerification",
            CreatedAtUtc: UtcNow,
            OccurredOnUtc: UtcNow);

        await _handler.Handle(@event, CancellationToken.None);

        _repo.Verify(r => r.Add(It.Is<UserProfile>(p =>
            p.UserId == 1 &&
            p.Email == "test@test.com" &&
            p.FirstName == "Juan" &&
            p.LastName == "Pérez" &&
            p.AccountStatus == "PendingVerification")), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithExistingProfile_SkipsCreation()
    {
        var existingProfile = UserProfile.Create(
            1, Guid.NewGuid(), "test@test.com", "Juan", "Pérez",
            "Active", UtcNow, UtcNow);

        _repo.Setup(r => r.GetByUserIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingProfile);

        var @event = new UserRegisteredEvent(
            UserId: 1,
            UserExternalId: Guid.NewGuid(),
            Email: "test@test.com",
            FirstName: "Juan",
            LastName: "Pérez",
            AccountStatus: "PendingVerification",
            CreatedAtUtc: UtcNow,
            OccurredOnUtc: UtcNow);

        await _handler.Handle(@event, CancellationToken.None);

        _repo.Verify(r => r.Add(It.IsAny<UserProfile>()), Times.Never);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
