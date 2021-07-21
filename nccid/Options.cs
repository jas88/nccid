using CommandLine;

namespace nccid
{
    public class Options
    {
            
    }
        
    public class PacsOptions : Options
    {
        [Option("pacshost", Required = true)]
        public string Theirhost { get; set;  }

        [Option("pacsport",Required = false,Default = 104)]
        public int Theirport { get; set;  }

        [Option("ourport",Required = false,Default = 104)]
        public int Ourport { get; set;  }

        [Option("pacsaet",Required = true,HelpText = "PACS AE Title")]
        public string Theirname { get; set;  }

        [Option("ourname",Required = true,HelpText = "Our AE Title")]
        public string Ourname { get; set; }
    }

    public class SearchOptions : PacsOptions
    {
        [Option("csv",Required = true,HelpText = "CSV file to search")]
        public string Filename { get; set; }
        
        [Option("out", Required = false, HelpText = "CSV file to write results to", Default = "results.csv")]
        public string Output { get; set; } = "results.csv";
    }

    public class FetchOptions : PacsOptions
    {
        public string Filename { get; set; }
    }

    public class UploadOptions : Options
    {
        [Option("centre", Required = false, Default = "NHS Scotland", HelpText = "Name of submitting centre")]
        public string CentreName { get; set; } = "NHS Scotland";

        [Option("bucket",Required = true,HelpText = "S3 bucket to upload to")]
        public string bucket { get; set; }
        [Option("prefix",Required = false,Default="",HelpText = "Prefix to add to uploaded files")]
        public string prefix { get; set; }

        [Option("csv",Required = true,HelpText = "CSV file to search")]
        public string Filename { get; set; }
        [Option("awsid",Required = true,HelpText = "AWS ID")]
        public string AwsId { get; set; }
        [Option("awskey",Required = true,HelpText = "AWS secret key")]
        public string AwsKey { get; set; }
    }
}