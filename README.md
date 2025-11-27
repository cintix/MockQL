
# MockQL – SQLite ORM Generator

MockQL er et lille, hurtigt og selvkørende ORM-generator‐framework.  
Det tager POCO-klasser som input og genererer automatisk:

✔ SQLite-tabeller  
✔ SQL (Create, Insert, Select, Update, Delete)  
✔ ORM-model-klasser  
✔ Services med CRUD + relationer  

Du skriver bare klasser – MockQL gør resten.

## Quick Start

```bash
dotnet run
```

MockQL scanner dine modeller og genererer:

```
MockQL/
 ├─ Models/
 ├─ Services/
 └─ SQLite/
```

## Eksempel – POCO input

```csharp
public class Worker
{
    public string Name { get; set; }
    public int Age { get; set; }
    public Job Job { get; set; }
}

public class Job
{
    public string Name { get; set; }
    public double Cash { get; set; }
}
```

## Genereret database (SQLite)

```sql
CREATE TABLE worker (
    id BLOB PRIMARY KEY NOT NULL DEFAULT (lower(hex(randomblob(16)))),
    name TEXT NOT NULL,
    age INTEGER NOT NULL,
    job_id BLOB NOT NULL,
    FOREIGN KEY(job_id) REFERENCES job(id)
);
```

## NuGet

```bash
dotnet add package Microsoft.Data.Sqlite
```

## Roadmap

| Fase | Status |
|------|--------|
| Model scanning | ✔ |
| SQL generation | ✔ |
| ORM models | ✔ |
| CRUD services | ✔ |
| Interface / DI | 🔜 |
