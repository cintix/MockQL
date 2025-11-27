using System;
using Cintix.MockQL.Infrastructure.Application;
using Cintix.MockQL.Infrastructure.Domain;
using Cintix.MockQL.Infrastructure.SQLite;

namespace Cintix.MockQL.Infrastructure.Demo;

public class Program
{
    public static void Main(string[] args)
    {
        var model = ModelConverter.Convert(typeof(Worker));
        ModelWriter.Build(model,"Cintix.Demo","/mnt/data/projects/MockQLDemo/");
        Console.WriteLine($"TOTAL TABLES: {model.Tables.Count}");
        foreach (var table in model.Tables.Values)
        {
            Console.WriteLine($"========== {table.Name} ==========");
            Console.WriteLine(table.SQLActions["CREATE_TABLE_SQL"]);
            Console.WriteLine();
            Console.WriteLine(table.SQLActions["INSERT_SQL"]);
            Console.WriteLine();
            Console.WriteLine(table.SQLActions["UPDATE_SQL"]);
            Console.WriteLine();
            Console.WriteLine(table.SQLActions["DELETE_SQL"]);
            Console.WriteLine();
            Console.WriteLine(table.SQLActions["SELECT_ALL_SQL"]);
            Console.WriteLine();
            Console.WriteLine(table.SQLActions["SELECT_BY_ID_SQL"]);
            Console.WriteLine();
        }
    }
}

public class Worker
{
    public required Guid Id;
    public required string Name;
    public string? Email;
    public required int Age;
    public required Job Job;
}

public class Job
{
    public required Guid Id;
    public required string Name;
    public required double Cash;
}
