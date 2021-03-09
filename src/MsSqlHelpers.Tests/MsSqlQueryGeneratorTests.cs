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
                .AddMapping(person => person.FirstName, columnName: nameof(Person.FirstName))
                .AddMapping(person => person.LastName, columnName: nameof(Person.LastName))
                .AddMapping(person => person.DateOfBirth, columnName: "Birthday")
                .Build();
            var people = new Faker<Person>()
                .RuleFor(person => person.FirstName, fakePerson => fakePerson.Person.FirstName)
                .RuleFor(person => person.LastName, fakePerson => fakePerson.Person.LastName)
                .RuleFor(person => person.DateOfBirth, fakePerson => fakePerson.Person.DateOfBirth)
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
                .RuleFor(person => person.FirstName, fakePerson => fakePerson.Person.FirstName)
                .RuleFor(person => person.LastName, fakePerson => fakePerson.Person.LastName)
                .RuleFor(person => person.DateOfBirth, fakePerson => fakePerson.Person.DateOfBirth)
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
                { nameof(Person.DateOfBirth), "Birthday" },
            };
            var mapper = new MapperBuilder<Person>()
                .SetTableName(tableName)
                .SetMappings(mappings)
                .Build();
            var people = new Faker<Person>()
                .RuleFor(person => person.FirstName, fakePerson => fakePerson.Person.FirstName)
                .RuleFor(person => person.LastName, fakePerson => fakePerson.Person.LastName)
                .RuleFor(person => person.DateOfBirth, fakePerson => fakePerson.Person.DateOfBirth)
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
                .AddMapping(person => person.FirstName, columnName: nameof(Person.FirstName))
                .AddMapping(person => person.LastName, columnName: nameof(Person.LastName))
                .AddMapping(person => person.DateOfBirth, columnName: "Birthday")
                .Build();

            Action act = () => _msSqlQueryGenerator.GenerateParametrizedBulkInserts(mapper, people);

            act.Should().ThrowExactly<ArgumentException>();
        }

        [Test]
        public void ShouldValidateIfGeneratedSqlAndParameters_AreEquals_ExpectedSqlAndParameters()
        {
            var tableName = Guid.NewGuid().ToString();
            var mapper = new MapperBuilder<Person>()
                .SetTableName(tableName)
                .AddMapping(person => person.FirstName, columnName: nameof(Person.FirstName))
                .AddMapping(person => person.LastName, columnName: nameof(Person.LastName))
                .AddMapping(person => person.DateOfBirth, columnName: "Birthday")
                .Build();
            var people = new Faker<Person>()
                .RuleFor(person => person.FirstName, fakePerson => fakePerson.Person.FirstName)
                .RuleFor(person => person.LastName, fakePerson => fakePerson.Person.LastName)
                .RuleFor(person => person.DateOfBirth, fakePerson => fakePerson.Person.DateOfBirth)
                .Generate(2);
            var sqlParameters = CreateSqlParameters(mapper, people);
            var expectedResult = new List<(string SqlQuery, IEnumerable<IDbDataParameter> SqlParameters)>()
            {
                (CreateSqlInsert(mapper, sqlParameters), sqlParameters)
            };
            var result = _msSqlQueryGenerator.GenerateParametrizedBulkInserts(mapper, people);

            result.Should().BeEquivalentTo(expectedResult.AsEnumerable());
        }

        [Test]
        public void ShouldSplitGeneratedSqlAndParameters_WhenCollectionOfObjects_IsMoreThanMaxAllowedBatchSize()
        {
            var tableName = Guid.NewGuid().ToString();
            var mapper = new MapperBuilder<Person>()
                .SetTableName(tableName)
                .AddMapping(person => person.FirstName, columnName: nameof(Person.FirstName))
                .Build();
            var people = new Faker<Person>()
                .RuleFor(person => person.FirstName, fakePerson => fakePerson.Person.FirstName)
                .GenerateLazy(MsSqlQueryGenerator.MaxAllowedBatchSize + 1);

            var result = _msSqlQueryGenerator.GenerateParametrizedBulkInserts(mapper, people);

            result.Count().Should().Be(2);
        }

        [Test]
        public void ShouldSplitGeneratedSqlAndParameters_WhenParametersCount_IsMoreThanMaxAllowedSqlParametersCount()
        {
            var tableName = Guid.NewGuid().ToString();
            var mapper = new MapperBuilder<Person>()
                .SetTableName(tableName)
                .AddMapping(person => person.FirstName, columnName: nameof(Person.FirstName))
                .AddMapping(person => person.LastName, columnName: nameof(Person.LastName))
                .AddMapping(person => person.DateOfBirth, columnName: "Birthday")
                .Build();
            // 3 properties/parameters * 700 entities = 2,100 properties/parameters, wich is greater than MaxAllowedSqlParametersCount (2100-1)
            var people = new Faker<Person>()
                .RuleFor(person => person.FirstName, fakePerson => fakePerson.Person.FirstName)
                .RuleFor(person => person.LastName, fakePerson => fakePerson.Person.LastName)
                .RuleFor(person => person.DateOfBirth, fakePerson => fakePerson.Person.DateOfBirth)
                .GenerateLazy(700);

            var result = _msSqlQueryGenerator.GenerateParametrizedBulkInserts(mapper, people);

            result.Count().Should().Be(2);
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