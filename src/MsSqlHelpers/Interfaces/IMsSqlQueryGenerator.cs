#if NETFRAMEWORK
using System.Data.SqlClient;
#else
using Microsoft.Data.SqlClient;
#endif
using System.Collections.Generic;

namespace MsSqlHelpers.Interfaces
{
    public interface IMsSqlQueryGenerator
    {
        IEnumerable<(string SqlQuery, IEnumerable<SqlParameter> SqlParameters)> GenerateParametrizedBulkInserts<T>(Mapper<T> mapper, IEnumerable<T> collectionOfObjects) 
            where T : class;
    }
}
