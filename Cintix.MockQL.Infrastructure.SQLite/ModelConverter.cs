using System;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using System.Diagnostics.CodeAnalysis;
using Cintix.MockQL.Infrastructure.Domain;

namespace Cintix.MockQL.Infrastructure.SQLite;

public static class ModelConverter
{
    private static readonly NullabilityInfoContext NullabilityContext = new();

    public static ModelDefinition Convert(params Type[] types)
    {
        var result = new ModelDefinition();

        foreach (var type in types)
            ConvertType(type, result);

        return result;
    }

    private static void ConvertType(Type type, ModelDefinition model)
    {
        string tableName = type.Name;
        if (model.Tables.ContainsKey(tableName))
            return;

        var table = new SQLTable(tableName);

        var members = type.GetMembers(
            BindingFlags.Instance |
            BindingFlags.Public |
            BindingFlags.NonPublic);

        foreach (var member in members)
        {
            Type? memberType = null;
            string name;
            bool isNullableMember = false;

            switch (member)
            {
                case PropertyInfo prop:
                    if (!prop.CanRead || !prop.CanWrite) continue;
                    memberType = prop.PropertyType;
                    name = prop.Name;

                    var propNullInfo = NullabilityContext.Create(prop);
                    isNullableMember =
                        propNullInfo.ReadState == NullabilityState.Nullable ||
                        propNullInfo.WriteState == NullabilityState.Nullable;
                    break;

                case FieldInfo field:
                    if (field.IsDefined(typeof(System.Runtime.CompilerServices.CompilerGeneratedAttribute), true))
                        continue;
                    memberType = field.FieldType;
                    name = field.Name;

                    var fieldNullInfo = NullabilityContext.Create(field);
                    isNullableMember =
                        fieldNullInfo.ReadState == NullabilityState.Nullable ||
                        fieldNullInfo.WriteState == NullabilityState.Nullable;
                    break;

                default:
                    continue;
            }

            if (name.Equals("id", StringComparison.OrdinalIgnoreCase))
            {
                if (!IsValidIdType(memberType))
                    throw new Exception($"Invalid ID type: {memberType!.Name}. Use Guid, int or long.");

                var idField = new SQLField(name, MapToSqlType(memberType!), false);
                table.PrimaryKey = idField;
                table.Fields.Add(idField);
                continue;
            }

            if (IsPrimitive(memberType!, out SQLType sqlType))
            {
                table.Fields.Add(new SQLField(name, sqlType, isNullableMember));
            }
            else
            {
                table.References.Add(new SQLReference($"{name}_id", memberType!.Name));
                ConvertType(memberType!, model);
            }
        }

        if (table.PrimaryKey == null)
        {
            table.PrimaryKey = new SQLField("id", SQLType.Guid, false);
            table.Fields.Insert(0, table.PrimaryKey);
        }

        SqlGenerator.GenerateSQLActionMaps(table);
        model.AddTable(table);
    }

    private static bool IsPrimitive(Type t, out SQLType sqlType)
    {
        if (t == typeof(int)) { sqlType = SQLType.Int; return true; }
        if (t == typeof(double)) { sqlType = SQLType.Real; return true; }
        if (t == typeof(string)) { sqlType = SQLType.Varchar; return true; }
        if (t == typeof(Guid)) { sqlType = SQLType.Guid; return true; }
        if (t == typeof(bool)) { sqlType = SQLType.Boolean; return true; }
        if (t == typeof(byte[])) { sqlType = SQLType.Blob; return true; }

        sqlType = default;
        return false;
    }

    private static SQLType MapToSqlType(Type type)
    {
        if (type == typeof(int) || type == typeof(long))
            return SQLType.Int;

        if (type == typeof(Guid))
            return SQLType.Guid;

        throw new Exception($"Unsupported ID type: {type.Name}");
    }

    private static bool IsValidIdType(Type type)
    {
        if (type == typeof(Guid)) return true;
        if (type == typeof(int) || type == typeof(long)) return true;
        return false;
    }
}
