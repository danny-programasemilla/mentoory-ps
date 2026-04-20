# Data Model / Code Contracts: Cross-cutting Hardening — Phase 0

**Spec**: [spec.md](./spec.md) | **Plan**: [plan.md](./plan.md)

This feature introduces no database entities. It introduces **code contracts** (types, interfaces, constants) consumed across the solution. This document records the exact shape of each new or modified contract so implementation is unambiguous.

## C-1 `Roles` (unchanged — reference only)

**Location**: `Mentoory.Shared.Domain/Constants/Roles.cs`
**Status**: Exists today. No modifications.

```csharp
namespace Mentoory.Shared.Domain.Constants;

public static class Roles
{
    public const string GlobalAdmin = "GlobalAdmin";
    public const string IncubatorAdmin = "IncubatorAdmin";
    public const string ProjectCoordinator = "ProjectCoordinator";
    public const string Mentor = "Mentor";
    public const string Entrepreneur = "Entrepreneur";
    public const string Sponsor = "Sponsor";

    public static readonly IReadOnlyList<string> All = new[]
    {
        GlobalAdmin, IncubatorAdmin, ProjectCoordinator, Mentor, Entrepreneur, Sponsor,
    };
}
```

## C-2 `RoleHierarchies` (NEW — QW-1)

**Location**: `Mentoory.Shared.Domain/Constants/RoleHierarchies.cs`
**Purpose**: Pre-built comma-joined role strings for `[Authorize(Roles = ...)]` and menu configuration. Each hierarchy includes every strictly higher role per constitution §X.

```csharp
namespace Mentoory.Shared.Domain.Constants;

public static class RoleHierarchies
{
    // GlobalAdmin only.
    public const string PlatformScope = Roles.GlobalAdmin;

    // IncubatorAdmin + higher.
    public const string IncubatorScope = Roles.IncubatorAdmin + "," + Roles.GlobalAdmin;

    // ProjectCoordinator + higher.
    public const string ProjectScope =
        Roles.ProjectCoordinator + "," + Roles.IncubatorAdmin + "," + Roles.GlobalAdmin;

    // Mentor + higher.
    public const string MentoringScope =
        Roles.Mentor + "," + Roles.ProjectCoordinator + ","
        + Roles.IncubatorAdmin + "," + Roles.GlobalAdmin;

    // Entrepreneur + higher.
    public const string ParticipantScope =
        Roles.Entrepreneur + "," + Roles.Mentor + "," + Roles.ProjectCoordinator + ","
        + Roles.IncubatorAdmin + "," + Roles.GlobalAdmin;

    // Sponsor + IncubatorAdmin + GlobalAdmin (Sponsor is a read-only peer, not in the main hierarchy).
    public const string SponsorScope =
        Roles.Sponsor + "," + Roles.IncubatorAdmin + "," + Roles.GlobalAdmin;
}
```

**Attribute-expansion note**: `const string` concatenation is a compile-time constant in C#, so these are usable in `[Authorize(Roles = RoleHierarchies.ProjectScope)]`.

## C-3 `IDomainEvent` (NEW — QW-2)

**Location**: `Mentoory.Shared.Domain/SeedWork/IDomainEvent.cs`
**Purpose**: Framework-free marker for events emitted by aggregates. Replaces the direct `MediatR.INotification` inheritance in `Entity.cs`.

```csharp
namespace Mentoory.Shared.Domain.SeedWork;

public interface IDomainEvent
{
}
```

## C-4 `DomainEventNotification<TEvent>` (NEW — QW-2)

**Location**: `Mentoory.Shared.Application/DomainEvents/DomainEventNotification.cs`
**Purpose**: MediatR-side adapter that wraps any `IDomainEvent` as `INotification` for publish-time dispatch.

```csharp
using MediatR;
using Mentoory.Shared.Domain.SeedWork;

namespace Mentoory.Shared.Application.DomainEvents;

public sealed record DomainEventNotification<TEvent>(TEvent DomainEvent) : INotification
    where TEvent : IDomainEvent;
```

**Handler subscription pattern**:

```csharp
public sealed class UserRegisteredEventHandler
    : INotificationHandler<DomainEventNotification<UserRegisteredEvent>>
{
    public Task Handle(
        DomainEventNotification<UserRegisteredEvent> notification,
        CancellationToken cancellationToken)
    {
        var domainEvent = notification.DomainEvent;
        // ...
    }
}
```

## C-5 `Entity.cs` (MODIFIED — QW-2)

**Location**: `Mentoory.Shared.Domain/SeedWork/Entity.cs`
**Before** (relevant lines only):

```csharp
using MediatR;

public abstract class Entity
{
    private List<INotification> _domainEvents;
    public IReadOnlyCollection<INotification> DomainEvents => _domainEvents?.AsReadOnly();
    public void AddDomainEvent(INotification eventItem) { ... }
    public void RemoveDomainEvent(INotification eventItem) { ... }
    public void ClearDomainEvents() { ... }
}
```

**After**:

```csharp
// No using MediatR;
using Mentoory.Shared.Domain.SeedWork;  // already in this namespace

public abstract class Entity
{
    private List<IDomainEvent> _domainEvents;
    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents?.AsReadOnly();
    public void AddDomainEvent(IDomainEvent eventItem) { ... }
    public void RemoveDomainEvent(IDomainEvent eventItem) { ... }
    public void ClearDomainEvents() { ... }
}
```

## C-6 `SharedAbstractDbContext.DispatchDomainEventsAsync` (MODIFIED — QW-2)

**Location**: `Mentoory.Shared.Infrastructure/Persistence/SharedAbstractDbContext.cs`
**Change**: when iterating tracked entities and their `.DomainEvents`, each `IDomainEvent` MUST be wrapped in `DomainEventNotification<TEvent>` before publishing via `IPublisher`. Use reflection *only* at this single site to construct the closed generic `DomainEventNotification<T>` — this is the **one acceptable use of reflection** because there is no compile-time knowledge of the concrete event type at dispatch.

Sketch:

```csharp
foreach (var domainEvent in entity.DomainEvents)
{
    var notificationType = typeof(DomainEventNotification<>)
        .MakeGenericType(domainEvent.GetType());
    var notification = Activator.CreateInstance(notificationType, domainEvent)!;
    await publisher.Publish(notification, cancellationToken);
}
```

**Performance note**: `MakeGenericType` + `Activator.CreateInstance` cost is bounded by the number of distinct event types per request. A per-DbContext cache is acceptable if CP-6 shows measurable overhead, but this optimisation is NOT required in the initial implementation — premature optimisation violates YAGNI.

## C-7 `ITenantContextWriter` (NEW — QW-3)

**Location**: `Mentoory.Shared.Application/Interfaces/ITenantContextWriter.cs`

```csharp
namespace Mentoory.Shared.Application.Interfaces;

public interface ITenantContextWriter
{
    void SetCurrentIncubatorId(long? incubatorId);
}
```

## C-8 `TenantContextService` (MODIFIED — QW-3)

**Location**: `Mentoory.Shared.Infrastructure/Services/TenantContextService.cs`
**Change**: implement both `ITenantContext` and `ITenantContextWriter`. The setter MUST accept `null` (required for GlobalAdmin global-scope operation per edge case E-1).

```csharp
public sealed class TenantContextService : ITenantContext, ITenantContextWriter
{
    public long? CurrentIncubatorId { get; private set; }
    public void SetCurrentIncubatorId(long? incubatorId) => CurrentIncubatorId = incubatorId;
}
```

**DI registration** (in `Mentoory.Web/Program.cs` or relevant extension method):

```csharp
services.AddScoped<TenantContextService>();
services.AddScoped<ITenantContext>(sp => sp.GetRequiredService<TenantContextService>());
services.AddScoped<ITenantContextWriter>(sp => sp.GetRequiredService<TenantContextService>());
```

## C-9 `TenantContextMiddleware` (MODIFIED — QW-3)

**Location**: `Mentoory.Web/Infrastructure/Authorization/TenantContextMiddleware.cs`
**Change**: inject `ITenantContextWriter` and call it directly; remove the `is TenantContextService` cast.

```csharp
public async Task InvokeAsync(HttpContext context, ITenantContextWriter writer)
{
    if (context.User.Identity?.IsAuthenticated == true)
    {
        var incubatorId = context.User.GetActiveIncubatorIdOrNull();
        writer.SetCurrentIncubatorId(incubatorId);
    }
    await _next(context);
}
```

## C-10 `ResultValidatorBehavior<TRequest>` (NEW — QW-5)

**Location**: `Mentoory.Shared.Application/Behaviors/ResultValidatorBehavior.cs`

```csharp
public sealed class ResultValidatorBehavior<TRequest>
    : IPipelineBehavior<TRequest, Result>
    where TRequest : IBaseRequest
{
    private readonly IEnumerable<IValidator<TRequest>> _validators;

    public ResultValidatorBehavior(IEnumerable<IValidator<TRequest>> validators)
        => _validators = validators;

    public async Task<Result> Handle(
        TRequest request,
        RequestHandlerDelegate<Result> next,
        CancellationToken cancellationToken)
    {
        if (!_validators.Any()) return await next();

        var context = new ValidationContext<TRequest>(request);
        var results = await Task.WhenAll(_validators.Select(v => v.ValidateAsync(context, cancellationToken)));
        var failures = results.SelectMany(r => r.Errors).Where(f => f is not null).ToList();
        if (failures.Count == 0) return await next();

        var errors = failures.Select(f => (f.PropertyName, f.ErrorMessage)).ToArray();
        return Result.Failure(ErrorCode.Validation_SomeFieldsAreInvalid, errors);
    }
}
```

## C-11 `ResultTValidatorBehavior<TRequest, TResponse>` (NEW — QW-5)

**Location**: `Mentoory.Shared.Application/Behaviors/ResultTValidatorBehavior.cs`

```csharp
public sealed class ResultTValidatorBehavior<TRequest, TResponse>
    : IPipelineBehavior<TRequest, Result<TResponse>>
    where TRequest : IBaseRequest<TResponse>
{
    private readonly IEnumerable<IValidator<TRequest>> _validators;

    public ResultTValidatorBehavior(IEnumerable<IValidator<TRequest>> validators)
        => _validators = validators;

    public async Task<Result<TResponse>> Handle(
        TRequest request,
        RequestHandlerDelegate<Result<TResponse>> next,
        CancellationToken cancellationToken)
    {
        if (!_validators.Any()) return await next();

        var context = new ValidationContext<TRequest>(request);
        var results = await Task.WhenAll(_validators.Select(v => v.ValidateAsync(context, cancellationToken)));
        var failures = results.SelectMany(r => r.Errors).Where(f => f is not null).ToList();
        if (failures.Count == 0) return await next();

        var errors = failures.Select(f => (f.PropertyName, f.ErrorMessage)).ToArray();
        return Result<TResponse>.Failure(ErrorCode.Validation_SomeFieldsAreInvalid, errors);
    }
}
```

**DI registration** (replacing the single reflection-based behavior):

```csharp
services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ResultValidatorBehavior<>));
services.AddTransient(typeof(IPipelineBehavior<,,>), typeof(ResultTValidatorBehavior<,>));
```

Note: MediatR's `IPipelineBehavior` is always two-parameter; the "non-generic Result" vs "generic Result<T>" distinction is achieved through the `TResponse` type parameter constraint (pinned by the `where TRequest : IBaseRequest` clause which makes `TResponse = Result`).

## C-12 `RegisterUserHandler` error-payload change (MODIFIED — QW-7)

**Location**: `Mentoory.Access.Application/Commands/RegisterUser/RegisterUserHandler.cs`
**Before**:

```csharp
if (existingByEmail is not null)
    return Failure(ErrorCode.Email_AlreadyRegistered,
        ("Email", "Ya existe una cuenta con este correo electrónico."));

if (existingByNationalId is not null)
    return Failure(ErrorCode.NationalId_AlreadyRegistered,
        ("NationalId", "Ya existe una cuenta con esta cédula."));
```

**After**:

```csharp
if (existingByEmail is not null || existingByNationalId is not null)
    return Failure(ErrorCode.Registration_Conflict,
        (string.Empty, "No se pudo completar el registro. Verifica tus datos."));
```

A single `ErrorCode` and a field-agnostic error tuple. `ErrorCode.Registration_Conflict` is added to the existing error-code enum. The previous two codes (`Email_AlreadyRegistered`, `NationalId_AlreadyRegistered`) remain in the enum for any other consumers; if none exist, they MAY be removed — implementation decides after grep.

## C-13 Architecture test project (NEW — QW-4)

**Location**: `tests/Mentoory.Tests.Architecture/`

**csproj shape**:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <IsPackable>false</IsPackable>
    <IsTestProject>true</IsTestProject>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="xunit" />
    <PackageReference Include="xunit.runner.visualstudio" />
    <PackageReference Include="Microsoft.NET.Test.Sdk" />
    <PackageReference Include="FluentAssertions" />
    <PackageReference Include="NetArchTest.Rules" />
  </ItemGroup>
  <ItemGroup>
    <!-- Reference every module so reflection can load their assemblies -->
    <ProjectReference Include="..\..\Mentoory.Shared.Domain\Mentoory.Shared.Domain.csproj" />
    <ProjectReference Include="..\..\Mentoory.Shared.Application\Mentoory.Shared.Application.csproj" />
    <ProjectReference Include="..\..\Mentoory.Shared.Infrastructure\Mentoory.Shared.Infrastructure.csproj" />
    <!-- ... plus every {Module}.{Domain|Application|Infrastructure} csproj -->
  </ItemGroup>
</Project>
```

Add a corresponding `<PackageVersion Include="NetArchTest.Rules" Version="..." />` entry to `Directory.Packages.props` (latest stable).

**Rule skeleton** (one `[Fact]` per rule; contracts in `contracts/`):

```csharp
public class LayerBoundaryTests
{
    [Fact]
    public void Domain_MustNotDependOn_MediatR_EF_AspNet_FluentValidation()
    {
        var result = Types.InAssembliesMatching("Mentoory.*.Domain")
            .ShouldNot().HaveDependencyOnAny(
                "MediatR", "Microsoft.EntityFrameworkCore", "Microsoft.AspNetCore", "FluentValidation")
            .GetResult();
        result.IsSuccessful.Should().BeTrue(
            because: string.Join(", ", result.FailingTypeNames ?? Array.Empty<string>()));
    }
    // ... R-4.2.3, R-4.2.4, R-4.2.5, R-4.2.6, R-4.2.7 in similar shape
}

public class NoDateTimeUtcNowInDomainTests  // R-4.2.2 — source-grep fallback
{
    [Fact]
    public void No_DateTime_UtcNow_Or_Now_In_Domain_Or_Application_Source()
    {
        var offenders = new List<string>();
        foreach (var file in EnumerateSourceFiles("Mentoory.*.Domain", "Mentoory.*.Application"))
        {
            var content = StripCommentsAndStrings(File.ReadAllText(file));
            if (content.Contains("DateTime.UtcNow", StringComparison.Ordinal)
                || content.Contains("DateTime.Now", StringComparison.Ordinal))
            {
                offenders.Add(file);
            }
        }
        offenders.Should().BeEmpty();
    }
}

public class NoRoleLiteralsOutsideRolesTests  // R-4.2.8
{
    [Fact]
    public void Role_String_Literals_MustNot_Appear_Outside_Roles_Cs()
    {
        var forbidden = new[] { "\"GlobalAdmin\"", "\"IncubatorAdmin\"", "\"ProjectCoordinator\"",
                                "\"Mentor\"", "\"Entrepreneur\"", "\"Sponsor\"" };
        var offenders = EnumerateSourceFiles("Mentoory.*")
            .Where(f => !f.EndsWith("Roles.cs", StringComparison.OrdinalIgnoreCase))
            .Where(f => !f.Contains(".Generated.", StringComparison.OrdinalIgnoreCase))
            .Where(f => !f.Contains("/bin/") && !f.Contains("/obj/"))
            .Where(f => forbidden.Any(lit => File.ReadAllText(f).Contains(lit, StringComparison.Ordinal)))
            .ToList();
        offenders.Should().BeEmpty();
    }
}
```

## References

- Constitution §I, §V, §X
- Spec FR-001 through FR-022
- Plan CP-1 through CP-7
