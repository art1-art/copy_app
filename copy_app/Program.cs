using copy_app;
using Ionic.Zip;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using qbch_db_saver_sdc;
using Serilog;
using ShellProgressBar;

// Отключаем в консоли возможность редактирования
DisableConsoleQuickEdit.Go();

using IHost host = Host.CreateDefaultBuilder(args).Build();
var comaprer = new HashComparer();

//Подключаем файл конфигурации
IConfiguration _configuration = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json")
    .Build();

//Внедрение сервисов
static IServiceProvider ConfigureServices(IConfiguration configuration)
{
    ServiceCollection services = new();
    services.AddSingleton(configuration);
    services.AddLogging(builder => builder.AddSerilog(new LoggerConfiguration().ReadFrom.Configuration(configuration).CreateLogger()));
    return services.BuildServiceProvider();
}

IServiceProvider ServiceProvider = ConfigureServices(_configuration);
ILogger<Program> _logger = ServiceProvider.GetRequiredService<ILogger<Program>>();

// Читаем папку, проверяем существует ли
var _folder = _configuration.GetSection("AppConfig:Folder").Value;
if (!Directory.Exists(_folder))
{
    _logger.LogError("Directory not exists {directory}", _folder);
    Console.ReadKey();
    Environment.Exit(0);
}

// Ищем файлы в папке + вложенные
var dir = new DirectoryInfo(_folder).GetFiles("*", SearchOption.AllDirectories);

List<string> filesToAdd = [];
HashHistory? fileHistory;
string fileName = string.Empty;

// Прогресс бар
var options = new ProgressBarOptions
{
    ProgressCharacter = '─',
    ProgressBarOnBottom = false,
    EnableTaskBarProgress = true,
    ForegroundColor = ConsoleColor.Yellow,
    BackgroundCharacter = '\u2593',
    ForegroundColorDone = ConsoleColor.DarkGreen,
};

using (var pbar = new ProgressBar(dir.Length, "Processing files.", options))
{
    for (int i = 0; i < dir.Length; i++)
    {
        if (comaprer.HasHistory)
        {
            fileName = dir[i].FullName;

            fileHistory = comaprer.History?.HashHistories.FirstOrDefault(x => x.FilePath == fileName);

            if (fileHistory == null || !await comaprer.IsSimillarByHash(fileHistory, fileName))
                filesToAdd.Add(dir[i].FullName);
        }
        else
        {
            filesToAdd.Add(dir[i].FullName);
        }

        pbar.Tick();
    }
}

_logger.LogWarning("History parsing completed.");
_logger.LogWarning("Saving history.");

await comaprer.SaveHashHistory(dir.Select(x => x.FullName));


if (filesToAdd.Count != 0)
{
    _logger.LogWarning("Creating zip file.");

    using ZipFile zip = new()
    {
        UseZip64WhenSaving = Zip64Option.Always,
        CompressionLevel = Ionic.Zlib.CompressionLevel.BestCompression,
        CompressionMethod = CompressionMethod.Deflate // or CompressionMethod.Deflate or CompressionMethod.None
    };

    zip.AddFiles(filesToAdd);

    _logger.LogWarning("Entry bytes reading.");
    var pbar2 = new ProgressBar(filesToAdd.Count, "Packing zip file", options);

    zip.SaveProgress += (sender, e) =>
    {
        if (e.EventType == ZipProgressEventType.Saving_BeforeWriteEntry)
            pbar2.Tick();
    };

    zip.Save($"arch{DateTime.Now:mmfff}.zip");
    _logger.LogWarning("Archive saved.");
}
else
{
    _logger.LogWarning("Files has no changes.");
}

Console.ReadKey();