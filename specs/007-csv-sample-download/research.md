# Research: CSV Sample Download for Batch Upload

**Feature**: 007-csv-sample-download
**Date**: 2026-04-10

## Research Summary

No NEEDS CLARIFICATION items were identified in the Technical Context. The feature is straightforward and all decisions can be made from existing codebase context.

## Decision: Sample CSV Generation Approach

**Decision**: Generate the CSV dynamically via a controller action using CsvHelper and the existing `CsvUserMap` class map.

**Rationale**: Using the same `CsvUserMap` that the upload parser uses guarantees the sample headers always match the expected format. If column names or mappings change, the sample automatically stays in sync — no risk of a stale static file.

**Alternatives considered**:
- **Static CSV file in wwwroot**: Simpler but risks going out of sync with `CsvUserMap` if columns are added/renamed. Rejected because maintenance burden outweighs simplicity.
- **Embedded resource CSV**: Same sync risk as static file. Rejected.

## Decision: Example Row Content

**Decision**: Include two example rows with realistic but clearly fictitious data using Costa Rica ("CRI") as country code, since the platform targets Spanish-speaking markets.

**Rationale**: Two rows demonstrate the format without being excessive. Using obviously fake but representative data (e.g., "Juan Ejemplo", "Maria Muestra") prevents users from accidentally uploading sample data.

**Alternatives considered**:
- Headers only (no example rows): Users might not understand expected formats (e.g., country code vs full country name). Rejected.
- Three or more rows: No additional value over two rows. Rejected.

## Decision: File Download Mechanism

**Decision**: Return a `FileContentResult` from a GET action on the existing `BatchUploadController`. The action writes to a `MemoryStream` using CsvHelper, then returns the bytes with `text/csv` content type.

**Rationale**: Keeps all batch upload logic in one controller. The class-level `[Authorize]` attribute already covers authorization. No new routes or controllers needed.
