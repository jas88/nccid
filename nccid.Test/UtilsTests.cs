using System;
using System.Text;
using NUnit.Framework;

namespace nccid.Test
{
    public static class UtilsTests
    {
        /// <summary>
        /// Arbitrary date to use in testing
        /// </summary>
        internal static readonly DateTime bd = new(1980, 2, 24);

        /// <summary>
        /// UTF8 encoding used repeatedly
        /// </summary>
        internal static Encoding utf8 = Encoding.UTF8;

        [Test]
        public static void DicomDate()
        {
            Assert.AreEqual("19800224", Utils.DicomDate(bd));
        }

        [Test]
        public static void DicomWindow()
        {
            Assert.AreEqual("19800224-", Utils.DicomWindow(bd, 0, 0, null));
            Assert.AreEqual("19790216-19800228", Utils.DicomWindow(bd, 1, 8, 4));
        }
    }
}