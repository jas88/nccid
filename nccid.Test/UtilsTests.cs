using System;
using NUnit.Framework;

namespace nccid.Test
{
    public static class UtilsTests
    {
        /// <summary>
        /// Arbitrary date to use in testing
        /// </summary>
        private static readonly DateTime bd = new(1980, 2, 24);

        [Test]
        public static void DicomDate()
        {
            Assert.AreEqual("19800224", Utils.DicomDate(bd));
        }

        [Test]
        public static void DicomWindow()
        {
            Assert.AreEqual("19800224-", Utils.DicomWindow(bd, 0, 0, null));
            Assert.AreEqual("19800224-19790216", Utils.DicomWindow(bd, 1, 8, 4));
        }
    }
}