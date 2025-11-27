using System.Text;
using Cintix.MockQL.Infrastructure.Domain.Enums;
using Cintix.MockQL.Infrastructure.Domain.Models;

namespace Cintix.MockQL.Infrastructure.Application.Services.ModelManagement;

public class ModelWriter : IModelWriter
{
    private ModelDefinition _model;

    public void Build(ModelDefinition model, string @namespace, string path)
    {
        _model = model;
        string basePath = Path.Combine(path, "MockQL");
        string servicePath = Path.Combine(basePath, "Services");
        string modelPath = Path.Combine(basePath, "Models");

        Directory.CreateDirectory(servicePath);
        Directory.CreateDirectory(modelPath);

        foreach (var table in model.Tables.Values)
        {
            WriteService(table, servicePath, @namespace);
            WriteModel(table, modelPath, @namespace);
        }
    }

    private void WriteModel(SQLTable table, string modelPath, string ns)
    {
        string fileName = Path.Combine(modelPath, $"{table.Name}.cs");
        var sb = new StringBuilder();

        sb.AppendLine($"namespace {ns}.MockQL.Models;");
        sb.AppendLine();
        sb.AppendLine($"public class {table.Name}");
        sb.AppendLine("{");

        sb.AppendLine($"    public {MapToClrType(table.PrimaryKey!.Type)} Id {{ get; set; }}");

        foreach (var f in table.Fields)
        {
            if (f.Name.Equals(table.PrimaryKey.Name, StringComparison.OrdinalIgnoreCase))
                continue;

            var clrType = MapToClrType(f.Type);

            if (f.IsNullable && clrType != "string" && clrType != "byte[]")
                clrType += "?";

            sb.AppendLine($"    public {clrType} {Pascal(f.Name)} {{ get; set; }}");
        }

        foreach (var r in table.References)
        {
            string propName = Pascal(r.TargetTable);
            sb.AppendLine($"    public {MapToClrType(table.PrimaryKey!.Type)} {propName}Id {{ get; set; }}");
            sb.AppendLine($"    public {r.TargetTable} {propName} {{ get; set; }}");
        }

        sb.AppendLine("}");
        File.WriteAllText(fileName, sb.ToString());
    }

    private void WriteService(SQLTable table, string servicePath, string ns)
    {
        string className = $"{table.Name}Service";
        var sb = new StringBuilder();

        sb.AppendLine($"using Microsoft.Data.Sqlite;");
        sb.AppendLine($"using System.Collections.Generic;");
        sb.AppendLine($"using {ns}.MockQL.Models;");
        sb.AppendLine($"using Cintix.MockQL.Infrastructure.SQLite;");
        sb.AppendLine();
        sb.AppendLine($"namespace {ns}.MockQL.Services;");
        sb.AppendLine();
        sb.AppendLine($"public class {className}");
        sb.AppendLine("{");
        sb.AppendLine($"    private readonly SqliteConnection _connection;");

        foreach (var r in table.References)
            sb.AppendLine($"    private readonly {r.TargetTable}Service _{ToLowerFirst(r.TargetTable)}Service;");

        foreach (var kv in table.SQLActions)
        {
            if (kv.Key != "CREATE_TABLE_SQL")
                sb.AppendLine($"    public const string {kv.Key} = @\"{EscapeSql(kv.Value)}\";");
        }

        sb.AppendLine();
        sb.AppendLine($"    public {className}(SqliteConnection connection)");
        sb.AppendLine("    {");
        sb.AppendLine($"        _connection = connection;");

        foreach (var r in table.References)
            sb.AppendLine($"        _{ToLowerFirst(r.TargetTable)}Service = new {r.TargetTable}Service(connection);");

        sb.AppendLine("    }");

        // -------------------------
        //  CreateOrGet + GetByNaturalKey
        // -------------------------

        foreach (var r in table.References)
        {
            string refModel = r.TargetTable;
            sb.AppendLine();

            sb.AppendLine($"    private Guid CreateOrGet({refModel} entity)");
            sb.AppendLine("    {");
            sb.AppendLine($"        if ({refModel}.Id is not null) return {refModel}.Id;");
            sb.AppendLine($"        var existing = _{ToLowerFirst(refModel)}Service.GetByNaturalKey(entity);");
            sb.AppendLine($"        if (existing != null) return existing.id;");
            sb.AppendLine($"        _{ToLowerFirst(refModel)}Service.Create(entity);");
            sb.AppendLine($"        return entity.id;");
            sb.AppendLine("    }");

            // --- GetByNaturalKey (V1: match på alle primitive fields)
            sb.AppendLine();
            sb.AppendLine($"    public {refModel}? GetByNaturalKey({refModel} entity)");
            sb.AppendLine("    {");
            sb.AppendLine($"        using var cmd = _connection.CreateCommand();");

            var natKeyConditions = new List<string>();
            foreach (var f in _model.Tables[refModel].Fields)
            {
                if (f.Name.Equals("id", StringComparison.OrdinalIgnoreCase)) continue;
                natKeyConditions.Add($"{ToSqlCase(f.Name)} = @{SqlParam(f.Name)}");
            }

            string whereSql = string.Join(" AND ", natKeyConditions);
            sb.AppendLine($"        cmd.CommandText = @\"SELECT * FROM {ToSqlCase(refModel)} WHERE {whereSql} LIMIT 1;\";");

            foreach (var f in _model.Tables[refModel].Fields)
            {
                if (f.Name.Equals("id", StringComparison.OrdinalIgnoreCase)) continue;
                sb.AppendLine($"        cmd.Parameters.AddWithValue(\"@{SqlParam(f.Name)}\", entity.{f.Name});");
            }

            sb.AppendLine($"        using var reader = cmd.ExecuteReader();");
            sb.AppendLine($"        if (!reader.Read()) return null;");

            sb.AppendLine($"        var result = new {refModel}();");
            foreach (var f in _model.Tables[refModel].Fields)
            {
                if (f.IsNullable)
                    sb.AppendLine($"        result.{f.Name} = reader.IsDBNull(reader.GetOrdinal(\"{ToSqlCase(f.Name)}\")) ? null : reader.Get{MapReaderMethod(f.Type)}(reader.GetOrdinal(\"{ToSqlCase(f.Name)}\"));");
                else
                    sb.AppendLine($"        result.{f.Name} = reader.Get{MapReaderMethod(f.Type)}(reader.GetOrdinal(\"{ToSqlCase(f.Name)}\"));");
            }

            sb.AppendLine($"        return result;");
            sb.AppendLine("    }");
        }

// --- UPDATE
        sb.AppendLine();
        sb.AppendLine($"    public bool Update({table.Name} entity)");
        sb.AppendLine("    {");
        sb.AppendLine($"        using var cmd = _connection.CreateCommand();");
        sb.AppendLine($"        cmd.CommandText = SQL_UPDATE;");

// Primitive felter
        foreach (var f in table.Fields)
        {
            if (f.Name.Equals("id", StringComparison.OrdinalIgnoreCase)) continue;
            sb.AppendLine($"        cmd.Parameters.AddWithValue(\"@{SqlParam(f.Name)}\", entity.{Pascal(f.Name)});");
        }

// Reference felter
        foreach (var r in table.References)
        {
            var svcName = $"_{ToLowerFirst(r.TargetTable)}Service";
            var fkName = SqlParam(r.ColumnName);

            sb.AppendLine($"        var {fkName} = {svcName}.CreateOrGet(entity.{Pascal(r.TargetTable)});");
            sb.AppendLine($"        entity.{Pascal(r.ColumnName)} = {fkName};");
            sb.AppendLine($"        cmd.Parameters.AddWithValue(\"@{fkName}\", {fkName});");
        }

        sb.AppendLine($"        cmd.Parameters.AddWithValue(\"@id\", entity.Id);");
        sb.AppendLine("        return cmd.ExecuteNonQuery() > 0;");
        sb.AppendLine("    }");

        // --- DELETE
        sb.AppendLine();
        sb.AppendLine($"    public bool Delete({MapToClrType(table.PrimaryKey!.Type)} id)");
        sb.AppendLine("    {");
        sb.AppendLine($"        using var cmd = _connection.CreateCommand();");
        sb.AppendLine($"        cmd.CommandText = SQL_DELETE;");
        sb.AppendLine($"        cmd.Parameters.AddWithValue(\"@id\", id);");
        sb.AppendLine("        return cmd.ExecuteNonQuery() > 0;");
        sb.AppendLine("    }");

        // -------------------------
        //   CREATE
        // -------------------------
        sb.AppendLine();
        sb.AppendLine($"    public bool Create({table.Name} entity)");
        sb.AppendLine("    {");
        sb.AppendLine($"        using var cmd = _connection.CreateCommand();");
        sb.AppendLine($"        cmd.CommandText = SQL_INSERT;");

        foreach (var f in table.Fields)
        {
            if (f.Name.Equals("id", StringComparison.OrdinalIgnoreCase)) continue;
            sb.AppendLine($"        cmd.Parameters.AddWithValue(\"@{SqlParam(f.Name)}\", entity.{f.Name});");
        }

        foreach (var r in table.References)
        {
            var svcName = $"_{ToLowerFirst(r.TargetTable)}Service";
            sb.AppendLine($"        var {ToLowerFirst(r.ColumnName)} = {svcName}.CreateOrGet(entity.{r.TargetTable});");
            sb.AppendLine($"        cmd.Parameters.AddWithValue(\"@{SqlParam(r.ColumnName)}\", {ToLowerFirst(r.ColumnName)});");
        }

        sb.AppendLine("        return cmd.ExecuteNonQuery() > 0;");
        sb.AppendLine("    }");

        // --- LoadAll
        sb.AppendLine();
        sb.AppendLine($"    public List<{table.Name}> LoadAll()");
        sb.AppendLine("    {");
        sb.AppendLine($"        var list = new List<{table.Name}>();");
        sb.AppendLine($"        using var cmd = _connection.CreateCommand();");
        sb.AppendLine($"        cmd.CommandText = SQL_SELECT_ALL;");
        sb.AppendLine($"        using var reader = cmd.ExecuteReader();");
        sb.AppendLine($"        while (reader.Read())");
        sb.AppendLine("        {");
        sb.AppendLine($"            var entity = new {table.Name}();");

        foreach (var f in table.Fields)
        {
            if (f.IsNullable)
                sb.AppendLine($"            entity.{f.Name} = reader.IsDBNull(reader.GetOrdinal(\"{ToSqlCase(f.Name)}\")) ? null : reader.Get{MapReaderMethod(f.Type)}(reader.GetOrdinal(\"{ToSqlCase(f.Name)}\"));");
            else
                sb.AppendLine($"            entity.{f.Name} = reader.Get{MapReaderMethod(f.Type)}(reader.GetOrdinal(\"{ToSqlCase(f.Name)}\"));");
        }

        foreach (var r in table.References)
        {
            sb.AppendLine($"            var {ToLowerFirst(ToSqlCase(r.ColumnName))} = reader.GetGuid(reader.GetOrdinal(\"{ToSqlCase(r.ColumnName)}\"));");
            sb.AppendLine($"            entity.{ToLowerFirst(r.TargetTable)} = _{ToLowerFirst(r.TargetTable)}Service.GetById({ToLowerFirst(ToSqlCase(r.ColumnName))});");
        }

        sb.AppendLine($"            list.Add(entity);");
        sb.AppendLine("        }");
        sb.AppendLine($"        return list;");
        sb.AppendLine("    }");

        // --- GetById
        sb.AppendLine();
        sb.AppendLine($"    public {table.Name}? GetById({MapToClrType(table.PrimaryKey!.Type)} id)");
        sb.AppendLine("    {");
        sb.AppendLine($"        using var cmd = _connection.CreateCommand();");
        sb.AppendLine($"        cmd.CommandText = SQL_SELECT_BY_ID;");
        sb.AppendLine($"        cmd.Parameters.AddWithValue(\"@id\", id);");
        sb.AppendLine($"        using var reader = cmd.ExecuteReader();");
        sb.AppendLine($"        if (!reader.Read()) return null;");
        sb.AppendLine();
        sb.AppendLine($"        var entity = new {table.Name}();");

        foreach (var f in table.Fields)
        {
            if (f.IsNullable)
                sb.AppendLine($"        entity.{f.Name} = reader.IsDBNull(reader.GetOrdinal(\"{ToSqlCase(f.Name)}\")) ? null : reader.Get{MapReaderMethod(f.Type)}(reader.GetOrdinal(\"{ToSqlCase(f.Name)}\"));");
            else
                sb.AppendLine($"        entity.{f.Name} = reader.Get{MapReaderMethod(f.Type)}(reader.GetOrdinal(\"{ToSqlCase(f.Name)}\"));");
        }

        foreach (var r in table.References)
        {
            sb.AppendLine($"        var {ToLowerFirst(r.ColumnName)} = reader.GetGuid(reader.GetOrdinal(\"{ToSqlCase(r.ColumnName)}\"));");
            sb.AppendLine($"        entity.{ToLowerFirst(r.TargetTable)} = _{ToLowerFirst(r.TargetTable)}Service.GetById({ToLowerFirst(ToSqlCase(r.ColumnName))});");
        }

        sb.AppendLine($"        return entity;");
        sb.AppendLine("    }");

        sb.AppendLine("}");
        File.WriteAllText(Path.Combine(servicePath, $"{className}.cs"), sb.ToString());
    }

    private string MapReaderMethod(SQLType type) => type switch
    {
        SQLType.Int => "Int32",
        SQLType.Real => "Double",
        SQLType.Varchar => "String",
        SQLType.Guid => "Guid",
        SQLType.Boolean => "Boolean",
        SQLType.Blob => "FieldValue<byte[]>",
        _ => "Value"
    };

    private string EscapeSql(string value) =>
        value.Replace("\"", "\"\"").Replace("\r", "").Replace("\n", " ");

    private string ToLowerFirst(string v) => char.ToLower(v[0]) + v.Substring(1);

    private string ToSqlCase(string name)
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

    private string Pascal(string name) =>
        char.ToUpper(name[0]) + name.Substring(1);

    private string MapToClrType(SQLType t) => t switch
    {
        SQLType.Int => "int",
        SQLType.Real => "double",
        SQLType.Varchar => "string",
        SQLType.Guid => "Guid",
        SQLType.Boolean => "bool",
        SQLType.Blob => "byte[]",
        _ => "object"
    };

    private string SqlParam(string name) => $"{ToSqlCase(name)}";
}