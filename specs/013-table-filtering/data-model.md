# Data Model: Table Filtering

**Feature**: 013-table-filtering
**Date**: 2026-04-13

## Overview

No new database entities or schema changes. This feature operates entirely at the Web Layer (JavaScript + CSS) and Application Layer (query handler filter logic).

## Client-Side Data Structures

### Filter Type Registry Entry

Describes how a column should be filtered, keyed by render function signature.

| Field | Type | Description |
|-------|------|-------------|
| type | string | Filter input type: `'text'` or `'select'` |
| options | array? | For `'select'`: `[{ value: string, label: string }]`. First option is always `{ value: '', label: 'Todos' }` |

### Filter Override Entry

Per-view override passed in `initDataTable()` config.

| Field | Type | Description |
|-------|------|-------------|
| column | string | Column `data` property name to target |
| filterable | boolean? | Set `false` to exclude from filtering |
| type | string? | Override detected filter type |
| options | array? | Override dropdown options |
| placeholder | string? | Custom placeholder text |

### Active Filter State

Runtime state tracked per table instance.

| Field | Type | Description |
|-------|------|-------------|
| tableId | string | The DataTable element ID |
| formId | string | Generated filter form ID (`{tableId}-filter-form`) |
| panelId | string | Generated panel container ID (`{tableId}-filter-panel`) |
| toggleId | string | Toggle link ID (`{tableId}-filter-toggle`) |
| activeCount | number | Count of non-empty filter fields |

## Server-Side Data Flow

### Existing (no changes)

```
DataTableServerRequest.Filters : Dictionary<string, string>?
    ↓ ToDataTableRequest()
DataTableRequest.Filters : Dictionary<string, string>?
    ↓ passed to handler
Handler consumes Filters → applies LINQ Where clauses
```

### Filter Key Convention

Filter keys match the column's `data` property from DataTables config:

| Column Data | Filter Key | Example Value |
|-------------|------------|---------------|
| email | email | "john@example.com" |
| firstName | firstName | "Juan" |
| accountStatus | accountStatus | "Active" |
| createdAtUtc | createdAtUtc | "2026-01-15" |

## URL Parameter Schema

Filter values are encoded as URL query params with `f_` prefix:

```
/Administration/Users?f_accountStatus=Active&f_firstName=Juan
```

| URL Param | Maps To | Filter Key |
|-----------|---------|------------|
| f_{columnData} | Form field name="{columnData}" | Sent as `filters[columnData]` in POST |
