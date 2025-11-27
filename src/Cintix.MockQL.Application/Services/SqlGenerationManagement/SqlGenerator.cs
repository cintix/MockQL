using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Cintix.MockQL.Infrastructure.Domain.Enums;
using Cintix.MockQL.Infrastructure.Domain.Models;

namespace Cintix.MockQL.Application.Services.SqlGenerationManagement;

public class SqlGenerator : ISqlGenerator
{
    public void GenerateSqlActionMaps(SQLTable table)
    {
        GenerateCreateTable(table);
        GenerateInsert(table);
        GenerateUpdate(table);
        GenerateDelete(table);
        GenerateSelectAll(table);
        GenerateSelectById(table);
    }

    private static void GenerateInsert(SQLTable table)
    {
        string tableName = ToSqlCase(table.Name);

        var columns = new List<string>();
        var parameters = new List<string>();

        foreach (var field in table.Fields)
        {
            if (field.Name.Equals("id", StringComparison.OrdinalIgnoreCase))
                continue;

            columns.Add(ToSqlCase(field.Name));
            parameters.Add($"@{ToSqlCase(field.Name)}");
        }

        foreach (var reference in table.References)
        {
            columns.Add(ToSqlCase(reference.ColumnName));
            parameters.Add($"@{ToSqlCase(reference.ColumnName)}");
        }

        string sql = $"INSERT INTO {tableName} " +
                     $"({string.Join(", ", columns)}) " +
                     $"VALUES ({string.Join(", ", parameters)});";

        table.SQLActions["SQL_INSERT"] = sql;
    }

    private static void GenerateUpdate(SQLTable table)
    {
        string tableName = ToSqlCase(table.Name);

        var setParts = new List<string>();

        foreach (var field in table.Fields)
        {
            if (field.Name.Equals("id", StringComparison.OrdinalIgnoreCase))
                continue;

            var columnName = ToSqlCase(field.Name);
            setParts.Add($"{columnName} = @{columnName}");
        }

        foreach (var reference in table.References)
        {
            var columnName = ToSqlCase(reference.ColumnName);
            setParts.Add($"{columnName} = @{columnName}");
        }

        string sql = $"UPDATE {tableName} SET {string.Join(", ", setParts)} WHERE id = @id;";
        table.SQLActions["SQL_UPDATE"] = sql;
    }

    private static void GenerateDelete(SQLTable table)
    {
        string tableName = ToSqlCase(table.Name);
        string sql = $"DELETE FROM {tableName} WHERE id = @id;";
        table.SQLActions["SQL_DELETE"] = sql;
    }

    private static void GenerateSelectAll(SQLTable table)
    {
        string tableName = ToSqlCase(table.Name);
        string sql = $"SELECT * FROM {tableName};";
        table.SQLActions["SQL_SELECT_ALL"] = sql;
    }

    private static void GenerateSelectById(SQLTable table)
    {
        string tableName = ToSqlCase(table.Name);
        string sql = $"SELECT * FROM {tableName} WHERE id = @id;";
        table.SQLActions["SQL_SELECT_BY_ID"] = sql;
    }

    private static void GenerateCreateTable(SQLTable table)
    {
        string tableName = ToSqlCase(table.Name);
        var sb = new StringBuilder();
        sb.AppendLine($"CREATE TABLE IF NOT EXISTS {tableName} (");

        var lines = new List<string>();
        var primaryKey = table.PrimaryKey!;

        if (primaryKey.Type == SQLType.Guid)
        {
            lines.Add("    id BLOB PRIMARY KEY NOT NULL DEFAULT (lower(hex(randomblob(16))))");
        }
        else if (primaryKey.Type == SQLType.Int)
        {
            lines.Add("    id INTEGER PRIMARY KEY AUTOINCREMENT");
        }
        else
        {
            throw new Exception($"Invalid ID type for table {table.Name}");
        }

        foreach (var field in table.Fields.Where(f => !f.Name.Equals("id", StringComparison.OrdinalIgnoreCase)))
        {
            string nullPart = field.IsNullable ? "NULL" : "NOT NULL";
            lines.Add($"    {ToSqlCase(field.Name)} {SqlTypeToSql(field.Type)} {nullPart}");
        }

        foreach (var reference in table.References)
        {
            lines.Add($"    {ToSqlCase(reference.ColumnName)} BLOB NOT NULL");
            lines.Add($"    FOREIGN KEY({ToSqlCase(reference.ColumnName)}) REFERENCES {ToSqlCase(reference.TargetTable)}(id)");
        }

        sb.AppendLine(string.Join(",", lines));
        sb.AppendLine(");");

        table.SQLActions["SQL_CREATE_TABLE"] = sb.ToString();
    }

    private static string ToSqlCase(string name)
    {
        var sb = new StringBuilder();
        for (int index = 0; index < name.Length; index++)
        {
            char character = name[index];
            if (char.IsUpper(character) && index > 0) sb.Append('_');
            sb.Append(char.ToLower(character));
        }

        return sb.ToString();
    }

    private static string SqlTypeToSql(SQLType type) => type switch
    {
        SQLType.Int => "INTEGER",
        SQLType.Real => "REAL",
        SQLType.Varchar => "TEXT",
        SQLType.Guid => "BLOB",
        SQLType.Boolean => "INTEGER",
        SQLType.Blob => "BLOB",
        _ => "TEXT"
    };
}