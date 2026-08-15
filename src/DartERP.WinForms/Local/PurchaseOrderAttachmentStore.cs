namespace DartERP.WinForms.Local;

/// <summary>
/// Disk-based attachment storage, same reasoning as ProfilePictureStore: the
/// database stores a path, not the bytes. Unlike a profile picture (one per
/// user, keyed by userId), a purchase order can have several attachments, so
/// each file gets its own GUID-named copy under a per-PO subfolder rather
/// than being keyed by a single owning id.
/// </summary>
public static class PurchaseOrderAttachmentStore
{
    private static string RootDirectory => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "DartERP", "PurchaseOrderAttachments");

    public static string SaveFromFile(int purchaseOrderId, string sourceFilePath)
    {
        var poDirectory = Path.Combine(RootDirectory, purchaseOrderId.ToString());
        Directory.CreateDirectory(poDirectory);

        var destination = Path.Combine(poDirectory, $"{Guid.NewGuid():N}{Path.GetExtension(sourceFilePath)}");
        File.Copy(sourceFilePath, destination);
        return destination;
    }

    public static void Delete(string storedPath)
    {
        if (File.Exists(storedPath))
            File.Delete(storedPath);
    }
}
