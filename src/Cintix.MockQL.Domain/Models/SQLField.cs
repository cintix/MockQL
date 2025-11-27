using Cintix.MockQL.Infrastructure.Domain.Enums;

namespace Cintix.MockQL.Infrastructure.Domain.Models;

public class SQLField(string name, SQLType type, bool isNullable)
{
    public string Name { get; } = name;
    public SQLType Type { get; } = type;
    public bool IsNullable { get; } = isNullable;
}
