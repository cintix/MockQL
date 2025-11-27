using System.Text;
using Cintix.MockQL.Infrastructure.Domain;

namespace Cintix.MockQL.Infrastructure.SQLite;

public static class SqlGenerator
{
    public static void GenerateSQLActionMaps(SQLTable table)
    {
        GenerateCreateTable(table);
        GenerateInsert(table);
        GenerateUpdate(table);
        GenerateDelete(table);
        GenerateSelectById(table);
        GenerateSelectAll(table);
    }

    private static void GenerateUpdate(SQLTable table)
    {
        string tableName = ToSqlCase(table.Name);

        var setParts = new List<string>();

        foreach (var field in table.Fields)
        {
            if (field.Name.Equals("id", StringComparison.OrdinalIgnoreCase))
                continue;

            setParts.Add($"{ToSqlCase(field.Name)} = @{SqlParam(field.Name)}");
        }

        foreach (var reference in table.References)
        {
            setParts.Add($"{ToSqlCase(reference.ColumnName)} = @{SqlParam(reference.ColumnName)}");
        }

        string sql =
            $"UPDATE {tableName} SET {string.Join(", ", setParts)} WHERE id = @id;";

        table.SQLActions["UPDATE_SQL"] = sql;
    }

    private static void GenerateDelete(SQLTable table)
    {
        string tableName = ToSqlCase(table.Name);
        string sql = $"DELETE FROM {tableName} WHERE id = @id;";
        table.SQLActions["DELETE_SQL"] = sql;
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
            parameters.Add($"@{SqlParam(field.Name)}");
        }

        foreach (var reference in table.References)
        {
            columns.Add(ToSqlCase(reference.ColumnName));
            parameters.Add($"@{SqlParam(reference.ColumnName)}");
        }

        string sql = $"INSERT INTO {tableName} " +
                     $"({string.Join(", ", columns)}) " +
                     $"VALUES ({string.Join(", ", parameters)});";

        table.SQLActions["INSERT_SQL"] = sql;
    }

    private static void GenerateCreateTable(SQLTable table)
    {
        string tableName = ToSqlCase(table.Name);
        var sb = new StringBuilder();
        sb.AppendLine($"CREATE TABLE IF NOT EXISTS {tableName} (");

        var lines = new List<string>();
        var pk = table.PrimaryKey!;

        if (pk.Type == SQLType.Guid)
            lines.Add($"    id BLOB PRIMARY KEY NOT NULL DEFAULT (lower(hex(randomblob(16))))");
        else if (pk.Type == SQLType.Int)
            lines.Add($"    id INTEGER PRIMARY KEY AUTOINCREMENT");
        else
            throw new Exception($"Invalid ID type for table {table.Name}");

        foreach (var field in table.Fields.Where(f => f.Name != "id"))
        {
            string nullPart = field.IsNullable ? "NULL" : "NOT NULL";
            lines.Add($"    {ToSqlCase(field.Name)} {SqlTypeToSql(field.Type)} {nullPart}");
        }

        foreach (var r in table.References)
        {
            lines.Add($"    {r.ColumnName} BLOB NOT NULL");
            lines.Add($"    FOREIGN KEY({ToSqlCase(r.ColumnName)}) REFERENCES {ToSqlCase(r.TargetTable)}(id)");
        }

        sb.AppendLine(string.Join(",\n", lines));
        sb.AppendLine(");");

        table.SQLActions["CREATE_TABLE_SQL"] = sb.ToString();
    }

    private static void GenerateSelectById(SQLTable table)
    {
        string tableName = ToSqlCase(table.Name);
        string sql = $"SELECT * FROM {tableName} WHERE id = @id;";
        table.SQLActions["SELECT_BY_ID_SQL"] = sql;
    }

    private static void GenerateSelectAll(SQLTable table)
    {
        string tableName = ToSqlCase(table.Name);
        string sql = $"SELECT * FROM {tableName};";
        table.SQLActions["SELECT_ALL_SQL"] = sql;
    }

    private static string ToSqlCase(string name)
    {
        var sb = new StringBuilder();
        for (int i = 0; i < name.Length; i++)
        {
            char c = name[i];
            if (char.IsUpper(c) && i > 0) sb.Append('_');
            sb.Append(char.ToLower(c));
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
    
    private static string SqlParam(string name) => $"{ToSqlCase(name)}";
}
