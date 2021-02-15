# MsSqlHelpers

MsSqlHelpers is a library to improve MS SQL Server common development tasks, like generating parametrized bulk inserts to be used with ADO.NET, Entity Framework and Dapper, and more (in a near future).

**ADO.NET usage:**

```csharp
using MsSqlHelpers;

...

var mapper = new MapperBuilder<Person>()
    .SetTableName("People")
    .AddMapping(propertyName: nameof(Person.FirstName), columnName: "Name")
    .AddMapping(propertyName: nameof(Person.LastName), columnName: "Surename")
    .AddMapping(propertyName: nameof(Person.DateOfBirth), columnName: "Birthday")
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
    
    // Default batch size: 1000 rows per insert.
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
    .AddMapping(propertyName: nameof(Person.FirstName), columnName: "Name")
    .AddMapping(propertyName: nameof(Person.LastName), columnName: "Surename")
    .AddMapping(propertyName: nameof(Person.DateOfBirth), columnName: "Birthday")
    .Build();
var people = new List<Person>()
{ 
    new Person() { FirstName = "John", LastName = "Lennon", DateOfBirth = new DateTime(1940, 10, 9) },
    new Person() { FirstName = "Paul", LastName = "McCartney", DateOfBirth = new DateTime(1942, 6, 18) },
};
var sqlQueriesAndParameters = new MsSqlQueryGenerator().GenerateParametrizedBulkInserts(mapper, people);

// Default batch size: 1000 rows per insert.
foreach (var (SqlQuery, SqlParameters) in sqlQueriesAndParameters)
{
    await _context.Database.ExecuteSqlRawAsync(SqlQuery, SqlParameters);
    // Depracated but still works: await _context.Database.ExecuteSqlCommand(SqlQuery, SqlParameters);
}
```

**Dapper usage:**

```csharp
using MsSqlHelpers;

...

var mapper = new MapperBuilder<Person>()
    .SetTableName("People")
    .AddMapping(propertyName: nameof(Person.FirstName), columnName: "Name")
    .AddMapping(propertyName: nameof(Person.LastName), columnName: "Surename")
    .AddMapping(propertyName: nameof(Person.DateOfBirth), columnName: "Birthday")
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
    // Default batch size: 1000 rows per insert.
    foreach (var (SqlQuery, DapperDynamicParameters) in sqlQueriesAndDapperParameters)
    {
        sqlConnection.Execute(SqlQuery, DapperDynamicParameters);
    }
}
```
