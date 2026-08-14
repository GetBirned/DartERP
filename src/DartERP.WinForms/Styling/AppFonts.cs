using System.Drawing.Text;

namespace DartERP.WinForms.Styling;

/// <summary>
/// Loads the bundled Inter TTFs so the whole app can use it without
/// requiring it to be installed on the machine running DartERP.
///
/// NOTE: PrivateFontCollection.AddMemoryFont takes a raw pointer, not a
/// managed byte[] — it doesn't copy the font data in, it just reads
/// straight out of that memory whenever GDI+ needs to rasterize a glyph.
/// If the byte[] backing that pointer gets garbage collected (which it
/// will, the moment nothing else references it), you get either garbled
/// text or a hard crash the first time a control repaints — and it won't
/// happen right away, so it's a nasty one to debug. Keeping both buffers
/// alive in static fields for the app's whole lifetime is what prevents
/// that; don't refactor this into a local variable inside LoadFonts().
/// </summary>
public static class AppFonts
{
    private static readonly PrivateFontCollection Fonts = new();
    private static readonly byte[] RegularBytes = File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, "Assets", "Fonts", "Inter-Regular.ttf"));
    private static readonly byte[] BoldBytes = File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, "Assets", "Fonts", "Inter-Bold.ttf"));

    public static FontFamily InterFamily { get; } = LoadFonts();

    private static FontFamily LoadFonts()
    {
        AddMemoryFont(RegularBytes);
        AddMemoryFont(BoldBytes);
        return Fonts.Families[0];
    }

    private static unsafe void AddMemoryFont(byte[] fontBytes)
    {
        fixed (byte* ptr = fontBytes)
        {
            Fonts.AddMemoryFont((nint)ptr, fontBytes.Length);
        }
    }
}
