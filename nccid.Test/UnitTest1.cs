using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions.TestingHelpers;
using System.Threading.Tasks;
using NUnit.Framework;

namespace nccid.Test
{
    public class Tests
    {
        private readonly MockFileSystem mfs;
        private readonly nccid.Nccidmain prog;

        public Tests()
        {
            mfs = new MockFileSystem(new Dictionary<string, MockFileData>
            {
                {
                    @"test.csv", new MockFileData(@"ID,Status,Date
PAT003,1,1/1/2001
PAT004,0,20/10/1997
PAT023,1,26/11/1990
PAT999,0,1/3,2017
")
                }
            });
            prog = new Nccidmain(mfs);
            Nccidmain.ObjectSender p = async (body,bucket,key,ct) => { await Task.Delay(0); };
            prog.Upload(new UploadOptions {
                Filename="test.csv"
            }, p).Wait();
        }

        [SetUp]
        public void Setup()
        {
/*            var r = new Random(4291);
            using var gen = new DicomDataGenerator(r, null, "CT")
            {
                NoPixels = true
            };
            // Create 25 random patients each with CT scans
            foreach (var i in Enumerable.Range(1,10))
            {
                var p=new Person(r);
                var dataset = gen.GenerateTestDataset(p, r);
                var status = new CovidStatus(dataset,r.Next(1)==1,r.Next(10)==10);
            }*/
        }

        [Test]
        public async Task SearchTest()
        {
            var po = new SearchOptions
            {
                Theirhost = "www.dicomserver.co.uk",
                Theirport = 104,
                Theirname = "dicomserver",
                Ourport = 104,
                Filename="test.csv",
                Ourname="nccidqr"
            };
            await prog.Search(po);
            Assert.True(mfs.FileExists(po.Output));
        }

        [Test]
        public void Test1()
        {
            Assert.Pass();
        }
    }
}