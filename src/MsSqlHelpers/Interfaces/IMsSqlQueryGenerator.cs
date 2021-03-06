﻿using Dapper;
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
        IEnumerable<(string SqlQuery, IEnumerable<SqlParameter> SqlParameters)> GenerateParametrizedBulkInserts<T>(
            Mapper<T> mapper, 
            IEnumerable<T> collectionOfObjects,
            bool allowIdentityInsert = false) 
            where T : class;

        IEnumerable<(string SqlQuery, DynamicParameters DapperDynamicParameters)> GenerateDapperParametrizedBulkInserts<T>(
            Mapper<T> mapper, 
            IEnumerable<T> collectionOfObjects,
            bool allowIdentityInsert = false)
            where T : class;
    }
}
