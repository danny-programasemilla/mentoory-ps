/speckit.plan

Create the technical implementation plan for an existing ASP.NET MVC website that must be rebuilt and extended on top of its current solution, preserving the architectural spirit of the existing example code in the HomeController and any existing demonstrations of Clean Architecture and Domain-Driven Design.

This plan must be written for Claude and should be detailed, practical, and implementation-ready, but it must avoid overengineering. Favor simplicity without sacrificing quality, security, maintainability, scalability, or testability.

## Core product and architecture direction

The system must use:

- ASP.NET MVC
- SQL Server as the database
- .NET Aspire compatibility across the solution
- GitHub CI/CD pipelines for build, test, and deployment automation

The authentication and identity capabilities must be built in-house. Do not use an external authentication library or external identity provider as the primary authentication module. I want an internal authentication domain with security capabilities comparable in seriousness to enterprise-grade identity systems.

The implementation plan must define an authentication domain that includes, at minimum:

- Secure login and logout
- Session management
- Configurable inactivity timeout with automatic logout
- Account activation and deactivation
- Account status handling such as active, inactive, locked, disabled, password reset required, etc.
- Strong password policies aligned with industry best practices
- Password hashing and secure credential storage
- Password reset and recovery flows
- Auditability of authentication-relevant events
- Role-based authorization
- Secure menu and feature visibility by role
- Extensible design so any domain could later be extracted into a microservice if needed

Do not introduce unnecessary complexity. Keep the design modular and disciplined, but practical.

## UI/UX direction

The UI must be modern, elegant, simple, and maintainable.

I want the implementation plan to propose a UI foundation for an admin-style website with:

- A left-side navigation menu
- Menu options shown dynamically based on the user’s role and permissions
- A design that starts simple but scales well
- A menu model that avoids duplicating whole menus per role
- Support for sharing menu options across multiple roles
- Clean, reusable components
- Consistent handling of authenticated session state in the UI

The current site’s existing styles, controllers, and libraries may be replaced entirely if necessary. You may propose rebuilding the UI foundation from scratch if that is the best path.

For listing-heavy pages:

- Avoid repetitive code
- Use reusable list, table, filter, and pagination patterns
- Prefer dynamic and asynchronous interactions rather than full-page reloads
- Pagination, filtering, and sorting should happen with asynchronous JavaScript behavior where appropriate
- Prefer vanilla JavaScript as the default approach
- If a carefully selected admin template or component approach uses jQuery and it is justified, that is acceptable
- Recommend a concrete admin template or UI foundation that is appropriate for ASP.NET MVC and this type of back-office site
- Explain whether Areas, feature folders, modular MVC organization, or another structure is most appropriate

## Architectural expectations

Study and preserve the intent of the current solution’s architectural example, especially how the HomeController interacts with different domains.

The implementation plan must preserve and strengthen:

- Clean Architecture principles
- Domain-Driven Design principles
- Clear domain boundaries
- A modular monolith structure where each domain is separable and could later become an independent microservice
- Explicit application, domain, infrastructure, and presentation responsibilities
- Clear dependency direction
- No overengineering
- No accidental coupling between domains
- Reusable cross-cutting patterns only where justified

The plan should identify the likely domains and clearly explain why each domain boundary exists.

At a minimum, evaluate domains such as:

- Identity / Authentication
- Authorization / Roles / Permissions
- User Administration
- Navigation / Menu Configuration
- Auditing
- Core business domains inferred from the existing site structure
- Shared kernel or cross-cutting concerns only if truly needed

## Engineering quality expectations

The solution must include strong automated testing.

I want the plan to define a testing strategy that includes:

- Unit tests
- Integration tests where needed
- Application/service-level tests where needed
- End-to-end UI tests with Playwright if appropriate
- A dedicated test project structure, since this does not currently exist
- Automated execution of tests on every relevant change
- Sufficient regression protection so changes do not silently break other areas

Bias toward automated tests rather than manual-only verification.

## Delivery and DevOps expectations

The plan must include:

- GitHub Actions pipelines for CI/CD
- Build, test, lint/quality checks, and deployment stages as appropriate
- Compatibility with Aspire-based orchestration and local developer workflows
- Environment strategy for local, test, staging, and production
- Secret/configuration handling recommendations
- Database migration/deployment strategy for SQL Server
- Rollback or safe deployment considerations appropriate for this type of site

## Important constraints

- Do not use an external authentication module as the core auth solution
- Use SQL Server
- Use ASP.NET MVC
- Maintain compatibility with .NET Aspire
- Favor vanilla JavaScript where practical
- Accept jQuery only when justified by the selected template/components
- Avoid overengineering
- Favor modularity and extraction-readiness at the domain level
- Maintain high security and maintainability standards
- Prioritize reusable UI building blocks for list-heavy screens
- The current implementation may be partially or fully replaced if needed

## What I want in the plan output

Produce a detailed implementation plan that includes:

1. Executive summary of the target architecture
2. Recommended solution structure
3. Domain decomposition and bounded context definitions
4. Authentication domain design, including entities, workflows, security controls, and session lifecycle
5. Authorization and dynamic menu strategy
6. UI architecture and recommended admin template or component foundation
7. Frontend interaction strategy for asynchronous tables, filters, pagination, and reusable components
8. Data access and SQL Server persistence strategy
9. Testing strategy and proposed test project layout
10. CI/CD and GitHub Actions pipeline design
11. Aspire compatibility considerations
12. Migration strategy from the current site to the new structure
13. Risks, tradeoffs, and decisions that should be validated early
14. A phased implementation approach that starts simple but supports future scaling

Where useful, also generate or prepare supporting artifacts expected from the planning phase, including research, data model ideas, contracts, quickstart validation scenarios, and test scenario guidance.

Be opinionated and practical. Recommend concrete patterns, structure, and technologies rather than staying generic. When there are multiple reasonable options, choose one and justify it briefly.