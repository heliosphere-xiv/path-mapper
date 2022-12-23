// See https://aka.ms/new-console-template for more information

using System.Diagnostics;
using System.Globalization;
using System.IO.Compression;
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

using var client = new HttpClient();

async Task<string[]> GetPaths(HttpClient client) {
    Console.WriteLine("downloading paths");
    await using var pathsStream = await client.GetStreamAsync("https://rl2.perchbird.dev/download/export/CurrentPathList.gz");
    await using var gzip = new GZipStream(pathsStream, CompressionMode.Decompress);
    using var reader = new StreamReader(gzip);
    var paths = await reader.ReadToEndAsync();
    return paths
        .Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries)
        .OrderBy(path => path)
        .ToArray();
}

BNpcContainer bnpcs;
if (bnpcJson == "auto") {
    Console.WriteLine("downloading bnpc info");
    var json = await client.GetStringAsync("https://gubal.hasura.app/api/rest/bnpc");
    bnpcs = JsonSerializer.Deserialize<BNpcContainer>(json)!;
} else {
    await using var bnpcFile = File.OpenRead(bnpcJson);
    bnpcs = (await JsonSerializer.DeserializeAsync<BNpcContainer>(bnpcFile))!;
}

var gameData = new GameData(gamePath);
var identifier = new ObjectIdentification(gameData, new GamePathParser(gameData), bnpcs);

string[] allPaths;
if (pathsCsv == "auto") {
    allPaths = await GetPaths(client);
} else {
    await using var file = File.Open(pathsCsv, FileMode.Open);
    using var csv = new CsvReader(new StreamReader(file), new CsvConfiguration(CultureInfo.InvariantCulture) {
        HasHeaderRecord = true,
    });
    allPaths = csv.GetRecords<Record>()
        .Select(record => record.path)
        .OrderBy(path => path)
        .ToArray();
}

var affects = new Dictionary<string, List<string>>();
var stopwatch = new Stopwatch();
stopwatch.Start();

var onePct = (int) Math.Round((float) allPaths.Length / 100);
for (var i = 0; i < allPaths.Length; i++) {
    if (i % onePct == 0) {
        Console.WriteLine($"{(float) i / allPaths.Length * 100:N2}% - {i:N0}/{allPaths.Length:N0}");
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
