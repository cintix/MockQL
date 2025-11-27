using System.Reflection;
using Cintix.MockQL.Infrastructure.Domain.Enums;
using Cintix.MockQL.Infrastructure.Domain.Models;
using Cintix.MockQL.Infrastructure.SQLite;

namespace Cintix.MockQL.Infrastructure.Application.Services.ModelManagement;

public class ModelConverter : IModelConverter
{
    public ModelDefinition Convert(params Type[] types)
    {
        var result = new ModelDefinition();

        foreach (var type in types)
        {
            ConvertType(type, result);
        }

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
            Type? memberType;
            string name;
            bool isNullableMember = false;

            switch (member)
            {
                case PropertyInfo propertyInfo:
                    if (!propertyInfo.CanRead || !propertyInfo.CanWrite)
                        continue;
                    memberType = propertyInfo.PropertyType;
                    name = propertyInfo.Name;
                    break;

                case FieldInfo fieldInfo:
                    if (fieldInfo.IsDefined(typeof(System.Runtime.CompilerServices.CompilerGeneratedAttribute), true))
                        continue;
                    memberType = fieldInfo.FieldType;
                    name = fieldInfo.Name;
                    break;

                default:
                    continue;
            }

            if (string.Equals(name, "id", StringComparison.OrdinalIgnoreCase))
            {
                if (!IsValidIdType(memberType))
                    throw new Exception($"Invalid ID type: {memberType.Name}. Use Guid, int or long.");

                var idField = new SQLField(name, MapToSqlType(memberType), false);
                table.PrimaryKey = idField;
                table.Fields.Add(idField);
                continue;
            }

            if (IsPrimitive(memberType, out SQLType sqlType))
            {
                if (Nullable.GetUnderlyingType(memberType) != null)
                    isNullableMember = true;
                else if (memberType == typeof(string))
                    isNullableMember = true;

                table.Fields.Add(new SQLField(name, sqlType, isNullableMember));
            }
            else
            {
                table.References.Add(new SQLReference($"{name}_id", memberType.Name));
                ConvertType(memberType, model);
            }
        }

        if (table.PrimaryKey == null)
        {
            table.PrimaryKey = new SQLField("id", SQLType.Guid, false);
            table.Fields.Insert(0, table.PrimaryKey);
        }

        // SqlGenerator will be injected/used by caller in the refactor;
        // here we only build the in-memory model.
        SqlGenerator.GenerateSQLActionMaps(table);
        model.AddTable(table);
    }

    private static bool IsPrimitive(Type type, out SQLType sqlType)
    {
        if (type == typeof(int)) { sqlType = SQLType.Int; return true; }
        if (type == typeof(double)) { sqlType = SQLType.Real; return true; }
        if (type == typeof(string)) { sqlType = SQLType.Varchar; return true; }
        if (type == typeof(Guid)) { sqlType = SQLType.Guid; return true; }
        if (type == typeof(bool)) { sqlType = SQLType.Boolean; return true; }
        if (type == typeof(byte[])) { sqlType = SQLType.Blob; return true; }

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
