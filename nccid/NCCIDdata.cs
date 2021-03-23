using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace nccid
{
    public abstract class INCCIDdata
    {
        public string Pseudonym { get; }
        internal readonly DateTime when;
        public string SubmittingCentre { get; } = "NHS Tayside";
        public string PatientGroup
        {
            get
            {
                int i = 0;
                foreach (var n in Pseudonym.ToLowerInvariant().ToCharArray())
                {
                    i += n;
                }
                return i % 1 == 0 ? "training" : "validation";
            }
        }

        protected INCCIDdata(DateTime @when,string pn)
        {
            Pseudonym = pn;
            this.when = when;
        }

        public static INCCIDdata Make(bool pos,DateTime _when,string _pn)
        {
            if (pos)
                return new PositiveData(_when,_pn);
            else return new NegativeData(_when,_pn);
        }

        public abstract byte[] ToJson();
        public abstract string S3Path(string prefix);
    }

    public class PositiveData : INCCIDdata
    {
        [JsonPropertyName("Date of Positive Covid Swab")]
        public string SwabDate => base.when.ToString("MM/dd/yyyy");

        public override byte[] ToJson()
        {
            return JsonSerializer.SerializeToUtf8Bytes(this);
        }

        public override string S3Path(string prefix)
        {
            return $"{prefix}{DateTime.Now.ToString("yyyy-MM-dd")}/data/{Pseudonym}_data.json";
        }

        public PositiveData(DateTime when,string pn) : base(when,pn)
        {
        }
    }

    public class NegativeData : INCCIDdata
    {
        public int SwabStatus { get; } = 0;
        public string SwabDate => base.when.ToString("dd/MM/yyyy");
        
        public override byte[] ToJson()
        {
            return JsonSerializer.SerializeToUtf8Bytes(this);
        }

        public override string S3Path(string prefix)
        {
            return $"{prefix}{DateTime.Now.ToString("yyyy-MM-dd")}/data/{Pseudonym}_status.json";
        }

        public NegativeData(DateTime when,string pn) : base(when,pn)
        {
        }
    }
}