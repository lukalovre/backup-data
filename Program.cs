using Newtonsoft.Json;

namespace backup_data;

class Program
{
    private static string _dailyPath = null!;
    private static string _weeklyPath = null!;

    public record Settings
    {
        public string BackupLocation { get; set; } = null!;
        public string BackupName { get; set; } = null!;
        public List<string> BackupPaths { get; set; } = null!;
    }

    public static string SettingsFilePath => Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Settings.json");

    static void Main(string[] args)
    {
        var settings = LoadSettings();

        var rootPath = Path.Combine(settings.BackupLocation, settings.BackupName);

        if (!Directory.Exists(rootPath))
        {
            Directory.CreateDirectory(rootPath);
        }

        _dailyPath = Path.Combine(rootPath, "Daily");

        if (!Directory.Exists(_dailyPath))
        {
            Directory.CreateDirectory(_dailyPath);
        }

        _weeklyPath = Path.Combine(rootPath, DateTime.Now.Year.ToString());

        if (!Directory.Exists(_weeklyPath))
        {
            Directory.CreateDirectory(_weeklyPath);
        }

        var name = $"{settings.BackupName}_{DateTime.Now:yyyy_MM_dd}";

        var path = Path.Combine(_dailyPath, name);

        if (File.Exists(path))
        {
            return;
        }

        MakeBackup();

        CheckWeekly();
        ChackDaily();
    }

    private static void MakeBackup()
    {
    }

    private static void ChackDaily()
    {
        var start = new TimeSpan(8, 0, 0);
        var now = DateTime.Now.TimeOfDay;

        if (now < start)
        {
            return;
        }

        var directory = new DirectoryInfo(_dailyPath);

        if (!Directory.Exists(_dailyPath))
        {
            return;
        }

        var latestFile = directory.GetFiles()
            .OrderByDescending(f => f.LastWriteTime)
            .FirstOrDefault();

        if (latestFile == null || DateTime.Now.Date != latestFile.CreationTime.Date)
        {
            throw new Exception("Daily backup not done");
        }
    }

    private static void CheckWeekly()
    {
        var sundayLastWeek = DateTime.Now.AddDays(-(int)DateTime.Now.DayOfWeek - 0);

        var directory = new DirectoryInfo(_weeklyPath);
        var latestFile = directory.GetFiles()
            .OrderByDescending(f => f.LastWriteTime)
            .FirstOrDefault();

        if (latestFile == null
            || sundayLastWeek.Date > latestFile.CreationTime.Date)
        {
            WeeklyMove();
        }
    }

    private static void WeeklyMove()
    {
        try
        {
            var direcrotyInfo = new DirectoryInfo(_dailyPath);
            var file = direcrotyInfo.GetFiles("*.bak").LastOrDefault();

            File.Move(file.FullName, Path.Combine(_weeklyPath, file.Name));

            Directory.Delete(_dailyPath, true);
        }
        catch (Exception ex)
        {
            throw new Exception($"Weekly backup not done\n{ex.Message}");
        }
    }

    private static Settings LoadSettings()
    {
        if (!File.Exists(SettingsFilePath))
        {
            var jsonSettings = new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Include,
                Formatting = Formatting.Indented,
            };
            File.WriteAllText(SettingsFilePath, JsonConvert.SerializeObject(new Settings(), jsonSettings));
        }

        var settingsText = File.ReadAllText(SettingsFilePath);
        return JsonConvert.DeserializeObject<Settings>(settingsText) ?? new Settings();
    }
}
