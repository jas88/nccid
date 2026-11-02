using System.IO.Abstractions;
using CsvHelper;

namespace nccid;

public sealed class CsvMerger : Dictionary<string, Dictionary<string, string>>
{
    public CsvMerger(string id, string path, IFileSystem? fs = null, ICollection<string>? exclude = null)
    {
        fs ??= new FileSystem();

        foreach (var csvFile in fs.Directory.GetFiles(path, "*.csv", SearchOption.AllDirectories).Where(csvFile => exclude?.Contains(csvFile) != true))
        {
            using var csv = CsvReader.Create(fs.File.OpenText(csvFile), Utils.CsvOptions);
            while (csv.Read())
            {
                var currentId = csv.GetString(id);

                if (!TryGetValue(currentId, out var datum))
                {
                    datum = [];
                    Add(currentId, datum);
                }

                foreach (var col in csv.Headers.Where(col => !string.IsNullOrWhiteSpace(col) && col != id))
                {
                    datum.Add(col, csv.GetString(col));
                }
            }
        }
    }
}