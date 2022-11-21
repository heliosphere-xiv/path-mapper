// See https://aka.ms/new-console-template for more information

using System.Diagnostics;
using System.Globalization;
using System.Text.Json;
using CsvHelper;
using CsvHelper.Configuration;
using Lumina;
using PathMapper;

if (args.Length != 4) {
    Console.WriteLine("args: <path to sqpack> <path to rl2 csv> <path to tc bnpc data> <output json path>");
    return;
}

var gamePath = args[0];
var pathsCsv = args[1];
var bnpcJson = args[2];
var outputPath = args[3];

await using var bnpcFile = File.OpenRead(bnpcJson);
var bnpcs = (await JsonSerializer.DeserializeAsync<BNpcContainer>(bnpcFile))!;

var gameData = new GameData(gamePath);
var identifier = new ObjectIdentification(gameData, new GamePathParser(), bnpcs);

await using var file = File.Open(pathsCsv, FileMode.Open);
using var csv = new CsvReader(new StreamReader(file), new CsvConfiguration(CultureInfo.InvariantCulture) {
    HasHeaderRecord = true,
});
var allPaths = csv.GetRecords<Record>()
    .Select(record => record.path)
    .OrderBy(path => path)
    .ToList();

var affects = new Dictionary<string, List<string>>();
var stopwatch = new Stopwatch();
stopwatch.Start();

var onePct = (int) Math.Round((float) allPaths.Count / 100);
for (var i = 0; i < allPaths.Count; i++) {
    if (i % onePct == 0) {
        Console.WriteLine($"{(float) i / allPaths.Count * 100:N2}% - {i:N0}/{allPaths.Count:N0}");
    }

    var path = allPaths[i];
    foreach (var (key, _) in identifier.Identify(path)) {
        if (affects.ContainsKey(path)) {
            affects[path].Add(key);
        } else {
            affects[path] = new List<string> { key };
        }
    }
}

Console.WriteLine($"processing took {stopwatch.Elapsed}");

await using var output = File.Create(outputPath);
await JsonSerializer.SerializeAsync(output, affects);

public class Record {
    public int hash { get; set; }
    public int index { get; set; }
    public string path { get; set; }
}

[Serializable]
public class BNpcContainer {
    public List<BNpcMapEntry> bnpc { get; set; }
}

[Serializable]
public class BNpcMapEntry {
    public uint bnpcBase { get; set; }
    public uint bnpcName { get; set; }
}
