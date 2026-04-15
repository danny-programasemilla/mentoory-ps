# Research: Notification Generic Delivery

**Date**: 2026-04-14

## R1: RazorLight Multi-Assembly Template Rendering

**Decision**: Each domain hosts its own RazorLight instance configured to read embedded resources from its own assembly.

**Rationale**: RazorLight's `UseEmbeddedResourcesProject(type)` accepts a type from the target assembly to locate embedded resources. Each domain's Infrastructure project can independently register a `RazorLightTemplateRenderer` scoped to its own assembly. This is already the pattern used by `Mentoory.Notification.Infrastructure` — it just needs to be replicated in Access.Infrastructure and Tenant.Infrastructure.

**Alternatives considered**:
- Shared RazorLight instance with multi-assembly resource loading: Requires custom `RazorLightProject` implementation, adds complexity with no benefit.
- File-based templates instead of embedded resources: Deployment overhead, file path management, security concerns. Embedded resources are simpler and already proven.

## R2: Shared Email Layout Approach

**Decision**: Convert `_EmailLayout.cshtml` from a Razor layout template into a simple `IEmailLayoutWrapper` service that wraps inner HTML content with the brand layout via string interpolation.

**Rationale**: Razor `@{ Layout = ... }` only works when layout and content templates are in the same RazorLight project. Since templates now span multiple assemblies, the layout cannot be referenced via Razor's built-in mechanism. A simple wrapper service in `Mentoory.Shared.Infrastructure` that takes the inner HTML string and wraps it in the header/footer HTML is sufficient. The layout is static HTML (no Razor logic) — it just needs string interpolation.

**Alternatives considered**:
- Copy `_EmailLayout.cshtml` to every domain: Violates DRY, leads to branding drift.
- Shared Razor template via NuGet package or shared assembly: Overengineered for a static HTML wrapper.
- Razor partial rendering with cross-assembly includes: RazorLight doesn't support this natively.

## R3: ITemplateRenderer Interface Location

**Decision**: Move `ITemplateRenderer` to `Mentoory.Shared.Application` since multiple domains need it.

**Rationale**: Clean Architecture requires interfaces (ports) in the Application layer. Since Access, Tenant, and potentially other future domains all need template rendering, the interface belongs in the shared Application project. Each domain's Infrastructure project provides its own `RazorLightTemplateRenderer` implementation registered in DI.

**Alternatives considered**:
- Keep in each domain's Application layer: Would create duplicate interfaces with identical signatures.
- Keep in Notification.Infrastructure: Violates Clean Architecture (Application handlers can't reference Infrastructure).

## R4: NotificationType Enum Accessibility

**Decision**: `NotificationType` enum stays in `Mentoory.Notification.Domain`. The `Mentoory.Notification.Contracts` project references `Mentoory.Notification.Domain` (or the enum is duplicated/referenced via Notification.Contracts).

**Rationale**: `NotificationRequestedEvent` carries a `NotificationType` field. Originating domains that publish this event need access to the enum. The simplest approach: `Notification.Contracts` references `Notification.Domain` for the enum, and originating domains reference `Notification.Contracts`. This follows the established pattern — `Access.Contracts` references `Shared.Application` for the `IntegrationEvent` base class.

**Alternatives considered**:
- Move enum to Shared.Domain: Notification-specific concept shouldn't live in shared code.
- Use raw byte/int in the event: Loses type safety.
- Duplicate enum in Contracts: Risks drift between domain and contract definitions.

## R5: Template Removal from Individual Templates

**Decision**: Remove `@{ Layout = "_EmailLayout.cshtml"; }` from all template files when moving them. Templates render only inner content. The `IEmailLayoutWrapper` handles wrapping.

**Rationale**: Since the layout is no longer a Razor template, individual templates should not reference it. The rendering flow becomes: (1) RazorLight renders inner content, (2) `IEmailLayoutWrapper.WrapInBrandLayout(innerHtml)` wraps it.

## R6: Notification.Contracts Project Dependencies

**Decision**: `Mentoory.Notification.Contracts` references `Mentoory.Shared.Application` (for `IntegrationEvent` base class) and `Mentoory.Notification.Domain` (for `NotificationType` enum).

**Rationale**: Follows the exact same pattern as `Mentoory.Access.Contracts` which references `Mentoory.Shared.Application`. Adding the `Notification.Domain` reference is needed for `NotificationType`. This is acceptable because Contracts projects are lightweight — they only contain event records and possibly constants.
