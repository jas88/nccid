using System;
using System.Text.RegularExpressions;

namespace nccid
{
    public static class Utils
    {
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

            // Repeated slashes:
            while (path.Contains("//"))
                path = path.Replace("//", "/");

            // Remove leading / if any
            if (path.StartsWith("/"))
                path = path.Substring(1);

            return path;
        }

        /// <summary>
        /// Convert a DateTime to DICOM format (20210131)
        /// </summary>
        /// <param name="t">Date to convert</param>
        /// <returns>Date in DICOM format</returns>
        public static string DicomDate(DateTime t) => t.ToString("yyyyMMdd");

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

    }

}