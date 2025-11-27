namespace Cintix.MockQL.Infrastructure.Domain.Models;

public class ModelDefinition
{
    public Dictionary<string, SQLTable> Tables { get; } = new();

    public void AddTable(SQLTable table)
    {
        Tables[table.Name] = table;
    }
}
