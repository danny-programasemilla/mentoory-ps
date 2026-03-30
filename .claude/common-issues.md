# Mentoory Common Issues & Solutions

## Database Build Issues

### SQL Syntax Error: "Incorrect syntax near 'INCLUDE'"
**Error**: `error SQL46010: Incorrect syntax near 'INCLUDE'` during `dotnet build` of SQL project
**Root Cause**: Wrong syntax order in filtered indexes - WHERE clause before INCLUDE clause
**Solution**: INCLUDE must come BEFORE WHERE in SQL Server index definitions
```sql
-- ❌ WRONG - Causes syntax error
CREATE NONCLUSTERED INDEX [IX_Name]
ON [schema].[Table] ([Column])
WHERE [FilterColumn] IS NOT NULL
INCLUDE ([IncludedColumns]);

-- ✅ CORRECT
CREATE NONCLUSTERED INDEX [IX_Name]
ON [schema].[Table] ([Column])
INCLUDE ([IncludedColumns])
WHERE [FilterColumn] IS NOT NULL;
```

### Filtered Index on Computed Column Error
**Error**: `Filtered index cannot be created because the column in the filter expression is a computed column`
**Root Cause**: Trying to use non-persisted computed column in WHERE clause
**Solution**:
- Use PERSISTED computed columns - they CAN be filtered
- Or filter on the base column instead
```sql
-- Column definition
[GeohashPrefix5] AS LEFT([Geohash], 5) PERSISTED

-- ✅ CORRECT - Can filter on persisted computed column
WHERE [GeohashPrefix5] IS NOT NULL

-- ✅ ALSO CORRECT - Filter on base column
WHERE LEN([Geohash]) >= 5
```

### UTF-8 BOM Characters Causing Parse Errors ⚠️ CRITICAL
**Error**: `error SQL46010: Incorrect syntax near 'INCLUDE'` (or other cryptic errors) on valid SQL files
**Root Cause**: UTF-8 Byte Order Mark (BOM - bytes 0xEF 0xBB 0xBF) at start of files confuses MSBuild.Sdk.SqlProj parser
**Detection**:
```bash
hexdump -C file.sql | head -1  # Shows "ef bb bf" at start
file file.sql  # Shows "UTF-8 (with BOM)"
```
**Solution**: Strip BOMs from all SQL files
```bash
# Remove BOM from all SQL files
find Db -name "*.sql" -type f -exec sed -i '1s/^\xEF\xBB\xBF//' {} \;
find PostDeployment -name "*.sql" -type f -exec sed -i '1s/^\xEF\xBB\xBF//' {} \;
```
**Prevention**: Configure editor to save SQL files as "UTF-8 without BOM"

### Missing Line Terminators
**Error**: `error SQL46010: Incorrect syntax near 'INCLUDE'` or parser failures
**Root Cause**: SQL files missing final newline
**Detection**: `file file.sql` shows "with no line terminators"
**Solution**: Add newline to end of file
```bash
echo "" >> file.sql
```

### MSBuild.Sdk.SqlProj File Ordering
**Issue**: Need schemas before tables, tables before indexes
**Wrong Approach**: Explicit ordering in sqlproj - MSBuild sorts alphabetically
**Solution**: Use wildcards - DacFx handles dependencies automatically
```xml
<!-- ✅ Simple and works -->
<ItemGroup>
  <Content Include="**/*.sql" Exclude="obj/**;bin/**" />
</ItemGroup>
```

## Dependency Injection & Service Issues

### ITimeProvider Not Found
**Error**: `CS0246: The type or namespace name 'ITimeProvider' could not be found`
**Solution**: Use correct namespace:
```csharp
// ❌ Wrong
using Mentoory.Shared.Domain.Services;

// ✅ Correct
using Mentoory.Shared.Application.TimeProvider;
```
**Pattern**: ITimeProvider is in Application layer, not Domain

### ResultErrorCodes Missing Definition
**Error**: `CS0117: 'ResultErrorCodes' does not contain a definition for 'BusinessValidationError'`
**Solution**: Use existing error codes:
```csharp
// For domain validation errors
ResultErrorCodes.GenericError

// For not found errors
ResultErrorCodes.BusinessIncubator_NotFound
```

