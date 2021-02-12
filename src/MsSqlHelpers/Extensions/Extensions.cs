using MsSqlHelpers.Enums;
using System;
using System.Globalization;

namespace MsSqlHelpers.Extensions
{
    public static class MsSqlExtensions
    {
        private const string SqlNull = "null";

        public static string ToSqlValue(this object @object, MsSqlUserLanguage msSqlUserLanguage = MsSqlUserLanguage.EnglishUnitedStates) =>
            HandleSqlObject(@object, msSqlUserLanguage);

        private static string HandleSqlObject(object @object, MsSqlUserLanguage msSqlUserLanguage)
        {
            if (@object is null)
            {
                return SqlNull;
            }

            if (@object is string)
            {
                return HandleSqlString((string)@object);
            }

            if (@object is decimal)
            {
                return HandleSqlDecimal((decimal)@object, msSqlUserLanguage);
            }

            if (@object is decimal?)
            {
                return HandleSqlNullableDecimal((decimal?)@object, msSqlUserLanguage);
            }

            if (@object is DateTime)
            {
                return HandleSqlDateTime((DateTime)@object, msSqlUserLanguage);
            }

            if (@object is DateTime?)
            {
                return HandleSqlNullableDateTime((DateTime?)@object, msSqlUserLanguage);
            }

            return @object.ToString();
        }

        private static string HandleSqlString(string @string) => $"'{@string}'";

        private static string HandleSqlDecimal(decimal @decimal, MsSqlUserLanguage msSqlUserLanguage) =>
            @decimal.ToString(msSqlUserLanguage.GetCultureInfo());

        private static CultureInfo GetCultureInfo(this MsSqlUserLanguage msSqlUserLanguage)
        {
            switch (msSqlUserLanguage)
            {
                case MsSqlUserLanguage.PortugueseBrazilian:
                    return new CultureInfo("pt-BR");
                case MsSqlUserLanguage.EnglishUnitedStates:
                default:
                    return new CultureInfo("en-US");
            }
        }

        private static string HandleSqlNullableDecimal(decimal? @nullableDecimal, MsSqlUserLanguage msSqlUserLanguage) =>
            @nullableDecimal?.ToString(msSqlUserLanguage.GetCultureInfo());

        private static string HandleSqlDateTime(DateTime dateTime, MsSqlUserLanguage msSqlUserLanguage) =>
            dateTime.ToString(msSqlUserLanguage.GetDateTimeFormat());

        private static string GetDateTimeFormat(this MsSqlUserLanguage msSqlUserLanguage)
        {
            switch (msSqlUserLanguage)
            {
                case MsSqlUserLanguage.PortugueseBrazilian:
                    return "dd-MM-yyyy HH:mm:ss.fff";
                case MsSqlUserLanguage.EnglishUnitedStates:
                default:
                    return "yyyy-MM-dd HH:mm:ss.fff";
            }
        }

        private static string HandleSqlNullableDateTime(DateTime? nullableDateTime, MsSqlUserLanguage msSqlUserLanguage) =>
            nullableDateTime?.ToString(msSqlUserLanguage.GetDateTimeFormat());
    }
}
