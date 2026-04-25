using System.Collections.Concurrent;
using System.Reflection;
using MediatR;
using Mentoory.Shared.Application.Audit;
using Mentoory.Shared.Application.Interfaces;
using Mentoory.Shared.Application.TimeProvider;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Mentoory.Shared.Application.Behaviors;

/// <summary>
/// MediatR pipeline behavior that captures one <see cref="AuditEntry"/> per command
/// decorated with <see cref="AuditedAttribute"/> in Automatic mode. Manual mode is a
/// passthrough — the handler owns the audit write.
/// Registration order is <c>Validator → Auditing → Transaction</c>: audit observes the
/// transaction outcome and writes OUTSIDE of it, preserving best-effort semantics.
/// </summary>
public sealed class AuditingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private static readonly ConcurrentDictionary<Type, AuditedAttribute?> AttributeCache = new();

    private readonly IAuditService _auditService;
    private readonly ITenantContext _tenantContext;
    private readonly ICorrelationContext _correlationContext;
    private readonly ITimeProvider _timeProvider;
    private readonly AuditOptions _options;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<AuditingBehavior<TRequest, TResponse>> _logger;

    public AuditingBehavior(
        IAuditService auditService,
        ITenantContext tenantContext,
        ICorrelationContext correlationContext,
        ITimeProvider timeProvider,
        IOptions<AuditOptions> options,
        IServiceProvider serviceProvider,
        ILogger<AuditingBehavior<TRequest, TResponse>> logger)
    {
        _auditService = auditService;
        _tenantContext = tenantContext;
        _correlationContext = correlationContext;
        _timeProvider = timeProvider;
        _options = options.Value;
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var attribute = AttributeCache.GetOrAdd(typeof(TRequest), static t => t.GetCustomAttribute<AuditedAttribute>(inherit: false));

        if (attribute is null || attribute.Mode == AuditMode.Manual)
        {
            return await next(cancellationToken);
        }

        TResponse? response = default;
        Exception? capturedException = null;
        try
        {
            response = await next(cancellationToken);
            return response;
        }
        catch (Exception ex)
        {
            capturedException = ex;
            throw;
        }
        finally
        {
            await WriteAuditEntryAsync(request, attribute, response, capturedException, cancellationToken);
        }
    }

    private static string DetermineOutcome(TResponse? response, Exception? exception)
    {
        if (exception is not null)
        {
            return "Failure";
        }

        return response is Result result && !result.IsSuccess ? "Failure" : "Success";
    }

    private async Task WriteAuditEntryAsync(
        TRequest request,
        AuditedAttribute attribute,
        TResponse? response,
        Exception? exception,
        CancellationToken cancellationToken)
    {
        var outcome = DetermineOutcome(response, exception);
        var (userId, userEmail) = ResolveUserFromAnonymousContext(request);

        var entry = new AuditEntry(
            EventType: attribute.EventType,
            UserId: userId ?? _tenantContext.UserId,
            IncubatorId: _tenantContext.IncubatorId,
            ProjectId: _tenantContext.ProjectId,
            EntityType: attribute.EntityType,
            EntityId: null,
            Action: typeof(TRequest).Name,
            Details: AuditPayloadRedactor.SerializeRedacted(request!, _options, _logger),
            IpAddress: _correlationContext.ClientIpAddress,
            OccurredAtUtc: _timeProvider.UtcNow,
            CorrelationId: _correlationContext.CorrelationId,
            Outcome: outcome,
            ExceptionType: exception?.GetType().FullName,
            UserEmail: userEmail ?? _tenantContext.UserEmail,
            RoleContext: _tenantContext.Role);

        // Best-effort: a failing audit MUST NOT break the wrapped command. The
        // AuditService implementation already swallows its own exceptions; this
        // try/catch is defense-in-depth so any future implementation (or a test
        // double) cannot accidentally propagate into the command's return path.
        try
        {
            await _auditService.LogAsync(entry, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "AuditingBehavior swallowed an audit-service failure for {RequestType}",
                typeof(TRequest).Name);
        }
    }

    private (long? UserId, string? UserEmail) ResolveUserFromAnonymousContext(TRequest request)
    {
        var resolverType = typeof(IAuditAnonymousResolver<>).MakeGenericType(typeof(TRequest));
        var resolver = _serviceProvider.GetService(resolverType);
        if (resolver is null)
        {
            return (null, null);
        }

        var resolveMethod = resolverType.GetMethod("Resolve")!;
        var result = resolveMethod.Invoke(resolver, [request]);
        return ((long? UserId, string? UserEmail))result!;
    }
}
