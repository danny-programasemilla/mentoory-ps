using FluentAssertions;
using Mentoory.Access.Infrastructure.Persistence;
using Mentoory.Example.Infrastructure.Persistence;
using Mentoory.Tenant.Infrastructure.Persistence;
using Mentoory.Tests.Integration.Fixtures;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Mentoory.Tests.Integration.Schema;

/// <summary>
/// Detects drift between EF Core model expectations and the actual database schema
/// (deployed from the DACPAC). Catches mismatches like TINYINT vs INT that cause
/// runtime <see cref="InvalidCastException"/> but pass silently when tests create
/// the schema via <c>EnsureCreatedAsync()</c>.
/// </summary>
[Collection(IntegrationTestCollection.Name)]
public class SchemaDriftTests
{
    private readonly MentooryWebApplicationFactory _factory;

    public SchemaDriftTests(MentooryWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Theory]
    [InlineData(typeof(AccessDbContext))]
    [InlineData(typeof(TenantDbContext))]
    [InlineData(typeof(ExampleDbContext))]
    public async Task EfModel_ColumnTypes_MatchDatabaseSchema(Type dbContextType)
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = (DbContext)scope.ServiceProvider.GetRequiredService(dbContextType);
        var model = dbContext.Model;

        var mismatches = new List<string>();

        foreach (var entityType in model.GetEntityTypes())
        {
            var tableName = entityType.GetTableName();
            var schema = entityType.GetSchema();

            if (tableName is null)
            {
                continue;
            }

            var dbColumns = await GetDatabaseColumnsAsync(schema ?? "dbo", tableName);

            foreach (var property in entityType.GetProperties())
            {
                var columnName = property.GetColumnName(
                    StoreObjectIdentifier.Table(tableName, schema));

                if (columnName is null || !dbColumns.TryGetValue(columnName, out var dbColumn))
                {
                    continue;
                }

                var clrType = GetEffectiveClrType(property);
                var expectedSqlType = MapClrTypeToSqlType(clrType);

                if (expectedSqlType is not null && !IsCompatible(expectedSqlType, dbColumn.DataType))
                {
                    mismatches.Add(
                        $"[{schema}].[{tableName}].[{columnName}]: " +
                        $"EF expects CLR type '{clrType.Name}' (SQL: {expectedSqlType}) " +
                        $"but database column is '{dbColumn.DataType}'");
                }
            }
        }

        mismatches.Should().BeEmpty(
            "EF Core model column types must match the database schema (deployed from DACPAC). " +
            "Fix either the EF HasConversion/HasColumnType configuration or the SQL table definition.");
    }

    /// <summary>
    /// Returns the CLR type that EF will actually try to read from the database,
    /// accounting for value conversions.
    /// </summary>
    private static Type GetEffectiveClrType(IProperty property)
    {
        var converter = property.GetValueConverter();
        var providerType = converter?.ProviderClrType ?? property.ClrType;
        return Nullable.GetUnderlyingType(providerType) ?? providerType;
    }

    private static string? MapClrTypeToSqlType(Type clrType)
    {
        return clrType switch
        {
            _ when clrType == typeof(bool) => "bit",
            _ when clrType == typeof(byte) => "tinyint",
            _ when clrType == typeof(short) => "smallint",
            _ when clrType == typeof(int) => "int",
            _ when clrType == typeof(long) => "bigint",
            _ when clrType == typeof(decimal) => "decimal",
            _ when clrType == typeof(float) => "real",
            _ when clrType == typeof(double) => "float",
            _ when clrType == typeof(string) => "nvarchar",
            _ when clrType == typeof(DateTime) => "datetime2",
            _ when clrType == typeof(DateTimeOffset) => "datetimeoffset",
            _ when clrType == typeof(Guid) => "uniqueidentifier",
            _ when clrType == typeof(byte[]) => "varbinary",
            _ when clrType == typeof(TimeSpan) => "time",
            _ => null,
        };
    }

    private static bool IsCompatible(string expectedSqlType, string actualSqlType)
    {
        return string.Equals(expectedSqlType, actualSqlType, StringComparison.OrdinalIgnoreCase);
    }

    private async Task<Dictionary<string, DbColumnInfo>> GetDatabaseColumnsAsync(string schema, string tableName)
    {
        var columns = new Dictionary<string, DbColumnInfo>(StringComparer.OrdinalIgnoreCase);

        await using var connection = new SqlConnection(_factory.ConnectionString);
        await connection.OpenAsync();

        await using var cmd = connection.CreateCommand();
        cmd.CommandText = """
            SELECT COLUMN_NAME, DATA_TYPE, IS_NULLABLE, CHARACTER_MAXIMUM_LENGTH
            FROM INFORMATION_SCHEMA.COLUMNS
            WHERE TABLE_SCHEMA = @schema AND TABLE_NAME = @table
            """;
        cmd.Parameters.AddWithValue("@schema", schema);
        cmd.Parameters.AddWithValue("@table", tableName);

        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            var name = reader.GetString(0);
            columns[name] = new DbColumnInfo(
                DataType: reader.GetString(1),
                IsNullable: reader.GetString(2) == "YES",
                MaxLength: reader.IsDBNull(3) ? null : reader.GetInt32(3));
        }

        return columns;
    }

    private sealed record DbColumnInfo(string DataType, bool IsNullable, int? MaxLength);
}
