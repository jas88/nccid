using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using CsvHelper;

namespace nccid
{
    public abstract class INCCIDdata
    {
        public string Pseudonym { get; }
        internal readonly DateTime when;
        public string SubmittingCentre { get; }

        protected INCCIDdata(string cn, DateTime @when,string pn)
        {
            SubmittingCentre = cn;
            Pseudonym = pn;
            this.when = when;
        }

        public static INCCIDdata Make(string centreName, string pcrpos,DateTime _when,string _pn)
        {
            bool pos = pcrpos.ToLowerInvariant() switch
            {
                "0" => false,
                "negative" => false,

                "1" => true,
                "positive" => true,

                _ => throw new ArgumentException($"Invalid PCR test result '{pcrpos}'")
            };
            if (pos)
                return new PositiveData(centreName,_when,_pn);
            else return new NegativeData(centreName,_when,_pn);
        }

        [JsonIgnore]
        public abstract string When { get; }

        public abstract byte[] ToJson();
        public abstract string S3Path(string prefix);
    }

    public class PositiveData : INCCIDdata
    {
        [JsonPropertyName("Date of Positive Covid Swab")]
        public string SwabDate => base.when.ToString("MM/dd/yyyy");

        public override string When => Utils.DicomWindow(when, 3, when.DayOfYear, null);

        public override byte[] ToJson()
        {
            return JsonSerializer.SerializeToUtf8Bytes(this);
        }

        public override string S3Path(string prefix)
        {
            return $"{prefix}{DateTime.Now.ToString("yyyy-MM-dd")}/data/{Pseudonym}_data.json";
        }

        public PositiveData(string centreName, DateTime when,string pn) : base(centreName,when,pn)
        {
        }
    }

    public class NegativeData : INCCIDdata
    {
        public int SwabStatus { get; } = 0;
        public string SwabDate => base.when.ToString("dd/MM/yyyy");

        // ? 21 days of PCR test
        public override string When => $"{when.AddDays(-21).ToString("yyyyMMdd")}-{when.AddDays(21).ToString("yyyyMMdd")}";

        public override byte[] ToJson()
        {
            return JsonSerializer.SerializeToUtf8Bytes(this);
        }

        public override string S3Path(string prefix)
        {
            return $"{prefix}{DateTime.Now.ToString("yyyy-MM-dd")}/data/{Pseudonym}_status.json";
        }

        public NegativeData(string centreName, DateTime when,string pn) : base(centreName,when,pn)
        {
        }
    }
}