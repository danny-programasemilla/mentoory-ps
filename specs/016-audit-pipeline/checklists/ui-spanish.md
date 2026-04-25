# Spanish UI QA Checklist — `/Administration/AuditLog`

Verify all user-facing text on the admin audit-log viewer is Spanish, per Principle IX.

## Page shell

- [ ] Browser tab title shows `Registro de auditoría`
- [ ] H2 heading reads `Registro de auditoría`

## Table (rendered by `DataTableViewComponent` + `datatable-helper.js`)

- [ ] Column headers:
  - [ ] `Fecha (UTC)`
  - [ ] `Evento`
  - [ ] `Usuario`
  - [ ] `Acción`
  - [ ] `Resultado`
  - [ ] `Rol`
- [ ] Pagination controls: `Primero`, `Siguiente`, `Anterior`, `Ultimo`
- [ ] Length menu: `Mostrar _MENU_ registros`
- [ ] Info line: `Mostrando _START_ a _END_ de _TOTAL_ registros`
- [ ] Empty state: `No se encontraron resultados`
- [ ] Search label: `Buscar:`

## Filter panel (auto-generated)

- [ ] Toggle link label: `Filtros`
- [ ] `Evento` select — options labelled in Spanish:
  - [ ] `Todos`
  - [ ] `Contexto activado`
  - [ ] `Rol asignado`
  - [ ] `Usuario registrado`
  - [ ] `Inicio de sesión`
  - [ ] `Respuesta corregida`
- [ ] `Resultado` select — options labelled:
  - [ ] `Todos`
  - [ ] `Éxito`
  - [ ] `Fallo`
- [ ] `Usuario` text input placeholder reads `Correo electrónico`
- [ ] Submit button: `Filtrar`
- [ ] Clear button: `Limpiar`

## Expandable detail row

- [ ] Secondary-field labels (EntityType, EntityId, CorrelationId, etc.) are technical identifiers — intentionally not translated (Principle IX allows English for technical field names).

## Menu integration

- [ ] Menu entry under `Plataforma` reads `Registro de auditoría`

## Status badges

- [ ] `Outcome = Success` renders as green dot + `Éxito`
- [ ] `Outcome = Failure` renders as red dot + `Fallo`

## Toasts / error states

- [ ] On failed `/Data` request, surface the existing site-wide error toast (uses `datatable-helper.js`'s built-in messaging).

## Sign-off

- Tester: ____________________
- Date: ____________________
- Notes: ____________________
