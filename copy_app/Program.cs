using copy_app;
using Ionic.Zip;

var comaprer = new HashComparer();
var folder = @"";

if (!Directory.Exists(folder))
    return;

var dir = new DirectoryInfo(folder).GetFiles("*", SearchOption.AllDirectories);
List<string> filesToAdd = [];
HashHistory? fileHistory;

foreach (var file in dir)
{
    if(comaprer.HasHistory)
    {
        fileHistory = comaprer.History?.HashHistories.FirstOrDefault(x => x.FilePath == file.FullName);
                
        if(fileHistory != null && !await comaprer.IsSimillarByHash(fileHistory, file.FullName))
        {
            filesToAdd.Add(file.FullName);
        }
    }
    else
    {
        filesToAdd.Add(file.FullName);
    }
}

await comaprer.SaveHashHistory(dir.Select(x => x.FullName));

if(filesToAdd.Count != 0)
    using (ZipFile zip = new())
    {
        zip.AddFiles(filesToAdd);
        zip.Save(Path.Combine(folder, "arch.zip"));
    }