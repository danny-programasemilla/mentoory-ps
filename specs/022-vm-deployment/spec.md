# Feature Specification: Single Fixed-Cost VM Deployment

**Feature Branch**: `022-vm-deployment`

**Created**: 2026-06-06

**Status**: Draft

**Input**: User description: "Single fixed-cost Azure VM deployment for Mentoory, replicating the methodology used in bds-ps/deploy/vm. Replace/augment the current Aspire-on-Azure-SQL deployment approach with a self-contained Docker Compose stack on one Linux VM, so the monthly bill is fixed and predictable instead of usage-billed."

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Stand up the app on a fixed-cost VM and reach it over HTTPS (Priority: P1)

An operator with an Azure subscription and the `az` CLI runs the provided provisioning and deploy steps from their dev machine and, within one sitting, reaches the running Mentoory application over HTTPS at their own domain — backed by a self-contained SQL Server running on the same VM with the MentooryDb schema published. The monthly cost is fixed and predictable regardless of traffic, with no Azure Container Apps and no Azure SQL.

**Why this priority**: This is the core value — a complete, working, fixed-cost deployment. Without it nothing else matters. It is the MVP: if only this story ships, the operator already has a production-reachable app at a predictable price.

**Independent Test**: From a clean subscription, run the provisioning script, point DNS at the printed IP, configure secrets on the VM, and run the deploy step. Verify the app loads over HTTPS with a valid certificate, login works, and a Spanish UI renders — all without the Aspire AppHost, Azure Container Apps, or Azure SQL existing.

**Acceptance Scenarios**:

1. **Given** a clean Azure subscription with the operator logged into `az`, **When** the VM provisioning step runs, **Then** a resource group (created if missing), a Linux VM, a static public IP, and a network security group are created; web ports (80/443) are open to the internet; SSH (22) is restricted to the operator's current public IP; and the VM's public IP is printed.
2. **Given** the VM is provisioned and a public DNS A record resolves the chosen domain to the VM IP, **When** the operator configures secrets on the VM and runs the deploy step, **Then** the reverse proxy obtains a valid TLS certificate automatically and the application is reachable over HTTPS at the domain.
3. **Given** the application container is running, **When** a user opens the site, **Then** the existing Mentoory features work (login, navigation, Spanish UI) exactly as before, served by the in-VM SQL Server with the MentooryDb schema applied.
4. **Given** no Aspire AppHost is present, **When** the application container starts, **Then** it reads all required configuration from environment variables and appsettings (including the database connection) and starts healthy without requiring AppHost-injected configuration.
5. **Given** the deployment is fully provisioned, **When** the operator reviews the documented cost breakdown, **Then** the recurring monthly cost is fixed (default baseline ~$40/month) and does not vary with request volume.

---

### User Story 2 - Ship code and schema updates repeatably and safely (Priority: P2)

After the initial deployment, the operator pushes application code changes and database schema changes with a single, idempotent command from their dev machine, repeatedly and safely, without ever disturbing the VM's secrets or taking the site down unexpectedly.

**Why this priority**: A deployment that can't be updated cleanly is operationally fragile. This makes the deployment sustainable day-to-day, but it depends on Story 1 existing first.

**Independent Test**: With a running deployment, change application code, run the deploy command, and verify the new version is live. Separately, change the database schema, run the deploy command with the schema option, and verify the schema is updated — confirming the VM's secrets file was never overwritten and the command can be re-run with no ill effect.

**Acceptance Scenarios**:

1. **Given** a running deployment, **When** the operator runs the deploy command, **Then** the repository is synchronized to the VM (excluding source control, build artifacts, the VM's secrets file, and backups), the application image is rebuilt on the VM, and only the changed containers are recreated.
2. **Given** a running deployment, **When** the operator runs the deploy command repeatedly with no changes, **Then** it completes successfully with no disruptive effect (idempotent).
3. **Given** a database schema change, **When** the operator runs the deploy command with the schema option, **Then** the schema (compiled from the database project, including post-deployment scripts) is published to the in-VM database over a private channel without exposing the database to the public internet.
4. **Given** the VM has a configured secrets file, **When** any deploy or sync runs, **Then** that secrets file is never overwritten or deleted.
5. **Given** the database is not yet healthy, **When** the deploy command runs, **Then** it waits for the database to become healthy before deploying the application, and fails clearly if it does not.

---

### User Story 3 - Operate the deployment economically and observably (Priority: P3)

The operator runs the deployment economically and can observe it when needed: nightly local backups protect data, an optional power schedule cuts cost by deallocating the VM off-hours, telemetry can be viewed on demand at no cost, and logs are available without any cloud log bill.

**Why this priority**: These are operational quality-of-life and cost-optimization capabilities. They add resilience and savings but the deployment is already usable without them.

**Independent Test**: Enable the nightly backup job and confirm a database backup and a storage archive are produced and old ones pruned. Enable the power schedule and confirm the VM auto-stops and auto-starts on the configured times while the domain and certificate survive. Start the on-demand telemetry viewer, confirm it is reachable only via a secure local tunnel, and confirm it costs nothing and is off by default.

**Acceptance Scenarios**:

1. **Given** the nightly backup job is installed on the VM, **When** it runs, **Then** it produces a compressed database backup and an application-storage archive, retains them for a configurable number of days (default 7), and prunes older ones.
2. **Given** the power schedule is provisioned, **When** the configured stop time arrives, **Then** the VM is deallocated (compute billing stops); **and When** the configured weekday start time arrives, **Then** the VM is started automatically; **and** the static IP, DNS, and TLS remain valid across the stop/start cycle.
3. **Given** the operator wants to start or stop the VM outside the schedule, **When** they run the corresponding manual control command, **Then** the VM starts/stops on demand and its power state can be queried; the schedule can be disabled and re-enabled.
4. **Given** the operator needs telemetry, **When** they enable the on-demand telemetry viewer, **Then** it runs in-memory only (no persistence, no cost), is reachable only through a secure local tunnel (never published publicly), self-evicts old data, and is off by default; **and** the application exports telemetry only when explicitly pointed at it.
5. **Given** the operator needs logs, **When** they view container logs on the VM, **Then** live and recent logs are available with size-capped rotation and no cloud log-ingestion cost.

---

### Edge Cases

- **DNS not yet resolving**: If the domain does not resolve to the VM when the proxy starts, certificate issuance fails. The documentation MUST instruct the operator to point and verify the DNS A record before the first deploy.
- **Memory pressure on the baseline VM**: The default baseline VM is the smallest size that runs the application plus SQL Server. Under memory pressure (e.g., heavy operations or the telemetry viewer running), processes may be killed. The deployment MUST document how to move to a larger size (via an override or a resize command).
- **Managed-identity RBAC propagation delay**: When the (future-use) storage account and managed-identity role assignment are provisioned, role propagation can take several minutes. The deployment MUST document that any blob-dependent behavior may not work immediately and self-heals once propagation completes.
- **Missing secrets file on the VM**: If the operator runs a deploy before creating the VM's secrets file, the deploy MUST fail with a clear, actionable message rather than proceeding with defaults.
- **SQL password mismatch**: If the schema-publish credentials do not match the database's configured credentials, the publish MUST fail clearly.
- **Re-running provisioning**: Provisioning steps MUST be safe to re-run (e.g., after the resource group was deleted to stop billing) without manual cleanup.
- **Telemetry viewer left running**: Because the telemetry viewer consumes memory, the documentation MUST note it should be stopped after use on the baseline VM size.
- **Application requires AppHost-injected configuration**: If the application currently depends on configuration that only the Aspire AppHost injects at run time, that dependency MUST be resolved so the standalone container starts correctly; otherwise the container will fail to start.

## Requirements *(mandatory)*

### Functional Requirements

#### Provisioning & Infrastructure

- **FR-001**: The deployment MUST provision a single Linux VM with a static public IP using the `az` CLI from the operator's dev machine, creating the target resource group if it does not exist.
- **FR-002**: Provisioning MUST default to a documented low fixed-cost baseline (Standard_B2s, centralus region, 64 GB StandardSSD OS disk, Standard static public IP) while allowing the VM size, region, resource/VM names, admin user, and disk size to be overridden via environment variables.
- **FR-003**: Provisioning MUST configure network access so that web ports (80 and 443) are open to the internet and SSH (port 22) is restricted to the operator's current public IP (auto-detected, with a manual override).
- **FR-004**: The VM MUST bootstrap on first boot to install the container runtime and compose tooling, cap container log size on disk, and enable a host firewall permitting only SSH and web ports.
- **FR-005**: Provisioning MUST print the VM's public IP and the immediate next steps (DNS, secrets, deploy) on completion.
- **FR-006**: Provisioning steps MUST be idempotent / safe to re-run without requiring manual cleanup of previously created resources.

#### Application Runtime (Containers)

- **FR-007**: The deployment MUST run the application stack on the VM via a single multi-service container composition consisting of: an auto-TLS reverse proxy, the Mentoory web application, and a SQL Server 2022 database.
- **FR-008**: The reverse proxy MUST obtain and renew a valid TLS certificate automatically for the operator-supplied domain, and forward requests to the web application; the domain and certificate-registration email MUST be supplied via configuration.
- **FR-009**: The web application MUST be built from a new multi-stage container image definition that compiles the .NET 10 Mentoory web project (which references the full set of modular projects and service defaults) using the repository root as the build context, producing a runtime image that listens on an internal HTTP port and trusts forwarded headers.
- **FR-010**: The web application container MUST run standalone without the Aspire AppHost — reading all required configuration (including the database connection named `DefaultConnection`) from environment variables and appsettings, with no dependency on configuration that only the AppHost injects at run time.
- **FR-011**: The database MUST run as SQL Server 2022 (Developer edition for non-production), be reachable by the application only over the private container network, be bound to the VM's loopback for any host access (never exposed to the public internet), be memory-capped to coexist with the application on the baseline VM, and report a health status the deploy process can wait on.
- **FR-012**: The web application's connection string MUST target the in-stack database server, database name `MentooryDb`, using credentials supplied via configuration.
- **FR-013**: The deployment MUST persist the web application's data-protection keys across container recreations (via a durable volume) so that redeploys do not invalidate user sessions, antiforgery tokens, or data-protection-encrypted data.
- **FR-014**: The deployment MUST inject application-required configuration via environment (including the MediatR license key and any admin sentinel credential the app requires) and pin log levels for cost/parity; Mentoory does NOT use Syncfusion and that configuration MUST be omitted.
- **FR-015**: All application state (database files, durable volumes) MUST live on the VM such that the documented backup mechanism can capture it.

#### Database Schema Publishing

- **FR-016**: The deployment MUST publish the Mentoory database schema (compiled from the SSDT database project, including the project's post-deployment scripts) to the in-VM database, targeting database `MentooryDb`.
- **FR-017**: Schema publishing MUST reach the database over a secure private channel (e.g., an SSH tunnel) and MUST NEVER require exposing the database to the public internet.
- **FR-018**: Schema publishing MUST reuse the project's established SSDT/DACPAC conventions (no EF migrations), consistent with the existing database publish tooling.

#### Deploy Workflow

- **FR-019**: The deployment MUST provide a single idempotent deploy command, runnable from the dev machine, that handles both the first deploy and every subsequent update.
- **FR-020**: The deploy command MUST synchronize the repository to the VM excluding source control, build artifacts, dependency caches, the VM's secrets file, and backup artifacts.
- **FR-021**: The deploy command MUST ensure the database is healthy, optionally publish the schema (via a flag), build the application image on the VM, and recreate only the containers whose image or configuration changed.
- **FR-022**: The deploy command MUST support, at minimum, opting into schema publishing, recreating without rebuilding, and tailing logs after deploy.
- **FR-023**: The deploy command MUST NEVER overwrite or delete the VM's secrets file, and MUST fail with a clear, actionable message if that file is missing on a first deploy.
- **FR-024**: The deployment MUST provide a secrets template (copied to a real secrets file on the VM, never committed to source control) covering domain, certificate email, database password, MediatR license key, admin credential, optional storage settings, and optional telemetry endpoint.

#### Storage (Provisioned for Future Use)

- **FR-025**: The deployment MUST provide a script to create a durable object-storage account and grant the VM's system-assigned managed identity the appropriate data role, printing the storage endpoint for configuration — provided as future-ready plumbing.
- **FR-026**: Because no current Mentoory code consumes object storage, the storage wiring MUST be optional/no-op by default and clearly documented as "provisioned but not yet consumed" until an attachments feature is added; the deployment MUST function fully without it.

#### Operations: Backups, Power Schedule, Telemetry, Logs

- **FR-027**: The deployment MUST provide a nightly backup mechanism (run on the VM via a scheduled job) that produces a compressed database backup and an application-storage archive, retains them for a configurable number of days (default 7), prunes older artifacts, and documents how to optionally copy backups off the VM.
- **FR-028**: The deployment MUST provide a power-schedule mechanism that auto-starts the VM on configured weekday mornings and auto-stops it on configured evenings (timezone configurable, defaulting to Costa Rica), to roughly halve cost; the static IP MUST persist across stop/start so DNS and TLS are unaffected.
- **FR-029**: The power-schedule mechanism MUST provide manual controls to start, stop, query status, and disable/enable the schedules on demand, independent of the automatic schedule.
- **FR-030**: The deployment MUST provide an on-demand telemetry viewer that runs in-memory only (no persistence, no recurring cost), is bound to loopback and reachable only via a secure local tunnel (never published publicly, no public authentication exposure), self-evicts old data, and is disabled by default; the application MUST export telemetry only when explicitly configured to point at it.
- **FR-031**: The deployment MUST rely on container logs (live tail + recent history) with size-capped on-disk rotation and MUST incur no cloud log-ingestion cost; the documentation MUST cover the log commands and the rotation cap.
- **FR-032**: The deployment MUST document an optional literal cost kill-switch (documentation only, no script) describing how a budget alert can trigger automation that deallocates the VM at a spend threshold.

#### Deliverables & Documentation

- **FR-033**: All deployment artifacts MUST live under a new `deploy/vm/` directory in the Mentoory repository, mirroring the reference layout (provisioning scripts, container composition, reverse-proxy config, secrets template, deploy script, schema-publish script, backup script, power-schedule script, and a README).
- **FR-034**: The README MUST document one-time setup, day-to-day deploys, logs, storage, backups, power schedule, the cost kill-switch, and how to decommission the prior Aspire/Azure-SQL deployment to avoid double-billing.
- **FR-035**: Naming, region, VM size, domain, timezone, weekdays, and credentials MUST all be overridable via environment configuration, with the documented defaults serving as the baseline.
- **FR-036**: The Spanish-language UI and all existing application behavior MUST remain unchanged by this deployment method; no application feature may regress.

### Key Entities *(include if feature involves data)*

- **VM Deployment Host**: The single Linux VM that runs the entire stack. Attributes: size, region, OS disk, static public IP, managed identity, network security rules.
- **Container Stack**: The composed set of services (reverse proxy, web application, database, optional telemetry viewer) and their durable volumes (database data, data-protection keys, proxy state, application storage).
- **Secrets File**: The VM-local configuration holding domain, certificate email, database password, license key, admin credential, optional storage and telemetry settings. Never committed; never overwritten by deploys.
- **Schema Artifact**: The compiled database package (from the SSDT project + post-deployment scripts) published to the `MentooryDb` database.
- **Backup Artifact**: Nightly compressed database backup + application-storage archive, retained for a configurable window.
- **Object-Storage Account (future)**: Durable storage plus a managed-identity role grant, provisioned but not yet consumed by application code.
- **Power Schedule**: The automation that starts/stops the VM on configured times, plus its manual controls.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: From a clean Azure subscription, an operator can go from zero to a working HTTPS-served Mentoory app — VM provisioned, schema published, app reachable at their domain with a valid certificate — by following the documented steps, with no Aspire AppHost, no Azure Container Apps, and no Azure SQL involved.
- **SC-002**: The recurring monthly cost of the running deployment is fixed and independent of request volume, at the documented baseline (~$40/month for the default size) — verifiable from the documented cost breakdown and Azure billing.
- **SC-003**: A subsequent code change reaches production with a single deploy command, and re-running that command with no changes produces no disruptive effect (idempotent).
- **SC-004**: A database schema change reaches the in-VM database via a single command over a private channel, with the database never exposed publicly (no public 1433 endpoint at any point).
- **SC-005**: The VM's secrets file is preserved across an arbitrary number of deploys (never overwritten or deleted).
- **SC-006**: With the power schedule enabled, the VM is deallocated outside configured hours and started during configured hours automatically, and the domain + certificate continue to work after a stop/start cycle.
- **SC-007**: Nightly backups produce a database backup and a storage archive, retained for the configured window with older artifacts pruned.
- **SC-008**: Telemetry can be viewed on demand at zero recurring cost, is never reachable from the public internet, and is off by default.
- **SC-009**: All existing application features and the Spanish UI continue to work identically under the VM deployment (no functional regression).
- **SC-010**: Every documented override (name, region, size, domain, timezone, credentials) takes effect when set, and the defaults apply when unset.

## Assumptions

- The operator has an Azure subscription, is authenticated with the `az` CLI, has SSH access to the VM, and controls DNS for the chosen domain (able to create an A record).
- The chosen domain is supplied at deploy time via the VM's secrets file; the deployment does not manage DNS itself. The operator points and verifies the A record before the first deploy so certificate issuance can succeed.
- "Production" here means a single-instance, single-VM deployment without high-availability/multi-region; this trades resilience for fixed, predictable cost (matching the reference methodology and the stated goal).
- SQL Server Developer edition is acceptable for the target (non-production-license) usage, consistent with the reference.
- The application can be made to start standalone (without the Aspire AppHost) by supplying configuration via environment/appsettings; resolving the current AppHost-injected configuration dependency is part of this work and assumed feasible without changing application features.
- Object storage is included as future-ready plumbing only; no current feature reads or writes it, and the deployment is fully functional without it.
- Default operational parameters (Costa Rica timezone, weekday schedule, 7-day backup retention, B2s baseline) are reasonable defaults derived from the reference and are all overridable.
- This feature adds deployment artifacts and a container image definition; it does not alter application domain logic, CQRS, or database schema content (it only publishes the existing schema).
- The prior Aspire/Azure-SQL deployment path remains in the repository and is decommissioned by the operator only after the VM deployment is serving traffic (documented), to avoid downtime or double-billing during transition.
