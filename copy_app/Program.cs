using copy_app;
using Ionic.Zip;

var comaprer = new HashComparer();
var folder = @"";

if (!Directory.Exists(folder))
    return;

var dir = new DirectoryInfo(folder).GetFiles("*", SearchOption.AllDirectories);
List<string> filesToAdd = [];
HashHistory? fileHistory;


for (int i = 0; i < dir.Length; i++)
{
    if (comaprer.HasHistory)
    {
        fileHistory = comaprer.History?.HashHistories.FirstOrDefault(x => x.FilePath == dir[i].FullName);

        if (fileHistory != null && !await comaprer.IsSimillarByHash(fileHistory, dir[i].FullName))
        {
            filesToAdd.Add(dir[i].FullName);
        }
    }
    else
    {
        filesToAdd.Add(dir[i].FullName);
    }

    Console.SetCursorPosition(0, 0);
    Console.WriteLine($"Processing files... {Math.Round((decimal)i/dir.Length * 100)}%");
}

Console.WriteLine($"History parsing completed.");
Console.WriteLine();

Console.WriteLine("Saving history.");
await comaprer.SaveHashHistory(dir.Select(x => x.FullName));


if(filesToAdd.Count != 0)
{
    var pBar = 0;
    var pMax = filesToAdd.Count;

    Console.WriteLine("Creating zip file...");

    using ZipFile zip = new()
    {
        UseZip64WhenSaving = Zip64Option.Always,
        CompressionLevel = Ionic.Zlib.CompressionLevel.BestCompression,
        CompressionMethod = CompressionMethod.BZip2 // or CompressionMethod.Deflate or CompressionMethod.None
    };

    zip.AddFiles(filesToAdd);

    Console.WriteLine();
    Console.WriteLine("Entry bytes reading.");
    var (Left, Top) = Console.GetCursorPosition();

    zip.SaveProgress += (sender, e) =>
    {
        if (e.EventType == ZipProgressEventType.Saving_BeforeWriteEntry)
        {
            Console.SetCursorPosition(Left, Top);
            Console.WriteLine($"Writing zip... {Math.Round((decimal)pBar++ / pMax * 100)}%");
        }
    };

    (Left, Top) = Console.GetCursorPosition();

    Console.SetCursorPosition(Left, Top+3);

    zip.Save(@"arch.zip");
    Console.WriteLine("Archive saved.");
}
else
{
    Console.WriteLine("Files has no changes.");
}

Console.ReadKey();