using System.Text;
using NUnit.Framework;

namespace nccid.Test;

public static class DataTests
{
    [Test]
    public static void PositiveData()
    {
        Assert.That(NccidData.Make("", "1", UtilsTests.Bd, "") is PositiveData);
        var datum = NccidData.Make("Centre", "positive", UtilsTests.Bd, "pseudo1");
        Assert.Multiple(() =>
        {
            Assert.That(datum is PositiveData);
            Assert.That(datum.DtWhen, Is.EqualTo(UtilsTests.Bd));
            Assert.That(Encoding.UTF8.GetString(datum.ToJson()), Is.EqualTo("""{"Date of Positive Covid Swab":"02/24/1980","SpecialFormatTimestamp":"19761231-","Pseudonym":"pseudo1","SubmittingCentre":"Centre"}"""));
            Assert.That(datum.S3Path("prefix/"), Is.EqualTo($"prefix/{DateTime.Now:yyyy-MM-dd}/data/pseudo1_data.json"));
        });
    }

    [Test]
    public static void NegativeData()
    {
        Assert.That(NccidData.Make("", "negative", UtilsTests.Bd, "") is NegativeData);
        var datum = NccidData.Make("Centre", "0", UtilsTests.Bd, "pseudo1");
        Assert.Multiple(() =>
        {
            Assert.That(datum is NegativeData);
            Assert.That(Encoding.UTF8.GetString(datum.ToJson()), Is.EqualTo("""{"SwabStatus":0,"SwabDate":"24/02/1980","SpecialFormatTimestamp":"19800203-19800316","Pseudonym":"pseudo1","SubmittingCentre":"Centre"}"""));
            Assert.That(datum.S3Path("prefix/"), Is.EqualTo($"prefix/{DateTime.Now:yyyy-MM-dd}/data/pseudo1_status.json"));
        });
    }

    [Test]
    public static void AmbiguousThrows()
    {
        Assert.Throws<ArgumentException>(() => { _ = NccidData.Make("", "blah", DateTime.Now, ""); });
    }
}