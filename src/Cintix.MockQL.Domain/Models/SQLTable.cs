namespace Cintix.MockQL.Infrastructure.Domain.Models;

public class SQLTable(string name)
{
    public string Name { get; } = name;
    public SQLField? PrimaryKey { get; set; }
    public List<SQLField> Fields { get; } = new();
    public List<SQLReference> References { get; } = new();
    public Dictionary<string, string> SQLActions { get; } = new();
}
