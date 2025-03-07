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
        public static void DicomDateTest()
        {
            Assert.That(Utils.DicomDate(Bd), Is.EqualTo("19800224"));
        }

        [Test]
        public static void DicomWindowTest()
        {
          Assert.Multiple(static () =>
          {
              Assert.That(Utils.DicomWindow(Bd, 0, 0, null), Is.EqualTo("19800224-"));
              Assert.That(Utils.DicomWindow(Bd, 1, 8, 4), Is.EqualTo("19790216-19800228"));
          });
        }

        [TestCase("", "")]
        [TestCase("/foo.dcm", "foo.dcm")]
        [TestCase(@"\dir\foo.dcm", "dir/foo.dcm")]
        [TestCase(@"/////.//\/\///\foo.dcm", "foo.dcm")]
        public static void SanitizePathTest(string from,string to)
        {
            Assert.That(Utils.SanitizePath(from), Is.EqualTo(to));
        }

        [TestCase("20010224",2001,2,24)]
        public static void ParseDateTest(string s,int y,int m,int d)
        {
            Assert.AreEqual(Utils.ParseDate(s), new DateTime(y,m,d));
        }
    }

    [TestCase("", "")]
    [TestCase("/foo.dcm", "foo.dcm")]
    [TestCase(@"\dir\foo.dcm", "dir/foo.dcm")]
    [TestCase(@"/////.//\/\///\foo.dcm", "foo.dcm")]
    public static void SanitizePath(string from,string to)
    {
      // TODO
    }
}