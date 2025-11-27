namespace Cintix.MockQL.Infrastructure.Domain;

public enum SQLType
{
    Int,
    Real,
    Varchar,
    Guid,
    Boolean,
    Blob
}

public class SQLField
{
    public string Name { get; }
    public SQLType Type { get; }
    public bool IsNullable { get; }

    public SQLField(string name, SQLType type, bool isNullable)
    {
        Name = name;
        Type = type;
        IsNullable = isNullable;
    }
}

public class SQLReference
{
    public string ColumnName { get; }
    public string TargetTable { get; }

    public SQLReference(string columnName, string targetTable)
    {
        ColumnName = columnName;
        TargetTable = targetTable;
    }
}

public class SQLTable
{
    public string Name { get; }
    public List<SQLField> Fields { get; } = new();
    public List<SQLReference> References { get; } = new();
    public SQLField? PrimaryKey { get; set; }
    public Dictionary<string, string> SQLActions { get; } = new();

    public SQLTable(string name)
    {
        Name = name;
    }
}

public class ModelDefinition
{
    public Dictionary<string, SQLTable> Tables { get; } = new();

    public void AddTable(SQLTable table)
    {
        Tables[table.Name] = table;
    }
}
