using MsSqlHelpers.Interfaces;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace MsSqlHelpers
{
    public class MsSqlQueryGenerator : IMsSqlQueryGenerator
    {
        private const int MaxBatchSize = 1000;

        public IEnumerable<(string SqlQuery, IEnumerable<SqlParameter> SqlParameters)> GenerateParametrizedBulkInserts<T>(Mapper<T> mapper, IEnumerable<T> collectionOfObjects) 
            where T : class
        {
            ValidateParameters(mapper, collectionOfObjects);

            return GenerateSqlQueriesAndParameters(mapper, collectionOfObjects);
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
                throw new ArgumentException(@$"Parameter ""{nameof(collectionOfObjects)}"" can not be null or empty.", nameof(collectionOfObjects));
            }
        }

        private IEnumerable<(string SqlQuery, IEnumerable<SqlParameter> SqlParameters)> GenerateSqlQueriesAndParameters<T>(Mapper<T> mapper, IEnumerable<T> collectionOfObjects) 
            where T : class
        {
            var numberOfBatches = (int)Math.Ceiling((double)collectionOfObjects.Count() / MaxBatchSize);

            for (int batchNumber = 1; batchNumber <= numberOfBatches; batchNumber++)
            {
                yield return GenerateSqlQueryAndParameters(mapper, collectionOfObjects, batchNumber);
            }
        }

        private (string SqlQuery, IEnumerable<SqlParameter> SqlParameters) GenerateSqlQueryAndParameters<T>(Mapper<T> mapper, IEnumerable<T> collectionOfObjects, int batchNumber)
            where T : class
        {
            var collectionOfObjectsToInsert = collectionOfObjects.Skip((batchNumber - 1) * MaxBatchSize).Take(MaxBatchSize);
            var columnsDefinition = GenerateColumnsDefinitionSql(mapper);
            var values = GenerateValuesSql(collectionOfObjectsToInsert, mapper);
            var sqlQuery = new StringBuilder()
                .Append("SET NOCOUNT ON; ")
                .Append($"INSERT INTO [{mapper.TableName}] ")
                .Append(columnsDefinition)
                .Append(" VALUES ")
                .Append(values);

            return (SqlQuery: sqlQuery.ToString(), SqlParameters: GenerateSqlParameters(collectionOfObjectsToInsert, mapper));
        }

        private StringBuilder GenerateColumnsDefinitionSql<T>(Mapper<T> mapper) where T : class => new StringBuilder()
            .Append('(')
            .AppendJoin(", ", mapper.Mappings.Select(mapping => $"[{mapping.Value}]"))
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
                    .AppendJoin(", ", Enumerable.Range(offset, propertyValues.Count).Select(parameterIndex => $"@p{parameterIndex}"))
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
    }
}
