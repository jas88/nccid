using System;
using CsvHelper;
using Dicom;
using nccid;

namespace nccid
{
    public class Swab
    {
        public string Chi { get; }
        public int Result { get; }
        public DateTime When { get; }

        public Swab(CsvReader rdr)
        {
            Chi = rdr.GetField("CHI");
            Result = rdr.GetField<int>("Result");
            When = rdr.GetField<DateTime>("Date");
        }
    }
        
    public record CovidStatus
    {
        private bool _traininggroup,_covid;
        public string Pseudonym { get; }
        public DateTime SwabDate { get; }
        public string SubmittingCentre { get; }
        public string PatientGroup => _traininggroup?"training":"validation";
        public int SwabStatus => _covid ? 1 : 0;

        public CovidStatus(DateTime swabDate, string submittingCentre, string pseudonym, bool traininggroup, bool covid)
        {
            SwabDate = swabDate;
            SubmittingCentre = submittingCentre;
            Pseudonym = pseudonym;
            _traininggroup = traininggroup;
            _covid = covid;
        }

        public CovidStatus(DicomDataset ds, bool training, bool hascovid) : this(
            ds.GetDateTime(DicomTag.StudyDate, DicomTag.StudyTime), "BadHospital", ds.GetString(DicomTag.PatientID),
            training, hascovid)
        {
        }
    };
}