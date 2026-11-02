using System.IO.Abstractions;
using System.Text.Json;
using Amazon;
using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Transfer;
using CommandLine;
using CsvHelper;
using FellowOakDicom;
using FellowOakDicom.Network;
using FellowOakDicom.Network.Client;
using JetBrains.Annotations;

namespace nccid;

public sealed partial class Nccidmain(IFileSystem fileSystem)
{
    [CsvRecord]
    internal sealed partial record FetchItem([UsedImplicitly] string Chi, bool Covid, string Window);

    public async Task Search(SearchOptions o)
    {
        var pacs = DicomClientFactory.Create(o.Theirhost, o.Theirport, false, o.Ourname, o.Theirname);
        pacs.NegotiateAsyncOps();
        using var reader = new StreamReader(fileSystem.FileStream.New(o.Filename, FileMode.Open));
        using var csv = CsvReader.Create(reader, Utils.CsvOptions);
        await using var writer = new StreamWriter(fileSystem.FileStream.New(o.Output, FileMode.Create));
        await using var csvout = CsvWriter.Create(writer);
        csvout.WriteHeader<FetchItem>();
        while (await csv.ReadAsync())
        {
            var studies = new List<string>();
            var pt = NccidData.Make("Dummy centre name", csv.GetString("Status"), csv.Parse<DateTime>("Date"),
                csv.GetString("ID"));
            var req = new DicomCFindRequest(DicomQueryRetrieveLevel.Study);
            req.Dataset.AddOrUpdate(new DicomTag(0x8, 0x5), "ISO_IR 192");
            req.Dataset.AddOrUpdate(DicomTag.StudyDate, Utils.DicomWindow(pt.DtWhen, 0, 21, 21));
            req.Dataset.AddOrUpdate(DicomTag.PatientID, pt.Pseudonym);
            req.Dataset.AddOrUpdate(DicomTag.StudyInstanceUID, "");
            req.OnResponseReceived += (_, resp) =>
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
                csvout.WriteRecord(new FetchItem(pt.Pseudonym, pt is PositiveData, pt.SpecialFormatTimestamp));
            }

            Console.WriteLine(JsonSerializer.Serialize(pt));
        }

        await csvout.FlushAsync();
    }

    private static Task Fetch(FetchOptions o)
    {
        var pacs = DicomClientFactory.Create(o.Theirhost, o.Theirport, false, o.Ourname, o.Theirname);
        pacs.NegotiateAsyncOps();
        //using var reader = new StreamReader(fileSystem.FileStream.New(o.Filename,FileMode.Open));
        //using var csv = new CsvReader(reader, CultureInfo.GetCultureInfo("en-GB"));
        // TODO: Complete PACS fetch code.
        return Task.Delay(0);
    }

    public delegate void ObjectSender(Stream data, string bucket, string key);

    public async Task Upload(UploadOptions o, ObjectSender os)
    {
        using var reader = new StreamReader(fileSystem.FileStream.New(o.Filename, FileMode.Open));
        using var csv = CsvReader.Create(reader, Utils.CsvOptions);
        while (await csv.ReadAsync())
        {
            try
            {
                var datum = NccidData.Make(o.CentreName, csv.GetString("Status"), Utils.ParseDate(csv.GetString("Date")),
                    csv.GetString("ID"));
                var json = datum.ToJson();
                await using var ms = new MemoryStream(json, false);
                os(ms, o.Bucket, datum.S3Path(o.Prefix));
                Console.WriteLine($"Uploaded CSV row {csv.RowNumber + 1}");
            }
            catch (Exception e)
            {
                var row = csv.Current.ToString();
                Console.WriteLine($"Error '{e}' uploading JSON for '{row}'");
            }
        }

        var enumopts = new EnumerationOptions
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
                if ((attr & FileAttributes.Archive) != FileAttributes.Archive) continue;

                var dcmstream = File.Open(dcm, FileMode.Open);
                await using (dcmstream.ConfigureAwait(false))
                {
                    os(dcmstream, o.Bucket, $"{o.Prefix}{DateTime.Now:yyyy-MM-dd}/images/{Utils.SanitizePath(dcm)}");
                }

                attr &= ~FileAttributes.Archive;
                File.SetAttributes(dcm, attr);
            }
            catch (Exception e)
            {
                Console.WriteLine($"Exception '{e}' uploading '{dcm}' to '{o.Prefix}{DateTime.Now:yyyy-MM-dd}/images/{Utils.SanitizePath(dcm)}'");
            }
        }
    }

    private static int Main(string[] args)
    {
        var prog = new Nccidmain(new FileSystem());
        Parser.Default.ParseArguments<SearchOptions, FetchOptions, UploadOptions>(args)
            .WithParsed<SearchOptions>(o => prog.Search(o).RunSynchronously())
            .WithParsed<FetchOptions>(static o => Fetch(o).RunSynchronously())
            .WithParsed<UploadOptions>(o =>
            {
                var creds = new BasicAWSCredentials(o.AwsId, o.AwsKey);
                var s3 = new AmazonS3Client(creds, RegionEndpoint.EUWest2);
                using var tu = new TransferUtility(s3);
                prog.Upload(o, tu.Upload).Wait();
            });
        return 0;
    }
}