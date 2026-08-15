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
    private bool _isHovered;

    /// <summary>
    /// Off by default — the header avatar and the lock screen's avatar
    /// aren't "edit" targets (one opens a menu, the other's just
    /// decorative), so only ProfileForm turns this on.
    /// </summary>
    public bool ShowEditOverlayOnHover { get; set; }

    public Avatar()
    {
        DoubleBuffered = true;
        Cursor = Cursors.Hand;
        MouseEnter += (_, _) => SetHovered(true);
        MouseLeave += (_, _) => SetHovered(false);
    }

    private void SetHovered(bool hovered)
    {
        if (!ShowEditOverlayOnHover || _isHovered == hovered)
            return;

        _isHovered = hovered;
        Invalidate();
    }

    public void SetUser(string displayName, string usernameForColor, Image? picture)
    {
        _picture = picture;
        _initials = GetInitials(displayName);
        _fallbackColor = Palette[StableHash(usernameForColor) % Palette.Length];
        Invalidate();
    }

    /// <summary>
    /// NOTE: string.GetHashCode() is randomized per process on .NET Core+
    /// (a security hardening measure, not a bug) — using it here meant the
    /// same username picked a different avatar color every time the app
    /// restarted, defeating the whole point of deriving a color from the
    /// name instead of storing one. This is a simple FNV-1a hash instead,
    /// which is stable across runs since it's just arithmetic over the
    /// string's own characters, nothing process-specific.
    /// </summary>
    private static int StableHash(string value)
    {
        unchecked
        {
            var hash = 2166136261;
            foreach (var c in value)
                hash = (hash ^ c) * 16777619;
            return (int)(hash & 0x7FFFFFFF);
        }
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
        }
        else
        {
            using var brush = new SolidBrush(_fallbackColor);
            e.Graphics.FillEllipse(brush, bounds);

            var textSize = e.Graphics.MeasureString(_initials, Theme.FontBodyBold);
            var textLocation = new PointF((Width - textSize.Width) / 2, (Height - textSize.Height) / 2);
            e.Graphics.DrawString(_initials, Theme.FontBodyBold, Brushes.White, textLocation);
        }

        if (ShowEditOverlayOnHover && _isHovered)
        {
            using var overlayBrush = new SolidBrush(Color.FromArgb(150, 0, 0, 0));
            e.Graphics.FillEllipse(overlayBrush, bounds);

            var editSize = e.Graphics.MeasureString("Edit", Theme.FontSmall);
            var editLocation = new PointF((Width - editSize.Width) / 2, (Height - editSize.Height) / 2);
            e.Graphics.DrawString("Edit", Theme.FontSmall, Brushes.White, editLocation);
        }
    }
}
