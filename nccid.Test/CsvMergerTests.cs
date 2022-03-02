using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions.TestingHelpers;
using NUnit.Framework;

namespace nccid.Test
{
    public class CsvMergerTests
    {
        private static readonly string root = Path.GetPathRoot(Path.GetTempPath());
        private static readonly MockFileSystem mfs = new(new Dictionary<string,MockFileData>{
            {Path.Combine(root,"testcsv","test1.csv"),new MockFileData("testid,cola\ntest1,datum1")},
            {Path.Combine(root,"testcsv","test2.csv"),new MockFileData("testid,colb\ntest1,datum2")},
            {Path.Combine(root,"testcsv","excluded.csv"),new MockFileData("testid,colc\ntest1,datum3")},
            {Path.Combine(root,"other","wrongplace.csv"),new MockFileData("testid,cola\ntest2,datum1")},
            },root);
        [Test]
        public static void CsvMerger()
        {
            CsvMerger testSet=new("testid","testcsv",mfs,new List<string>{ Path.Combine(root,"testcsv","excluded.csv") });
            Assert.IsTrue(testSet.TryGetValue("test1",out var datum), "Found data for test1");
            Assert.IsFalse(testSet.ContainsKey("test2"), "Must not read data from other directories");
            Assert.IsTrue(datum.TryGetValue("cola",out var cola),"Has colA");
            Assert.AreEqual("datum1",cola,"First column entry found");
            Assert.IsFalse(datum.ContainsKey("colc"),"Data from excluded file");
        }

    }
}