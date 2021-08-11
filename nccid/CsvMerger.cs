using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Abstractions;
using CsvHelper;

namespace nccid
{
    public class CsvMerger : Dictionary<string, Dictionary<string, string>>
    {
        public CsvMerger(string id,string path,IFileSystem fs=null,List<string> exclude=null) : base()
        {
            fs ??= new FileSystem();

            foreach (var csvFile in fs.Directory.GetFiles(path,"*.csv",SearchOption.AllDirectories))
            {
                if (exclude != null && exclude.Contains(csvFile))
                    continue;
                using var csv = new CsvReader(fs.File.OpenText(csvFile), CultureInfo.InvariantCulture);
                csv.Read();
                csv.ReadHeader();
                while (csv.Read())
                {
                    var currentId = csv.GetField(id);
                    if (!this.TryGetValue(currentId, out var datum))
                    {
                        datum = new Dictionary<string, string>();
                        this.Add(currentId,datum);
                    }
                    foreach (var col in csv.HeaderRecord)
                    {
                        if (!string.IsNullOrWhiteSpace(col) && col != id)
                        {
                            datum.Add(col,csv.GetField(col));
                        }
                    }
                }
            }
        }
    }
}