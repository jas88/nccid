using System;

namespace nccid
{
    public static class Utils
    {
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