using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions.TestingHelpers;
using NUnit.Framework;

namespace nccid.Test;

public class CsvMergerTests
{
    private static readonly string Root = Path.GetPathRoot(Path.GetTempPath());
    private static readonly MockFileSystem Mfs = new(new Dictionary<string,MockFileData>{
        {Path.Combine(Root,"testcsv","test1.csv"),new MockFileData("testid,cola\ntest1,datum1")},
        {Path.Combine(Root,"testcsv","test2.csv"),new MockFileData("testid,colb\ntest1,datum2")},
        {Path.Combine(Root,"testcsv","excluded.csv"),new MockFileData("testid,colc\ntest1,datum3")},
        {Path.Combine(Root,"other","wrongplace.csv"),new MockFileData("testid,cola\ntest2,datum1")}
    },Root);
    [Test]
    public static void CsvMerger()
    {
        CsvMerger testSet=new("testid","testcsv",Mfs,new List<string>{ Path.Combine(Root,"testcsv","excluded.csv") });
        Assert.Multiple(() =>
        {
            Assert.That(testSet.TryGetValue("test1", out var datum), "Found data for test1");
            Assert.That(testSet.ContainsKey("test2"), Is.False, "Must not read data from other directories");
            Assert.That(datum.TryGetValue("cola", out var cola), "Has colA");
            Assert.That(cola, Is.EqualTo("datum1"), "First column entry found");
            Assert.That(datum.ContainsKey("colc"), Is.False, "Data from excluded file");
        });
    }

}