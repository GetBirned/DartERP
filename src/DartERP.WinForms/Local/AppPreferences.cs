using System.Text.Json;
using DartERP.WinForms.Styling;

namespace DartERP.WinForms.Local;

/// <summary>
/// Small local settings file for things that don't belong in the database
/// (or can't yet — there's no Users table until the auth phase lands, so
/// theme preference lives here for now rather than per-user in SQL).
/// </summary>
public class AppPreferences
{
    public ThemeMode Theme { get; set; } = ThemeMode.Light;

    private static string FilePath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "DartERP", "preferences.json");

    public static AppPreferences Load()
    {
        try
        {
            if (File.Exists(FilePath))
            {
                var loaded = JsonSerializer.Deserialize<AppPreferences>(File.ReadAllText(FilePath));
                if (loaded is not null)
                    return loaded;
            }
        }
        catch
        {
            // Corrupt or unreadable preferences file — fall back to defaults
            // rather than block startup over a non-essential settings file.
        }

        return new AppPreferences();
    }

    public void Save()
    {
        var directory = Path.GetDirectoryName(FilePath)!;
        Directory.CreateDirectory(directory);
        File.WriteAllText(FilePath, JsonSerializer.Serialize(this));
    }
}
