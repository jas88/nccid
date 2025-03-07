using CommandLine;
using JetBrains.Annotations;

// CommandLine uses properties implicitly so disable conflicting JetBrains annotations
// ReSharper disable AutoPropertyCanBeMadeGetOnly.Global
// ReSharper disable PropertyCanBeMadeInitOnly.Global

namespace nccid;

public abstract class Options;

public abstract class PacsOptions : Options
{
    [Option("pacshost", Required = true)]
    public required string Theirhost { get; set; }

    [Option("pacsport", Required = false, Default = 104)]
    public int Theirport { get; set; }

    [Option("ourport", Required = false, Default = 104)]
    public int Ourport { [UsedImplicitly] get; set; }

    [Option("pacsaet", Required = true, HelpText = "PACS AE Title")]
    public required string Theirname { get; set; }

    [Option("ourname", Required = true, HelpText = "Our AE Title")]
    public required string Ourname { get; set; }
}

[Verb("search", HelpText = "Query the PACS for relevant patient data")]
public sealed class SearchOptions : PacsOptions
{
    [Option("csv", Required = true, HelpText = "CSV file to search")]
    public required string Filename { get; set; }

    [Option("out", Required = false, HelpText = "CSV file to write results to", Default = "results.csv")]
    public string Output { get; set; } = "results.csv";
}

[Verb("fetch", HelpText = "(TODO) Retrieve data from the PACS")]
public sealed class FetchOptions : PacsOptions
{
    [UsedImplicitly] public required string Filename { get; set; }
}

[Verb("upload", HelpText = "Send retrieved patient data to the NCCID repository")]
public sealed class UploadOptions : Options
{
    [Option("centre", Required = false, Default = "NHS Scotland", HelpText = "Name of submitting centre")]
    public string CentreName { get; set; } = "NHS Scotland";

    [Option("bucket", Required = true, HelpText = "S3 bucket to upload to")]
    public required string Bucket { get; set; }

    [Option("prefix", Required = false, Default = "", HelpText = "Prefix to add to uploaded files")]
    public string Prefix { get; set; } = "";

    [Option("csv", Required = true, HelpText = "CSV file to search")]
    public required string Filename { get; set; }

    [Option("awsid", Required = true, HelpText = "AWS ID")]
    public required string AwsId { get; set; }

    [Option("awskey", Required = true, HelpText = "AWS secret key")]
    public required string AwsKey { get; set; }
}