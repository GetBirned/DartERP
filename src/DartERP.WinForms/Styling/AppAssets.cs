namespace DartERP.WinForms.Styling;

/// <summary>
/// Loads the DartERP logo for the sidebar wordmark and (converted to an
/// Icon) the window/taskbar icon.
/// </summary>
public static class AppAssets
{
    private static readonly string LogoPath = Path.Combine(AppContext.BaseDirectory, "Assets", "logo.png");

    /// <summary>
    /// Deliberately NOT cached to a single shared instance: PictureBox
    /// disposes whatever Image is assigned to it when the PictureBox
    /// itself is disposed, regardless of who else might be holding a
    /// reference. The sidebar gets rebuilt (and its old PictureBox
    /// disposed) on every theme toggle, so a shared Logo instance would
    /// get silently disposed out from under every other reference to it
    /// the moment the first toggle happened. Loading fresh each call costs
    /// a cheap file re-read and sidesteps that ownership problem entirely.
    /// </summary>
    public static Image Logo => Image.FromFile(LogoPath);

    /// <summary>
    /// The wordmark is wide (696x313) and its letters share connecting
    /// strokes, so there's no clean square crop of just one glyph to use
    /// as an icon. Instead this letterboxes the whole logo onto a square
    /// transparent canvas, scaled to fit — small but undistorted, rather
    /// than letting Windows squish a non-square bitmap into a square icon.
    /// </summary>
    public static Icon WindowIcon
    {
        get
        {
            var size = Math.Max(Logo.Width, Logo.Height);
            using var canvas = new Bitmap(size, size);
            using (var g = Graphics.FromImage(canvas))
            {
                g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                var scale = (float)size / Logo.Width;
                var scaledHeight = Logo.Height * scale;
                var y = (size - scaledHeight) / 2;
                g.DrawImage(Logo, 0, y, size, scaledHeight);
            }

            return Icon.FromHandle(canvas.GetHicon());
        }
    }
}
