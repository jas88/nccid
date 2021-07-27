using System;
using NUnit.Framework;

namespace nccid.Test
{
    public static class DataTests
    {
        [Test]
        public static void PositiveData()
        {
            Assert.IsTrue(INCCIDdata.Make("", "1", UtilsTests.bd, "") is PositiveData);
            var datum = INCCIDdata.Make("Centre", "positive", UtilsTests.bd, "pseudo1");
            Assert.IsTrue(datum is PositiveData);
            Assert.AreEqual(UtilsTests.bd, datum.when);
            Assert.AreEqual(@"{""Date of Positive Covid Swab"":""02/24/1980"",""When"":""19761231-"",""Pseudonym"":""pseudo1"",""SubmittingCentre"":""Centre""}", UtilsTests.utf8.GetString(datum.ToJson()));
            Assert.AreEqual("prefix/2021-07-27/data/pseudo1_data.json", datum.S3Path("prefix/"));
        }

        [Test]
        public static void NegativeData()
        {
            Assert.IsTrue(INCCIDdata.Make("", "negative", UtilsTests.bd, "") is NegativeData);
            var datum = INCCIDdata.Make("Centre", "0", UtilsTests.bd, "pseudo1");
            Assert.IsTrue(datum is NegativeData);
            Assert.AreEqual(@"{""SwabStatus"":0,""SwabDate"":""24/02/1980"",""When"":""19800203-19800316"",""Pseudonym"":""pseudo1"",""SubmittingCentre"":""Centre""}", UtilsTests.utf8.GetString(datum.ToJson()));
            Assert.AreEqual("prefix/2021-07-27/data/pseudo1_status.json", datum.S3Path("prefix/"));
        }

        [Test]
        public static void AmbiguousThrows()
        {
            Assert.Throws<ArgumentException>(() => { _ = INCCIDdata.Make("", "blah", System.DateTime.Now, ""); } );
        }
    }
}