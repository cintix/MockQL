using System.Collections.Concurrent;
using Microsoft.Data.Sqlite;

namespace Cintix.MockQL.Infrastructure.SQLite;


public static class DatasourceManager
{
    private static readonly ConcurrentDictionary<string, SqliteConnection> _dataSources = new();

    public static SqliteConnection GetInstance(string path)
    {
        if (_dataSources.TryGetValue(path, out var existing))
            return existing;

        var conn = new SqliteConnection($"Data Source={path}");
        conn.Open();
        _dataSources[path] = conn;
        return conn;
    }

    public static void RemoveDataSource(string path)
    {
        if (_dataSources.TryRemove(path, out var conn))
        {
            conn.Close();
            conn.Dispose();
        }
    }
}