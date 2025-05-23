using System;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace LibreLancer.Database;

public class DateTimeToJulianConverter(ConverterMappingHints mappingHints) :
    ValueConverter<DateTime, double>(
    v => ToJulianDays(v),
    v => FromJulianDays(v),
    mappingHints)
{
    public DateTimeToJulianConverter() : this(null) { }

    public static double ToJulianDays(DateTime dateTime)
        => ToJulianDays(
            dateTime.Year, dateTime.Month, dateTime.Day, dateTime.Hour, dateTime.Minute, dateTime.Second, dateTime.Millisecond);

    private static double ToJulianDays(int year, int month, int day, int hour, int minute, int second, int millisecond)
    {
        // computeJD
        var Y = year;
        var M = month;
        var D = day;

        if (M <= 2)
        {
            Y--;
            M += 12;
        }

        var A = Y / 100;
        var B = 2 - A + (A / 4);
        var X1 = 36525 * (Y + 4716) / 100;
        var X2 = 306001 * (M + 1) / 10000;
        var iJD = (long)((X1 + X2 + D + B - 1524.5) * 86400000);

        iJD += hour * 3600000 + minute * 60000 + (long)((second + millisecond / 1000.0) * 1000);

        return iJD / 86400000.0;
    }

    public static DateTime FromJulianDays(double julianDate)
    {
        // computeYMD
        var iJD = (long)(julianDate * 86400000.0 + 0.5);
        var Z = (int)((iJD + 43200000) / 86400000);
        var A = (int)((Z - 1867216.25) / 36524.25);
        A = Z + 1 + A - (A / 4);
        var B = A + 1524;
        var C = (int)((B - 122.1) / 365.25);
        var D = (36525 * (C & 32767)) / 100;
        var E = (int)((B - D) / 30.6001);
        var X1 = (int)(30.6001 * E);
        var day = B - D - X1;
        var month = E < 14 ? E - 1 : E - 13;
        var year = month > 2 ? C - 4716 : C - 4715;

        // computeHMS
        var s = (int)((iJD + 43200000) % 86400000);
        var fracSecond = s / 1000.0;
        s = (int)fracSecond;
        fracSecond -= s;
        var hour = s / 3600;
        s -= hour * 3600;
        var minute = s / 60;
        fracSecond += s - minute * 60;

        var second = (int)fracSecond;
        var millisecond = (int)Math.Round((fracSecond - second) * 1000.0);

        return new DateTime(year, month, day, hour, minute, second, millisecond);
    }
}
