using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;

namespace MsSqlHelpers
{
    public class MapperBuilder<T> where T : class
    {
        private readonly Mapper<T> _mapper;

        public MapperBuilder()
        {
            _mapper = new Mapper<T>();
        }

        public MapperBuilder(string tableName)
        {
            _mapper = new Mapper<T>(tableName);
        }

        public MapperBuilder<T> SetTableName(string tableName)
        {
            _mapper.TableName = tableName;

            return this;
        }

        public MapperBuilder<T> AddMapping(string propertyName, string columnName)
        {
            _mapper.Mappings.Add(propertyName, columnName);

            return this;
        }

        public MapperBuilder<T> AddMapping<TProperty>(
            Expression<Func<T, TProperty>> propertyLambda, string columnName)
        {
            var memberExpression = propertyLambda.Body as MemberExpression ??
                throw new ArgumentException($"{nameof(propertyLambda)}.Body must be a MemberExpression.", nameof(propertyLambda));
            var propertyInfo = memberExpression.Member as PropertyInfo ??
                throw new ArgumentException($"{nameof(memberExpression)}.Member must be a PropertyInfo.", nameof(propertyLambda));
            _mapper.Mappings.Add(propertyInfo.Name, columnName);

            return this;
        }

        public MapperBuilder<T> SetMappings(Dictionary<string, string> mappings)
        {
            _mapper.Mappings = mappings;

            return this;
        }

        public Mapper<T> Build() => _mapper;
    }
}
