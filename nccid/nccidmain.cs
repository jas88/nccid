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

        private static string DicomDate(DateTime t)
        {
            return t.ToString("yyyyMMdd");
        }
        
        private static string datewin(DateTime mid)
        {
            return $"{DicomDate(mid.AddDays(-210))}-{DicomDate(mid.AddDays(210))}";
        }

        public record FetchItem
        {
            public DateTime Start { get; }
            public string Chi { get; }
            public bool Covid { get; }

            public FetchItem(string ptChi, bool b, DateTime ptWhen)
            {
                Chi = ptChi;
                Covid = b;
                Start = ptWhen.AddMonths(1-ptWhen.Month).AddDays(1-ptWhen.Day).AddYears(-3);
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
                var pt = new Swab(csv);
                var req=new DicomCFindRequest(DicomQueryRetrieveLevel.Study);
                req.Dataset.AddOrUpdate(new DicomTag(0x8, 0x5), "ISO_IR 192");
                req.Dataset.AddOrUpdate(DicomTag.StudyDate, datewin(pt.When));
                req.Dataset.AddOrUpdate(DicomTag.PatientID, pt.Chi);
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
                    csvout.WriteRecord(new FetchItem(pt.Chi,pt.Result==1,pt.When));
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
        }

        public async Task Upload(UploadOptions o)
        {
            var ct = new CancellationToken();
            var creds = new BasicAWSCredentials(o.AwsId, o.AwsKey);
            var s3 = new AmazonS3Client(creds, RegionEndpoint.EUWest2);
            using var tu = new TransferUtility(s3);
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
                    await tu.UploadAsync(ms, o.bucket, datum.S3Path(o.prefix), ct);
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
            var dotstrip = new Regex(@"(^|[\\/])\.[\\/]");
            foreach (var dcm in fileSystem.Directory.EnumerateFiles(".", "*.dcm", enumopts))
            {
                // Ensure we don't send S3 any grubby DOS-style delimiters: https://github.com/jas88/nccid/issues/52
                var _dcm = dotstrip.Replace(dcm, "/").Replace(@"\","/").Replace("//","/");
                try
                {
                    var attr = File.GetAttributes(dcm);
                    // Ignore file unless Archive bit set; if uploaded successfully, clear that bit.
                    if ((attr & FileAttributes.Archive) == FileAttributes.Archive)
                    {
                        await tu.UploadAsync(dcm, o.bucket, $"{o.prefix}{DateTime.Now.ToString("yyyy-MM-dd")}/images/{_dcm}", ct);
                        attr &= ~FileAttributes.Archive;
                        File.SetAttributes(dcm, attr);
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine($"Exception '{e}' uploading '{_dcm}' to '{o.prefix}{DateTime.Now.ToString("yyyy-MM-dd")}/images/{_dcm}'");
                }
            }
        }

        static async Task Main(string[] args)
        {
            var prog = new Nccidmain(new FileSystem());
            switch (args.Length>0?args[0]:"help")
            {
                case "help":
                    Console.WriteLine(@"The first argument must be a verb:

help    Display this help text
version Display the version number
search  Search a PACS for patient data using QR C-FIND
fetch   Retrieve PACS data from a search operation
upload  Send retrieved patient data to the NCCID repository");
                    break;
                case "version":
                    Parser.Default.ParseArguments<Options>(new string[]{"--version"});
                    break;
                case "search":
                    await Parser.Default.ParseArguments<SearchOptions>(args).WithParsedAsync(async o=>await prog.Search(o));
                    break;
                case "fetch":
                    await Parser.Default.ParseArguments<FetchOptions>(args).WithParsedAsync(async o=>await prog.Fetch(o));
                    break;
                case "upload":
                    await Parser.Default.ParseArguments<UploadOptions>(args).WithParsedAsync(async o=>await prog.Upload(o));
                    break;
            }
        }
    }
}
