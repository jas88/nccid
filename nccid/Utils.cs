using System.Globalization;
using System.Text.RegularExpressions;

namespace nccid;

public static partial class Utils
{
    private static readonly Regex YearMonthDay = YearMonthDayRegex();

    public static DateTime ParseDate(string ds)
    {
        var digits = YearMonthDay.Match(ds);
        if (!digits.Success) return DateTime.Parse(ds, CultureInfo.InvariantCulture);

        var y = int.Parse(digits.Groups[1].Value, CultureInfo.InvariantCulture);
        var m = int.Parse(digits.Groups[3].Value, CultureInfo.InvariantCulture);
        var d = int.Parse(digits.Groups[4].Value, CultureInfo.InvariantCulture);
        return new DateTime(y, m, d);
    }

    /// <summary>
    /// Ensure we don't send S3 any grubby DOS-style delimiters: https://github.com/jas88/nccid/issues/52
    /// </summary>
    /// <param name="path"></param>
    /// <returns>Cleaned up path</returns>
    public static string SanitizePath(string path)
    {
        // First, no more DOS-style slashes:
        path = path.Replace(@"\", "/");

        // No reduntant dots (/./)
        path = path.Replace("/./", "/");

        // Remove leading / if any
        while (path.StartsWith('/') || path.StartsWith('.'))
            path = path[1..];

        // Repeated slashes:
        while (path.Contains("//"))
            path = path.Replace("//", "/");

        return path;
    }

    /// <summary>
    /// Convert a DateTime to DICOM format (20210131)
    /// </summary>
    /// <param name="t">Date to convert</param>
    /// <returns>Date in DICOM format</returns>
    public static string DicomDate(DateTime t) => t.ToString("yyyyMMdd", CultureInfo.InvariantCulture);

    /// <summary>
    /// Generate an asymmetric DICOM date range either side of a date
    /// </summary>
    /// <param name="t">Date to use as basis</param>
    /// <param name="preyears">Years before date</param>
    /// <param name="pre">Days before date</param>
    /// <param name="post">Days after date, or null to cover all dates after t</param>
    /// <returns></returns>
    public static string DicomWindow(DateTime t, int preyears, int pre, int? post)
        => $"{DicomDate(t.AddYears(-preyears).AddDays(-pre))}-{(post.HasValue ? DicomDate(t.AddDays(post.Value)) : "")}";

    [GeneratedRegex(@"^((19|20)\d{2})(\d\d)(\d\d)$", RegexOptions.CultureInvariant)]
    private static partial Regex YearMonthDayRegex();
}
