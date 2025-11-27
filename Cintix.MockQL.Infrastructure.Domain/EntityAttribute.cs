namespace Cintix.MockQL.Infrastructure.Domain;

[AttributeUsage(AttributeTargets.Class)]
public class EntityAttribute(Type manager) : Attribute
{
    public Type Manager { get; } = manager;
}