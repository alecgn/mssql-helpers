using Dapper;
using MsSqlHelpers.Interfaces;
#if NETFRAMEWORK
using System.Data.SqlClient;
#else
using Microsoft.Data.SqlClient;
#endif
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace MsSqlHelpers
{
    public class MsSqlQueryGenerator : IMsSqlQueryGenerator
    {
        public const int MaxAllowedBatchSize = 1000;
        public const int MaxAllowedSqlParametersCount = (2100 - 1);

        public IEnumerable<(string SqlQuery, IEnumerable<SqlParameter> SqlParameters)> GenerateParametrizedBulkInserts<T>(
            Mapper<T> mapper, 
            IEnumerable<T> collectionOfObjects,
            bool allowIdentityInsert = false)
            where T : class
        {
            ValidateParameters(mapper, collectionOfObjects);

            return GenerateSqlQueriesAndParameters(mapper, collectionOfObjects, allowIdentityInsert);
        }

        public IEnumerable<(string SqlQuery, DynamicParameters DapperDynamicParameters)> GenerateDapperParametrizedBulkInserts<T>(
            Mapper<T> mapper, 
            IEnumerable<T> collectionOfObjects,
            bool allowIdentityInsert = false)
            where T : class
        {
            ValidateParameters(mapper, collectionOfObjects);

            return GenerateSqlQueriesAndDapperDynamicParameters(mapper, collectionOfObjects, allowIdentityInsert);
        }

        private static void ValidateParameters<T>(Mapper<T> mapper, IEnumerable<T> collectionOfObjects)
            where T : class
        {
            var mapperValidationResult = mapper.Validate();

            if (!mapperValidationResult.IsValid)
            {
                throw new ArgumentException(string.Join(Environment.NewLine, mapperValidationResult.ValidationErrors));
            }

            if (collectionOfObjects is null || !collectionOfObjects.Any())
            {
                throw new ArgumentException($@"Parameter ""{nameof(collectionOfObjects)}"" can not be null or empty.", nameof(collectionOfObjects));
            }
        }

        private IEnumerable<(string SqlQuery, IEnumerable<SqlParameter> SqlParameters)> GenerateSqlQueriesAndParameters<T>(
            Mapper<T> mapper, 
            IEnumerable<T> collectionOfObjects,
            bool allowIdentityInsert = false)
            where T : class
        {
            var numberOfObjectsPerInsert = ((int)Math.Floor((double)MaxAllowedSqlParametersCount / mapper.Mappings.Count));
            numberOfObjectsPerInsert = Math.Min(numberOfObjectsPerInsert, MaxAllowedBatchSize);
            var numberOfBatches = (int)Math.Ceiling((double)collectionOfObjects.Count() / numberOfObjectsPerInsert);

            for (int batchNumber = 1; batchNumber <= numberOfBatches; batchNumber++)
            {
                var collectionOfObjectsToInsert = collectionOfObjects.Skip((batchNumber - 1) * numberOfObjectsPerInsert).Take(numberOfObjectsPerInsert);
                var columnsDefinition = GenerateColumnsDefinitionSql(mapper);
                var values = GenerateValuesSql(collectionOfObjectsToInsert, mapper);
                var sqlQuery = new StringBuilder()
                    .Append("SET NOCOUNT ON; ")
                    .Append(allowIdentityInsert ? $"SET IDENTITY_INSERT [{mapper.TableName}] ON; " : "")
                    .Append($"INSERT INTO [{mapper.TableName}] ")
                    .Append(columnsDefinition)
                    .Append(" VALUES ")
                    .Append(values);

                yield return (SqlQuery: sqlQuery.ToString(), SqlParameters: GenerateSqlParameters(collectionOfObjectsToInsert, mapper));
            }
        }

        private IEnumerable<(string SqlQuery, DynamicParameters DapperDynamicParameters)> GenerateSqlQueriesAndDapperDynamicParameters<T>(
            Mapper<T> mapper, 
            IEnumerable<T> collectionOfObjects,
            bool allowIdentityInsert = false)
            where T : class
        {
            var numberOfObjectsPerInsert = ((int)Math.Floor((double)MaxAllowedSqlParametersCount / mapper.Mappings.Count));
            numberOfObjectsPerInsert = Math.Min(numberOfObjectsPerInsert, MaxAllowedBatchSize);
            var numberOfBatches = (int)Math.Ceiling((double)collectionOfObjects.Count() / numberOfObjectsPerInsert);

            for (int batchNumber = 1; batchNumber <= numberOfBatches; batchNumber++)
            {
                var collectionOfObjectsToInsert = collectionOfObjects.Skip((batchNumber - 1) * numberOfObjectsPerInsert).Take(numberOfObjectsPerInsert);
                var columnsDefinition = GenerateColumnsDefinitionSql(mapper);
                var values = GenerateValuesSql(collectionOfObjectsToInsert, mapper);
                var sqlQuery = new StringBuilder()
                    .Append("SET NOCOUNT ON; ")
                    .Append(allowIdentityInsert ? $"SET IDENTITY_INSERT [{mapper.TableName}] ON; " : "")
                    .Append($"INSERT INTO [{mapper.TableName}] ")
                    .Append(columnsDefinition)
                    .Append(" VALUES ")
                    .Append(values);

                yield return (SqlQuery: sqlQuery.ToString(), DapperDynamicParameters: GenerateDapperDynamicParameters(collectionOfObjectsToInsert, mapper));
            }
        }

        private StringBuilder GenerateColumnsDefinitionSql<T>(Mapper<T> mapper) where T : class => new StringBuilder()
            .Append('(')
            .Append(string.Join(", ", mapper.Mappings.Select(mapping => $"[{mapping.Value}]")))
            .Append(')');

        private StringBuilder GenerateValuesSql<T>(IEnumerable<T> collectionOfObjects, Mapper<T> mapper)
            where T : class
        {
            var valuesSql = new StringBuilder();
            var objectsProcessed = 0;
            var offset = 0;
            var collectionOfObjectsCount = collectionOfObjects.Count();

            foreach (var @object in collectionOfObjects)
            {
                var objectPropertiesInfo = @object.GetType().GetProperties();
                var filteredObjectPropertiesInfo = objectPropertiesInfo.Where(pi => mapper.Mappings.Keys.Contains(pi.Name));
                var propertyValues = GetPropertyValues(filteredObjectPropertiesInfo, @object);
                valuesSql
                    .Append('(')
                    .Append(string.Join(", ", Enumerable.Range(offset, propertyValues.Count).Select(parameterIndex => $"@p{parameterIndex}")))
                    .Append(')');
                objectsProcessed++;
                offset += propertyValues.Count;
                valuesSql.Append(objectsProcessed < collectionOfObjectsCount ? ", " : ";");
            }

            return valuesSql;
        }

        private List<object> GetPropertyValues(IEnumerable<PropertyInfo> propertiesInfo, object @object)
        {
            var propertyValues = new List<object>();

            foreach (var propertyInfo in propertiesInfo)
            {
                propertyValues.Add(GetPropertyValue(@object, propertyInfo.Name));
            }

            return propertyValues;
        }

        private object GetPropertyValue(object @object, string propertyName) =>
            @object.GetType().GetProperty(propertyName).GetValue(@object, null);

        private IEnumerable<SqlParameter> GenerateSqlParameters<T>(IEnumerable<T> collectionOfObjects, Mapper<T> mapper)
            where T : class
        {
            var sqlParameters = new List<SqlParameter>();
            var parameterIndex = 0;

            foreach (var @object in collectionOfObjects)
            {
                foreach (var mapping in mapper.Mappings)
                {
                    sqlParameters.Add(new SqlParameter($"@p{parameterIndex}", GetPropertyValue(@object, mapping.Key) ?? DBNull.Value));
                    parameterIndex++;
                }
            }

            return sqlParameters;
        }

        private DynamicParameters GenerateDapperDynamicParameters<T>(IEnumerable<T> collectionOfObjects, Mapper<T> mapper)
            where T : class
        {
            var dapperDynamicParameters = new DynamicParameters();
            var parameterIndex = 0;

            foreach (var @object in collectionOfObjects)
            {
                foreach (var mapping in mapper.Mappings)
                {
                    dapperDynamicParameters.Add($"@p{parameterIndex}", GetPropertyValue(@object, mapping.Key));
                    parameterIndex++;
                }
            }

            return dapperDynamicParameters;
        }
    }
}
