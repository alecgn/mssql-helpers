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
            if (@object is decimal?)
            {
                return HandleSqlNullableDecimal((decimal?)@object, msSqlUserLanguage);
            }

            if (@object is DateTime?)
            {
                return HandleSqlNullableDateTime((DateTime?)@object, msSqlUserLanguage);
            }

            return @object switch
            {
                null => SqlNull,
                string @string => HandleSqlString(@string),
                decimal @decimal => HandleSqlDecimal(@decimal, msSqlUserLanguage),
                DateTime @datetime => HandleSqlDateTime(@datetime, msSqlUserLanguage),
                _ => @object.ToString()
            };
        }

        private static string HandleSqlString(string @string) => $"'{@string}'";

        private static string HandleSqlDecimal(decimal @decimal, MsSqlUserLanguage msSqlUserLanguage) =>
            @decimal.ToString(msSqlUserLanguage.GetCultureInfo());

        private static CultureInfo GetCultureInfo(this MsSqlUserLanguage msSqlUserLanguage) =>
            msSqlUserLanguage switch
            {
                MsSqlUserLanguage.PortugueseBrazilian => new CultureInfo("pt-BR"),
                _ => new CultureInfo("en-US"),
            };

        private static string HandleSqlNullableDecimal(decimal? @nullableDecimal, MsSqlUserLanguage msSqlUserLanguage) =>
            @nullableDecimal?.ToString(msSqlUserLanguage.GetCultureInfo());

        private static string HandleSqlDateTime(DateTime dateTime, MsSqlUserLanguage msSqlUserLanguage) =>
            dateTime.ToString(msSqlUserLanguage.GetDateTimeFormat());

        private static string GetDateTimeFormat(this MsSqlUserLanguage msSqlUserLanguage) =>
            msSqlUserLanguage switch
            {
                MsSqlUserLanguage.PortugueseBrazilian => "dd-MM-yyyy HH:mm:ss.fff",
                _ => "yyyy-MM-dd HH:mm:ss.fff",
            };

        private static string HandleSqlNullableDateTime(DateTime? nullableDateTime, MsSqlUserLanguage msSqlUserLanguage) =>
            nullableDateTime?.ToString(msSqlUserLanguage.GetDateTimeFormat());
    }
}
