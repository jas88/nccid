using System.IO.Abstractions.TestingHelpers;
using CsvHelper;
using NUnit.Framework;

namespace nccid.Test;

public static class CsvHandlingTests
{
    private static List<string> Upload(string csv)
    {
        var keys = new List<string>();
        var mfs = new MockFileSystem(new Dictionary<string, MockFileData> { { "test.csv", new MockFileData(csv) } });
        new Nccidmain(mfs).Upload(new UploadOptions
        {
            Filename = "test.csv",
            Bucket = "testbucket",
            AwsId = "test",
            AwsKey = "test"
        }, (_, _, key) => keys.Add(key)).GetAwaiter().GetResult();
        return keys;
    }

    [TestCase("01/02/2021", 2021, 2, 1)]
    [TestCase("13/02/2021", 2021, 2, 13)]
    public static void DatesAreEnGb(string text, int year, int month, int day)
    {
        using var csv = CsvReader.Create(new StringReader($"Date\n{text}\n"), Utils.CsvOptions);
        Assert.That(csv.Read());
        Assert.That(csv.Parse<DateTime>("Date"), Is.EqualTo(new DateTime(year, month, day)));
    }

    [TestCase("01/02/2021", 2021, 2, 1)]
    [TestCase("13/02/2021", 2021, 2, 13)]
    public static void ParseDateIsEnGb(string text, int year, int month, int day)
    {
        Assert.That(Utils.ParseDate(text), Is.EqualTo(new DateTime(year, month, day)));
    }

    [Test]
    public static void SemicolonsAreNotSeparators()
    {
        using var csv = CsvReader.Create(new StringReader("ID;Status\nx;1\n"), Utils.CsvOptions);
        Assert.That(csv.Read());
        Assert.Throws<KeyNotFoundException>(() => csv.GetString("ID"));
    }

    [Test]
    public static void UploadSendsFirstDataRow()
    {
        var keys = Upload("ID,Status,Date\nPAT001,1,2001-01-01\nPAT002,0,2001-01-02\n");
        Assert.Multiple(() =>
        {
            Assert.That(keys, Has.Count.EqualTo(2));
            Assert.That(keys[0], Does.Contain("PAT001"));
            Assert.That(keys[1], Does.Contain("PAT002"));
        });
    }

    [Test]
    public static void UploadReportsFileRowNumbers()
    {
        var stdout = Console.Out;
        using var captured = new StringWriter();
        Console.SetOut(captured);
        try
        {
            Upload("ID,Status,Date\nPAT001,1,2001-01-01\n");
        }
        finally
        {
            Console.SetOut(stdout);
        }

        Assert.That(captured.ToString(), Does.Contain("Uploaded CSV row 2"));
    }

    [Test]
    public static void UploadSkipsRowWithMissingId()
    {
        var keys = Upload("Status,Date,ID\n1,2001-01-01\n0,2001-01-02,PAT002\n");
        Assert.Multiple(() =>
        {
            Assert.That(keys, Has.Count.EqualTo(1));
            Assert.That(keys[0], Does.Contain("PAT002"));
        });
    }

    [Test]
    public static void UploadRejectsUnclosedQuote()
    {
        Assert.Throws<CsvFormatException>(() => Upload("ID,Status,Date\n\"PAT001,1,2001-01-01\nPAT002,0,2001-01-02\n"));
    }

    [Test]
    public static void MergerRejectsShortRow()
    {
        var root = Path.GetPathRoot(Path.GetTempPath()) ?? throw new InvalidOperationException();
        var mfs = new MockFileSystem(new Dictionary<string, MockFileData>
        {
            { Path.Join(root, "testcsv", "short.csv"), new MockFileData("testid,cola\ntest1\n") }
        }, root);
        Assert.Throws<IndexOutOfRangeException>(() => _ = new CsvMerger("testid", "testcsv", mfs));
    }
}
