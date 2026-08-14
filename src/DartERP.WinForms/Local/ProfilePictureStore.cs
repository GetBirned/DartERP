namespace DartERP.WinForms.Local;

/// <summary>
/// Disk-based profile picture storage — same reasoning as every other
/// file-backed asset in this app (the logo, the login video): the database
/// stores a path, not the bytes, and the actual file lives under LocalAppData.
/// </summary>
public static class ProfilePictureStore
{
    private static string Directory => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "DartERP", "ProfilePictures");

    public static string SaveFromFile(int userId, string sourceFilePath)
    {
        System.IO.Directory.CreateDirectory(Directory);

        // Clean up any previous picture for this user first — otherwise
        // switching from a .png to a .jpg leaves the old file behind
        // forever, orphaned under a path nothing points to anymore.
        foreach (var existing in System.IO.Directory.GetFiles(Directory, $"{userId}.*"))
            File.Delete(existing);

        var destination = Path.Combine(Directory, $"{userId}{Path.GetExtension(sourceFilePath)}");
        File.Copy(sourceFilePath, destination, overwrite: true);
        return destination;
    }

    /// <summary>
    /// Loads via a byte buffer rather than Image.FromFile(path) directly —
    /// FromFile keeps the source file locked for as long as the Image is
    /// alive, which would block re-uploading a new picture to the same path
    /// (or even just deleting it) while the old one is still on screen.
    /// </summary>
    public static Image? Load(string? profilePicturePath)
    {
        if (string.IsNullOrWhiteSpace(profilePicturePath) || !File.Exists(profilePicturePath))
            return null;

        var bytes = File.ReadAllBytes(profilePicturePath);
        return Image.FromStream(new MemoryStream(bytes));
    }
}
