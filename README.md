# MsSqlHelpers

MsSqlHelpers is a library to improve MS SQL Server common development tasks, like generating parametrized bulk inserts to be used with ADO.NET, Entity Framework and Dapper, and more (in a near future).

ADO.NET usage:

```csharp
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
