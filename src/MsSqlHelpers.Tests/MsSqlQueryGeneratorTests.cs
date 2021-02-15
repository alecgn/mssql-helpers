using Bogus;
using FluentAssertions;
using Microsoft.Data.SqlClient;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace MsSqlHelpers.Tests
{
    public class MsSqlQueryGeneratorTests
    {
        private MsSqlQueryGenerator _msSqlQueryGenerator;

        [OneTimeSetUp]
        public void Setup()
        {
            _msSqlQueryGenerator = new MsSqlQueryGenerator();
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("   ")]
        [Test]
        public void ShouldThrowArgumentException_WhenMapperTableName_IsNullEmptyOrWhiteSpace(string tableName)
        {
            var mapper = new MapperBuilder<Person>()
                .SetTableName(tableName)
                .AddMapping(nameof(Person.FirstName), nameof(Person.FirstName))
                .AddMapping(nameof(Person.LastName), nameof(Person.LastName))
                .AddMapping(nameof(Person.DateOfBirth), nameof(Person.DateOfBirth))
                .Build();
            var people = new Faker<Person>()
                .RuleFor(p => p.FirstName, p => p.Person.FirstName)
                .RuleFor(p => p.LastName, p => p.Person.LastName)
                .RuleFor(p => p.DateOfBirth, p => p.Person.DateOfBirth)
                .GenerateLazy(2);

            Action act = () => _msSqlQueryGenerator.GenerateParametrizedBulkInserts(mapper, people);

            act.Should().ThrowExactly<ArgumentException>();
        }

        [TestCase(null)]
        [TestCaseSource(nameof(GetEmptyMapping))]
        [Test]
        public void ShouldThrowArgumentException_WhenMapperMappings_AreNullOrEmpty(Dictionary<string, string> mappings)
        {
            var tableName = Guid.NewGuid().ToString();
            var mapper = new MapperBuilder<Person>()
                .SetTableName(tableName)
                .SetMappings(mappings)
                .Build();
            var people = new Faker<Person>()
                .RuleFor(p => p.FirstName, p => p.Person.FirstName)
                .RuleFor(p => p.LastName, p => p.Person.LastName)
                .RuleFor(p => p.DateOfBirth, p => p.Person.DateOfBirth)
                .GenerateLazy(2);

            Action act = () => _msSqlQueryGenerator.GenerateParametrizedBulkInserts(mapper, people);

            act.Should().ThrowExactly<ArgumentException>();
        }

        [Test]
        public void ShouldThrowArgumentException_WhenMapperMappings_HaveInvalidPropertyNames()
        {
            var tableName = Guid.NewGuid().ToString();
            var mappings = new Dictionary<string, string>()
            {
                { $"{nameof(Person.FirstName)}_Invalid", nameof(Person.FirstName) },
                { nameof(Person.LastName), nameof(Person.LastName) },
                { nameof(Person.DateOfBirth), nameof(Person.DateOfBirth) },
            };
            var mapper = new MapperBuilder<Person>()
                .SetTableName(tableName)
                .SetMappings(mappings)
                .Build();
            var people = new Faker<Person>()
                .RuleFor(p => p.FirstName, p => p.Person.FirstName)
                .RuleFor(p => p.LastName, p => p.Person.LastName)
                .RuleFor(p => p.DateOfBirth, p => p.Person.DateOfBirth)
                .GenerateLazy(2);

            Action act = () => _msSqlQueryGenerator.GenerateParametrizedBulkInserts(mapper, people);

            act.Should().ThrowExactly<ArgumentException>();
        }

        [TestCase(null)]
        [TestCaseSource(nameof(GetEmptyCollectionOfPerson))]
        [Test]
        public void ShouldThrowArgumentException_WhenParameterCollectionOfObjects_IsNullOrEmpty(IEnumerable<Person> people)
        {
            var tableName = Guid.NewGuid().ToString();
            var mapper = new MapperBuilder<Person>()
                .SetTableName(tableName)
                .AddMapping(nameof(Person.FirstName), nameof(Person.FirstName))
                .AddMapping(nameof(Person.LastName), nameof(Person.LastName))
                .AddMapping(nameof(Person.DateOfBirth), nameof(Person.DateOfBirth))
                .Build();

            Action act = () => _msSqlQueryGenerator.GenerateParametrizedBulkInserts(mapper, people);

            act.Should().ThrowExactly<ArgumentException>();
        }

        [Test]
        public void ShouldThrowArgumentException_WhenCombinationOfCollectionOfObjectsAndMappings_IsMoreThanMaxAllowedSqlParametersCount()
        {
            var tableName = Guid.NewGuid().ToString();
            var mapper = new MapperBuilder<Person>()
                .SetTableName(tableName)
                .AddMapping(nameof(Person.FirstName), nameof(Person.FirstName))
                .AddMapping(nameof(Person.LastName), nameof(Person.LastName))
                .AddMapping(nameof(Person.DateOfBirth), nameof(Person.DateOfBirth))
                .Build();
            var people = new Faker<Person>()
                .RuleFor(p => p.FirstName, p => p.Person.FirstName)
                .RuleFor(p => p.LastName, p => p.Person.LastName)
                .RuleFor(p => p.DateOfBirth, p => p.Person.DateOfBirth)
                .GenerateLazy(701);

            Action act = () => _msSqlQueryGenerator.GenerateParametrizedBulkInserts(mapper, people);

            act.Should().ThrowExactly<ArgumentException>();
        }

        [Test]
        public void ValidateIfGeneratedSqlAndParameters_AreEquals_ExpectedSqlAndParameters()
        {
            var tableName = Guid.NewGuid().ToString();
            var mapper = new MapperBuilder<Person>()
                .SetTableName(tableName)
                .AddMapping(nameof(Person.FirstName), nameof(Person.FirstName))
                .AddMapping(nameof(Person.LastName), nameof(Person.LastName))
                .AddMapping(nameof(Person.DateOfBirth), nameof(Person.DateOfBirth))
                .Build();
            var people = new Faker<Person>()
                .RuleFor(p => p.FirstName, p => p.Person.FirstName)
                .RuleFor(p => p.LastName, p => p.Person.LastName)
                .RuleFor(p => p.DateOfBirth, p => p.Person.DateOfBirth)
                .Generate(2);
            var sqlParameters = CreateSqlParameters(mapper, people);
            var expectedResult = new List<(string SqlQuery, IEnumerable<IDbDataParameter> SqlParameters)>()
            {
                (CreateSqlInsert(mapper, sqlParameters), sqlParameters)
            };
            var result = _msSqlQueryGenerator.GenerateParametrizedBulkInserts(mapper, people);

            result.Should().BeEquivalentTo(expectedResult.AsEnumerable());
        }

        private static Dictionary<string, string> GetEmptyMapping() => new Dictionary<string, string>();

        private static IEnumerable<Person> GetEmptyCollectionOfPerson() => new Person[] { };

        private static List<IDbDataParameter> CreateSqlParameters<T>(Mapper<T> mapper, IEnumerable<Person> collectionOfObjects)
            where T : class
        {
            var sqlParameters = new List<IDbDataParameter>();
            var parameterIndex = 0;

            foreach (var entry in collectionOfObjects)
            {
                foreach (var mapping in mapper.Mappings)
                {
                    sqlParameters.Add(new SqlParameter($"@p{parameterIndex}", GetPropertyValue(entry, mapping.Key)));
                    parameterIndex++;
                }
            }

            return sqlParameters;
        }

        private static object GetPropertyValue(object @object, string propertyName) =>
            @object.GetType().GetProperty(propertyName).GetValue(@object, null);

        private string CreateSqlInsert<T>(Mapper<T> mapper, List<IDbDataParameter> sqlParameters) where T : class =>
            "SET NOCOUNT ON; " +
            $"INSERT INTO [{mapper.TableName}] ([{mapper.Mappings[nameof(Person.FirstName)]}], [{mapper.Mappings[nameof(Person.LastName)]}], [{mapper.Mappings[nameof(Person.DateOfBirth)]}]) " +
            $"VALUES ({sqlParameters[0].ParameterName}, {sqlParameters[1].ParameterName}, {sqlParameters[2].ParameterName}), " +
            $"({sqlParameters[3].ParameterName}, {sqlParameters[4].ParameterName}, {sqlParameters[5].ParameterName});";
    }
}