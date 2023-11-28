using System;
using NUnit.Framework;

namespace nccid.Test;

public static class UtilsTests
{
    /// <summary>
    /// Arbitrary date to use in testing
    /// </summary>
    internal static readonly DateTime Bd = new(1980, 2, 24);

    [Test]
    public static void DicomDate()
    {
        Assert.That(Utils.DicomDate(Bd), Is.EqualTo("19800224"));
    }

    [Test]
    public static void DicomWindow()
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
    public static void SanitizePath(string from,string to)
    {
        Assert.That(Utils.SanitizePath(from), Is.EqualTo(to));
    }
}