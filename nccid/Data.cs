using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using JetBrains.Annotations;

namespace nccid;

public abstract record NccidData
{
    public string Pseudonym { get; }
    internal readonly DateTime DtWhen;
    public string SubmittingCentre { [UsedImplicitly] get; }

    protected NccidData(string cn, DateTime DtWhen, string pn)
    {
        SubmittingCentre = cn;
        Pseudonym = pn;
        this.DtWhen = DtWhen;
    }

    public static NccidData Make(string centreName, ReadOnlySpan<char> pcrpos, DateTime when, string pn)
    {
        if (pcrpos.Length == 0)
            throw new ArgumentException($"Invalid PCR test result '{pcrpos}'");

        var pos = pcrpos[0] switch
        {
            '0' => false,
            'n' => pcrpos.Equals("negative", StringComparison.OrdinalIgnoreCase) ? false : throw new ArgumentException($"Invalid PCR test result '{pcrpos}'"),
            'N' => pcrpos.Equals("negative", StringComparison.OrdinalIgnoreCase) ? false : throw new ArgumentException($"Invalid PCR test result '{pcrpos}'"),

            '1' => true,
            'p' => pcrpos.Equals("positive", StringComparison.OrdinalIgnoreCase) ? true : throw new ArgumentException($"Invalid PCR test result '{pcrpos}'"),
            'P' => pcrpos.Equals("positive", StringComparison.OrdinalIgnoreCase) ? true : throw new ArgumentException($"Invalid PCR test result '{pcrpos}'"),

            _ => throw new ArgumentException($"Invalid PCR test result '{pcrpos}'")
        };
        return pos ? new PositiveData(centreName, when, pn) : new NegativeData(centreName, when, pn);
    }

    [JsonIgnore]
    public abstract string SpecialFormatTimestamp { get; }

    public abstract byte[] ToJson();
    public abstract string S3Path(string prefix);
}

public sealed record PositiveData : NccidData
{
    [JsonPropertyName("Date of Positive Covid Swab")]
    [UsedImplicitly]
    public string SwabDate => DtWhen.ToString("MM/dd/yyyy", CultureInfo.InvariantCulture);

    public override string SpecialFormatTimestamp => Utils.DicomWindow(DtWhen, 3, DtWhen.DayOfYear, null);

    public override byte[] ToJson() => JsonSerializer.SerializeToUtf8Bytes(this);

    public override string S3Path(string prefix) => $"{prefix}{DateTime.Now:yyyy-MM-dd}/data/{Pseudonym}_data.json";

    public PositiveData(string centreName, DateTime DtWhen, string pn) : base(centreName, DtWhen, pn)
    {
    }
}

public sealed record NegativeData : NccidData
{
#pragma warning disable IDE0079 // yes, the suppression is marked as erroneous so suppress the erroneous suppression warning...
    [UsedImplicitly]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "<Pending>")]
    public int SwabStatus => 0;
#pragma warning restore IDE0079

    [UsedImplicitly] public string SwabDate => DtWhen.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture);

    // ? 21 days of PCR test
    public override string SpecialFormatTimestamp => $"{DtWhen.AddDays(-21):yyyyMMdd}-{DtWhen.AddDays(21):yyyyMMdd}";

    public override byte[] ToJson() => JsonSerializer.SerializeToUtf8Bytes(this);

    public override string S3Path(string prefix) => $"{prefix}{DateTime.Now:yyyy-MM-dd}/data/{Pseudonym}_status.json";

    public NegativeData(string centreName, DateTime DtWhen, string pn) : base(centreName, DtWhen, pn)
    {
    }
}