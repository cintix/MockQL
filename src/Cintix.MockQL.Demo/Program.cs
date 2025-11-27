using System;
using Cintix.MockQL.Infrastructure.Application;
using Cintix.MockQL.Infrastructure.Application.Services.ModelManagement;
using Cintix.MockQL.Infrastructure.Domain;
using Cintix.MockQL.Infrastructure.SQLite;

namespace Cintix.MockQL.Infrastructure.Demo;

public class Program
{
    private readonly IModelConverter _modelConverter = new ModelConverter();
    private readonly IModelWriter _modelWriter = new ModelWriter();

    public static void Main(string[] args)
    {
        Program program = new Program();
        program.RunDemo();
    }

    private void RunDemo()
    {
        var model = _modelConverter.Convert(typeof(Worker));
        _modelWriter.Build(model, "Cintix.Demo", "/mnt/data/projects/MockQLDemo/");
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