using System.Collections.Generic;
using System.Linq;

namespace MsSqlHelpers
{
    public class Mapper<T> 
        where T : class
    {
        public string TableName { get; set; }
        public Dictionary<string, string> Mappings { get; set; }

        public Mapper()
        {
            Mappings = new Dictionary<string, string>();
        }

        public Mapper(string tableName)
        {
            TableName = tableName;
            Mappings = new Dictionary<string, string>();
        }

        public (bool IsValid, List<string> ValidationErrors) Validate()
        {
            var isValid = true;
            var validationErrors = new List<string>();

            if (string.IsNullOrWhiteSpace(TableName))
            {
                isValid = false;
                validationErrors.Add($"[{nameof(Mapper<T>)}.{nameof(TableName)}] can not be null, empty or white-space.");
            }

            if (Mappings is null || Mappings.Count == 0)
            {
                isValid = false;
                validationErrors.Add($"[{nameof(Mapper<T>)}.{nameof(Mappings)}] can not be null or empty.");
            }

            var propertyNamesNotFound = Mappings?
                .Select(mapping => mapping.Key)
                .Except(typeof(T).GetProperties()
                .Select(propertyInfo => propertyInfo.Name));

            if (!(propertyNamesNotFound is null) && propertyNamesNotFound.Any())
            {
                isValid = false;
                validationErrors.Add($"Properties [{string.Join(", ", propertyNamesNotFound)}] in [{nameof(Mapper<T>)}.{nameof(Mappings)}] not found in the source object [{typeof(T)}].");
            }

            return (IsValid: isValid, ValidationErrors: validationErrors);
        }
    }
}
