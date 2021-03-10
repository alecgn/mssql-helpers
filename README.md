# MsSqlHelpers
[![Build and tests status (mssql-helpers)](https://github.com/alecgn/mssql-helpers/workflows/build-and-test/badge.svg)](#)
[![Nuget version (mssql-helpers)](https://img.shields.io/nuget/v/MsSqlHelpers)](https://nuget.org/packages/MsSqlHelpers) 
[![Nuget downloads (MsSqlHelpers)](https://img.shields.io/nuget/dt/MsSqlHelpers)](https://nuget.org/packages/MsSqlHelpers)

MsSqlHelpers is a library to improve MS SQL Server common development tasks, like generating parametrized bulk inserts to be used with ADO.NET, Entity Framework and Dapper, and more (in a near future).

**ADO.NET usage:**

```csharp
using MsSqlHelpers;

...

var mapper = new MapperBuilder<Person>()
    .SetTableName("People")
    .AddMapping(person => person.FirstName, columnName: "Name")
    .AddMapping(person => person.LastName, columnName: "Surename")
    .AddMapping(person => person.DateOfBirth, columnName: "Birthday")
    .Build();
var people = new List<Person>()
{ 
    new Person() { FirstName = "John", LastName = "Lennon", DateOfBirth = new DateTime(1940, 10, 9) },
    new Person() { FirstName = "Paul", LastName = "McCartney", DateOfBirth = new DateTime(1942, 6, 18) },
};
var connectionString = "Server=SERVER_ADDRESS;Database=DATABASE_NAME;User Id=USERNAME;Password=PASSWORD;";
var sqlQueriesAndParameters = new MsSqlQueryGenerator().GenerateParametrizedBulkInserts(mapper, people);

using (var sqlConnection = new SqlConnection(connectionString))
{
    sqlConnection.Open();
    
    // Default batch size: 1000 rows or (2100-1) parameters per insert.
    foreach (var (SqlQuery, SqlParameters) in sqlQueriesAndParameters)
    {
        using (SqlCommand sqlCommand = new SqlCommand(SqlQuery, sqlConnection))
        {
            sqlCommand.Parameters.AddRange(SqlParameters.ToArray());
            sqlCommand.ExecuteNonQuery();
        }
    }
}
```

**Entity Framework usage:**

```csharp
using MsSqlHelpers;

...

var mapper = new MapperBuilder<Person>()
    .SetTableName("People")
    .AddMapping(person => person.FirstName, columnName: "Name")
    .AddMapping(person => person.LastName, columnName: "Surename")
    .AddMapping(person => person.DateOfBirth, columnName: "Birthday")
    .Build();
var people = new List<Person>()
{ 
    new Person() { FirstName = "John", LastName = "Lennon", DateOfBirth = new DateTime(1940, 10, 9) },
    new Person() { FirstName = "Paul", LastName = "McCartney", DateOfBirth = new DateTime(1942, 6, 18) },
};
var sqlQueriesAndParameters = new MsSqlQueryGenerator().GenerateParametrizedBulkInserts(mapper, people);

// Default batch size: 1000 rows or (2100-1) parameters per insert.
foreach (var (SqlQuery, SqlParameters) in sqlQueriesAndParameters)
{
    _context.Database.ExecuteSqlRaw(SqlQuery, SqlParameters);
    // Depracated but still works: _context.Database.ExecuteSqlCommand(SqlQuery, SqlParameters);
}
```

**Dapper usage:**

```csharp
using MsSqlHelpers;

...

var mapper = new MapperBuilder<Person>()
    .SetTableName("People")
    .AddMapping(person => person.FirstName, columnName: "Name")
    .AddMapping(person => person.LastName, columnName: "Surename")
    .AddMapping(person => person.DateOfBirth, columnName: "Birthday")
    .Build();
var people = new List<Person>()
{ 
    new Person() { FirstName = "John", LastName = "Lennon", DateOfBirth = new DateTime(1940, 10, 9) },
    new Person() { FirstName = "Paul", LastName = "McCartney", DateOfBirth = new DateTime(1942, 6, 18) },
};
var connectionString = "Server=SERVER_ADDRESS;Database=DATABASE_NAME;User Id=USERNAME;Password=PASSWORD;";
var sqlQueriesAndDapperParameters = new MsSqlQueryGenerator().GenerateDapperParametrizedBulkInserts(mapper, people);

using (var sqlConnection = new SqlConnection(connectionString))
{
    // Default batch size: 1000 rows or (2100-1) parameters per insert.
    foreach (var (SqlQuery, DapperDynamicParameters) in sqlQueriesAndDapperParameters)
    {
        sqlConnection.Execute(SqlQuery, DapperDynamicParameters);
    }
}
```
