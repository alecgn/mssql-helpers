using Microsoft.Data.SqlClient;
using System.Collections.Generic;

namespace MsSqlHelpers.Interfaces
{
    public interface IMsSqlQueryGenerator
    {
        IEnumerable<(string SqlQuery, IEnumerable<SqlParameter> SqlParameters)> GenerateParametrizedBulkInserts<T>(Mapper<T> mapper, IEnumerable<T> collectionOfObjects) 
            where T : class;
    }
}
