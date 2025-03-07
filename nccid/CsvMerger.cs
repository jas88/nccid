using System.Globalization;
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
            using var csv = new CsvReader(fs.File.OpenText(csvFile), CultureInfo.InvariantCulture);
            csv.Read();
            csv.ReadHeader();
            while (csv.Read())
            {
                var currentId = csv.GetField(id);
                if (currentId is null || csv.HeaderRecord is null) continue;

                if (!TryGetValue(currentId, out var datum))
                {
                    datum = [];
                    Add(currentId, datum);
                }

                foreach (var col in csv.HeaderRecord.Where(col => !string.IsNullOrWhiteSpace(col) && col != id))
                {
                    datum.Add(col, csv.GetField(col) ?? string.Empty);
                }
            }
        }
    }
}