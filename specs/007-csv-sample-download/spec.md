# Feature Specification: CSV Sample Download for Batch Upload

**Feature Branch**: `007-csv-sample-download`  
**Created**: 2026-04-10  
**Status**: Draft  
**Input**: User description: "Have in the BatchUpload page an option to download a sample of the CSV expected so it can be used as starting point to fill"

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Download CSV Sample File (Priority: P1)

A project coordinator or administrator navigates to the Batch Upload page to register multiple users at once. Before preparing their CSV file, they want to download a sample CSV that shows the exact columns and format expected by the system. They click a "Download sample" link, receive a pre-filled CSV file with example rows, and use it as a starting point to populate with real user data.

**Why this priority**: This is the core (and only) feature — without the downloadable sample, users must guess the correct column names, order, and format, leading to upload errors and frustration.

**Independent Test**: Can be fully tested by navigating to the Batch Upload page, clicking the download link, and verifying the downloaded file contains the correct headers and example data.

**Acceptance Scenarios**:

1. **Given** a user with batch upload permissions is on the Batch Upload page, **When** they click the "Download sample" link, **Then** a CSV file is downloaded to their browser with the filename `plantilla-carga-masiva.csv`.
2. **Given** the sample CSV is downloaded, **When** the user opens the file, **Then** it contains the five expected column headers (Country, Identification, Email, FirstName, LastName) and at least two example rows with realistic placeholder data.
3. **Given** the sample CSV is downloaded, **When** the user replaces the example rows with real data and uploads it through the Batch Upload form, **Then** the system accepts the file without header validation errors.

---

### Edge Cases

- What happens if the user's browser blocks the file download? Standard browser download behavior applies; no special handling is needed since this is a simple file link.
- What happens if the expected CSV format changes in the future? The sample file must be kept in sync with the CSV parsing logic to avoid mismatches between the sample and the actual expected format.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The Batch Upload page MUST display a visible link or button to download a sample CSV file, placed near the file upload area so users see it before uploading.
- **FR-002**: The downloadable sample CSV MUST contain the five column headers that the system accepts: Country, Identification, Email, FirstName, LastName.
- **FR-003**: The sample CSV MUST include at least two example rows with realistic placeholder data that demonstrates the expected format for each column.
- **FR-004**: The download link MUST be accessible to all users who have permission to access the Batch Upload page (ProjectCoordinator, IncubatorAdmin, GlobalAdmin).
- **FR-005**: The download label and any surrounding instructional text MUST be in Spanish, consistent with the rest of the application UI.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: 100% of users with batch upload permissions can locate and use the sample download link on their first visit to the page without external guidance.
- **SC-002**: The downloaded sample file can be uploaded back to the system without any header validation errors (proving format consistency).
- **SC-003**: Users can prepare a valid batch upload file starting from the sample in under 5 minutes (compared to trial-and-error without it).

## Assumptions

- The sample CSV uses the primary column names (Country, Identification, Email, FirstName, LastName) rather than the Spanish variants, since both are accepted by the parser and the primary names are more universally understood.
- The sample file is a static artifact — it does not need to be generated dynamically per user or per project context.
- The download does not require a server round-trip to a controller action; it can be served as a static file. However, if maintaining sync with the parsing logic is preferred, a controller-based approach is also acceptable.
- No new database entities, migrations, or domain changes are required for this feature.
