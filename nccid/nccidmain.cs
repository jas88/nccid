using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Amazon;
using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Transfer;
using CommandLine;
using CsvHelper;
using Dicom;
using Dicom.Network;
using DicomClient=Dicom.Network.Client.DicomClient;

namespace nccid
{
    public class Nccidmain
    {
        readonly IFileSystem fileSystem;

        public Nccidmain(IFileSystem fileSystem)
        {
            this.fileSystem = fileSystem;
        }

        public record FetchItem
        {
            public string Window { get; }
            public string Chi { get; }
            public bool Covid { get; }

            public FetchItem(string ptChi, bool b, string window)
            {
                Chi = ptChi;
                Covid = b;
                this.Window = window;
            }
        }
        
        public async Task Search(SearchOptions o)
        {
            var pacs=new DicomClient(o.Theirhost, o.Theirport, false, o.Ourname, o.Theirname);
            pacs.NegotiateAsyncOps();
            using var reader = new StreamReader(fileSystem.FileStream.Create(o.Filename,FileMode.Open));
            using var csv = new CsvReader(reader, CultureInfo.GetCultureInfo("en-GB"));
            using var writer = new StreamWriter(fileSystem.FileStream.Create(o.Output, FileMode.Create));
            using var csvout = new CsvWriter(writer, CultureInfo.InvariantCulture);
            csvout.WriteHeader<FetchItem>();
            csvout.NextRecord();
            csv.Read();
            csv.ReadHeader();
            while (csv.Read())
            {
                var studies = new List<string>();
                var pt = INCCIDdata.Make("Dummy centre name", csv.GetField<string>("Status"), csv.GetField<DateTime>("Date"),
                        csv.GetField("ID"));
                var req=new DicomCFindRequest(DicomQueryRetrieveLevel.Study);
                req.Dataset.AddOrUpdate(new DicomTag(0x8, 0x5), "ISO_IR 192");
                req.Dataset.AddOrUpdate(DicomTag.StudyDate, Utils.DicomWindow(pt.when,0,21,21));
                req.Dataset.AddOrUpdate(DicomTag.PatientID, pt.Pseudonym);
                req.Dataset.AddOrUpdate(DicomTag.StudyInstanceUID, "");
                req.OnResponseReceived += (req, resp) =>
                {
                    var uid = resp.Dataset?.GetSingleValue<string>(DicomTag.StudyInstanceUID);
                    if (uid != null)
                        studies.Add(uid);
                };
                await pacs.AddRequestAsync(req);
                await pacs.SendAsync();
                //swabs.Add(pt);
                if (studies.Count > 0)
                {
                    csvout.WriteRecord(new FetchItem(pt.Pseudonym,pt is PositiveData,pt.When));
                    csvout.NextRecord();
                }
                Console.WriteLine(JsonSerializer.Serialize(pt));
            }
            await csvout.FlushAsync();
        }

        public async Task Fetch(FetchOptions o)
        {
            var pacs=new DicomClient(o.Theirhost, o.Theirport, false, o.Ourname, o.Theirname);
            pacs.NegotiateAsyncOps();
            //using var reader = new StreamReader(fileSystem.FileStream.Create(o.Filename,FileMode.Open));
            //using var csv = new CsvReader(reader, CultureInfo.GetCultureInfo("en-GB"));
            // TODO: Complete PACS fetch code.
            await Task.Delay(0);
        }

        public delegate void ObjectSender(Stream data, string bucket, string key);

        public async Task Upload(UploadOptions o,ObjectSender os)
        {
            var ct = new CancellationToken();
            using var reader = new StreamReader(fileSystem.FileStream.Create(o.Filename,FileMode.Open));
            using var csv = new CsvReader(reader, CultureInfo.GetCultureInfo("en-GB"));
            csv.Read();
            csv.ReadHeader();
            while (csv.Read())
            {
                try
                {
                    var datum = INCCIDdata.Make(o.CentreName, csv.GetField<string>("Status"), csv.GetField<DateTime>("Date"),
                        csv.GetField("ID"));
                    var json = datum.ToJson();
                    await using var ms = new MemoryStream(json, false);
                    os(ms, o.bucket, datum.S3Path(o.prefix));
                    Console.WriteLine($"Uploaded CSV row {csv.Parser.Row}");
                }
                catch (Exception e)
                {
                    Console.WriteLine($"Error '{e}' uploading JSON for '{csv.Parser.RawRecord}'");
                }
            }
            var enumopts= new EnumerationOptions()
            {
                MatchCasing = MatchCasing.PlatformDefault,
				RecurseSubdirectories = true
            };
            foreach (var dcm in fileSystem.Directory.EnumerateFiles(".", "*.dcm", enumopts))
            {
                try
                {
                    var attr = File.GetAttributes(dcm);
                    // Ignore file unless Archive bit set; if uploaded successfully, clear that bit.
                    if ((attr & FileAttributes.Archive) == FileAttributes.Archive)
                    {
                        using (var dcmstream = File.Open(dcm, FileMode.Open)) {
                            os(dcmstream, o.bucket, $"{o.prefix}{DateTime.Now.ToString("yyyy-MM-dd")}/images/{Utils.SanitizePath(dcm)}");
                        }
                        attr &= ~FileAttributes.Archive;
                        File.SetAttributes(dcm, attr);
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine($"Exception '{e}' uploading '{dcm}' to '{o.prefix}{DateTime.Now.ToString("yyyy-MM-dd")}/images/{Utils.SanitizePath(dcm)}'");
                }
            }
        }

        static int Main(string[] args)
        {
            var prog = new Nccidmain(new FileSystem());
            Parser.Default.ParseArguments<SearchOptions, FetchOptions, UploadOptions>(args)
                .WithParsed<SearchOptions>(o => prog.Search(o).RunSynchronously())
                .WithParsed<FetchOptions>(o => prog.Fetch(o).RunSynchronously())
                .WithParsed<UploadOptions>(o =>
            {
                var creds = new BasicAWSCredentials(o.AwsId, o.AwsKey);
                var s3 = new AmazonS3Client(creds, RegionEndpoint.EUWest2);
                using var tu = new TransferUtility(s3);
                prog.Upload(o, tu.Upload).RunSynchronously();
            });
            return 0;
        }
    }
}
