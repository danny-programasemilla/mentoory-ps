# Quickstart: CSV Sample Download for Batch Upload

**Feature**: 007-csv-sample-download
**Date**: 2026-04-10

## What This Feature Does

Adds a "Descargar plantilla CSV" link to the Batch Upload page (`/Administration/BatchUpload`). When clicked, it downloads a sample CSV file (`plantilla-carga-masiva.csv`) pre-populated with the correct column headers and two example rows.

## Files Changed

| File | Change |
|------|--------|
| `Mentoory.Web/Areas/Administration/Controllers/BatchUploadController.cs` | Add `DownloadSample` GET action |
| `Mentoory.Web/Areas/Administration/Views/BatchUpload/Index.cshtml` | Add download link near file input |

## How to Test

1. Run the app: `dotnet run --project Mentoory.Web`
2. Log in as a user with ProjectCoordinator, IncubatorAdmin, or GlobalAdmin role
3. Select an incubator context
4. Navigate to `/Administration/BatchUpload`
5. Click the "Descargar plantilla CSV" link
6. Verify the downloaded file:
   - Filename: `plantilla-carga-masiva.csv`
   - Headers: `Country,Identification,Email,FirstName,LastName`
   - Contains 2 example rows with realistic placeholder data
7. Replace example rows with real data, upload via the form, and confirm no header errors

## Prerequisites

No additional setup, packages, or database changes required.
