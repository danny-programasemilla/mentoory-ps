# Data Model: CSV Sample Download for Batch Upload

**Feature**: 007-csv-sample-download
**Date**: 2026-04-10

## Summary

No new entities, attributes, or relationships are introduced by this feature.

The feature reuses the existing `CsvUserRecord` class and `CsvUserMap` class map (defined in `Mentoory.Web/Areas/Administration/Infrastructure/CsvUserMap.cs`) to generate the sample CSV. These types define the five-column schema:

| Column         | Type   | Accepted Header Names                                    |
|----------------|--------|----------------------------------------------------------|
| Country        | string | Country, Pais, País                                      |
| Identification | string | Identification, Identificacion, Identificación, NationalId |
| Email          | string | Email, Correo                                            |
| FirstName      | string | FirstName, Nombre                                        |
| LastName       | string | LastName, Apellido                                       |

No database tables, migrations, seed data, or PostDeployment scripts are affected.
