using System.Drawing.Drawing2D;
using DartERP.WinForms.Styling;

namespace DartERP.WinForms.Controls;

/// <summary>
/// Small circular avatar: shows a user's uploaded picture clipped to a
/// circle if they have one, otherwise a colored circle with their initials.
/// The fallback color is derived from the username so the same person gets
/// the same color every time without storing one anywhere.
/// </summary>
public class Avatar : Panel
{
    private static readonly Color[] Palette =
    [
        Color.FromArgb(0x2E, 0x86, 0xAB), Color.FromArgb(0xA2, 0x3B, 0x72),
        Color.FromArgb(0x3D, 0x8B, 0x5A), Color.FromArgb(0xC7, 0x6A, 0x1D),
        Color.FromArgb(0x6C, 0x5C, 0xE7),
    ];

    private Image? _picture;
    private string _initials = "?";
    private Color _fallbackColor = Palette[0];

    public Avatar()
    {
        DoubleBuffered = true;
        Cursor = Cursors.Hand;
    }

    public void SetUser(string displayName, string usernameForColor, Image? picture)
    {
        _picture = picture;
        _initials = GetInitials(displayName);
        _fallbackColor = Palette[Math.Abs(usernameForColor.GetHashCode()) % Palette.Length];
        Invalidate();
    }

    private static string GetInitials(string displayName)
    {
        var parts = displayName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return parts.Length switch
        {
            0 => "?",
            1 => parts[0][..1].ToUpperInvariant(),
            _ => $"{parts[0][0]}{parts[^1][0]}".ToUpperInvariant(),
        };
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        var bounds = new Rectangle(0, 0, Width - 1, Height - 1);

        using var clipPath = new GraphicsPath();
        clipPath.AddEllipse(bounds);

        if (_picture is not null)
        {
            var oldClip = e.Graphics.Clip;
            e.Graphics.SetClip(clipPath);
            e.Graphics.DrawImage(_picture, bounds);
            e.Graphics.Clip = oldClip;
            return;
        }

        using var brush = new SolidBrush(_fallbackColor);
        e.Graphics.FillEllipse(brush, bounds);

        var textSize = e.Graphics.MeasureString(_initials, Theme.FontBodyBold);
        var textLocation = new PointF((Width - textSize.Width) / 2, (Height - textSize.Height) / 2);
        e.Graphics.DrawString(_initials, Theme.FontBodyBold, Brushes.White, textLocation);
    }
}
