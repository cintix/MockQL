namespace Cintix.MockQL.Infrastructure.Domain.Models;

public class SQLReference(string columnName, string targetTable)
{
    public string ColumnName { get; } = columnName;
    public string TargetTable { get; } = targetTable;
}
