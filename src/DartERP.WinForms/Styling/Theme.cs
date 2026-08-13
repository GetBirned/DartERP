namespace DartERP.WinForms.Styling;

public enum ThemeMode
{
    Light,
    Dark,
}

/// <summary>
/// One full set of chrome colors. Semantic status colors (success/warning/
/// danger) stay close to their usual hue in both palettes on purpose —
/// green/amber/red is a convention every grid and badge in this app leans
/// on, and going monochrome there for the sake of "on brand" would hurt
/// usability for no visual gain.
/// </summary>
public sealed class ThemePalette
{
    public required Color SidebarBackground { get; init; }
    public required Color SidebarHover { get; init; }
    public required Color SidebarSelected { get; init; }
    public required Color SidebarText { get; init; }
    public required Color SidebarTextSelected { get; init; }

    public required Color AppBackground { get; init; }
    public required Color CardBackground { get; init; }
    public required Color BorderColor { get; init; }

    public required Color TextPrimary { get; init; }
    public required Color TextSecondary { get; init; }

    /// <summary>Grid row selection tint and zebra-striping — subtle enough not to compete with status badge colors sitting inside the same row.</summary>
    public required Color SelectionHighlight { get; init; }
    public required Color AlternateRowBackground { get; init; }

    /// <summary>
    /// Opaque low-stock row highlight (Inventory screen). DataGridView cell
    /// backgrounds don't reliably alpha-composite the way custom-painted
    /// controls do, so unlike StatusBadge's translucent tint, this needs to
    /// be a real solid color per theme rather than WarningAmber blended
    /// over an assumed-white background.
    /// </summary>
    public required Color WarningTint { get; init; }

    public required Color AccentPrimary { get; init; }

    // Status semantics — deliberately unchanged in hue from before the
    // rebrand (see Theme's class doc). AccentBlue specifically is the
    // "in progress / approved" indicator (Approved POs, In Production
    // work orders/serialized items) — a distinct role from AccentPrimary,
    // which is the brand tan used for buttons, KPI accents, and selection.
    public required Color AccentBlue { get; init; }
    public required Color SuccessGreen { get; init; }
    public required Color WarningAmber { get; init; }
    public required Color DangerRed { get; init; }
    public required Color NeutralGray { get; init; }
}

/// <summary>
/// Central color/font palette so every screen in DartERP looks consistent.
/// Colors are live-swappable via <see cref="CurrentMode"/> — every property
/// here reads from whichever palette is active, so flipping the mode and
/// rebuilding a screen (see MainForm's NavigateTo) is enough to re-skin it,
/// no restart needed.
/// </summary>
public static class Theme
{
    // Brand accent is the exact tan from the DartERP logo. Primary chrome
    // (sidebar, buttons) is black/white — tan is reserved for accents and
    // selection states so it stays a highlight, not the base color.
    private static readonly Color BrandTan = Color.FromArgb(0xD4, 0xC6, 0xA6);

    private static readonly ThemePalette LightPalette = new()
    {
        SidebarBackground = Color.FromArgb(0x18, 0x18, 0x18),
        SidebarHover = Color.FromArgb(0x28, 0x28, 0x28),
        SidebarSelected = BrandTan,
        SidebarText = Color.FromArgb(0xB8, 0xAF, 0x9E),
        SidebarTextSelected = Color.FromArgb(0x18, 0x18, 0x18),

        AppBackground = Color.FromArgb(0xFA, 0xF9, 0xF7),
        CardBackground = Color.White,
        BorderColor = Color.FromArgb(0xE5, 0xDF, 0xD3),

        TextPrimary = Color.FromArgb(0x1A, 0x1A, 0x1A),
        TextSecondary = Color.FromArgb(0x6B, 0x64, 0x59),

        SelectionHighlight = Color.FromArgb(0xF0, 0xEB, 0xE0),
        AlternateRowBackground = Color.FromArgb(0xFA, 0xF9, 0xF7),
        WarningTint = Color.FromArgb(0xFF, 0xFB, 0xEB),

        AccentPrimary = BrandTan,
        AccentBlue = Color.FromArgb(0x25, 0x63, 0xEB),
        SuccessGreen = Color.FromArgb(0x05, 0x96, 0x69),
        WarningAmber = Color.FromArgb(0xD9, 0x77, 0x06),
        DangerRed = Color.FromArgb(0xDC, 0x26, 0x26),
        NeutralGray = Color.FromArgb(0x9C, 0xA3, 0xAF),
    };

    private static readonly ThemePalette DarkPalette = new()
    {
        SidebarBackground = Color.FromArgb(0x0E, 0x0E, 0x0E),
        SidebarHover = Color.FromArgb(0x22, 0x22, 0x22),
        SidebarSelected = BrandTan,
        SidebarText = Color.FromArgb(0x9C, 0x94, 0x88),
        SidebarTextSelected = Color.FromArgb(0x18, 0x18, 0x18),

        AppBackground = Color.FromArgb(0x14, 0x14, 0x14),
        CardBackground = Color.FromArgb(0x1F, 0x1F, 0x1F),
        BorderColor = Color.FromArgb(0x33, 0x33, 0x33),

        TextPrimary = Color.FromArgb(0xF5, 0xF3, 0xEF),
        TextSecondary = Color.FromArgb(0xA8, 0xA0, 0x93),

        SelectionHighlight = Color.FromArgb(0x33, 0x2E, 0x24),
        AlternateRowBackground = Color.FromArgb(0x24, 0x24, 0x24),
        WarningTint = Color.FromArgb(0x3A, 0x2E, 0x14),

        AccentPrimary = BrandTan,
        AccentBlue = Color.FromArgb(0x5B, 0x8D, 0xF0),
        SuccessGreen = Color.FromArgb(0x22, 0xB0, 0x7D),
        WarningAmber = Color.FromArgb(0xE8, 0x93, 0x2E),
        DangerRed = Color.FromArgb(0xF0, 0x50, 0x50),
        NeutralGray = Color.FromArgb(0x8A, 0x8A, 0x8A),
    };

    private static ThemePalette _current = LightPalette;
    private static ThemeMode _currentMode = ThemeMode.Light;

    /// <summary>Raised after the active palette changes. Subscribers should rebuild (not just repaint) anything holding onto colors from the previous palette.</summary>
    public static event EventHandler? ThemeChanged;

    public static ThemeMode CurrentMode
    {
        get => _currentMode;
        set
        {
            if (_currentMode == value)
                return;

            _currentMode = value;
            _current = value == ThemeMode.Dark ? DarkPalette : LightPalette;
            ThemeChanged?.Invoke(null, EventArgs.Empty);
        }
    }

    public static Color SidebarBackground => _current.SidebarBackground;
    public static Color SidebarHover => _current.SidebarHover;
    public static Color SidebarSelected => _current.SidebarSelected;
    public static Color SidebarText => _current.SidebarText;
    public static Color SidebarTextSelected => _current.SidebarTextSelected;

    public static Color AppBackground => _current.AppBackground;
    public static Color CardBackground => _current.CardBackground;
    public static Color BorderColor => _current.BorderColor;

    public static Color TextPrimary => _current.TextPrimary;
    public static Color TextSecondary => _current.TextSecondary;

    public static Color SelectionHighlight => _current.SelectionHighlight;
    public static Color AlternateRowBackground => _current.AlternateRowBackground;
    public static Color WarningTint => _current.WarningTint;

    public static Color AccentPrimary => _current.AccentPrimary;
    public static Color AccentBlue => _current.AccentBlue;
    public static Color SuccessGreen => _current.SuccessGreen;
    public static Color WarningAmber => _current.WarningAmber;
    public static Color DangerRed => _current.DangerRed;
    public static Color NeutralGray => _current.NeutralGray;

    public static readonly Font FontHeader = new("Segoe UI", 18F, FontStyle.Bold);
    public static readonly Font FontSubheader = new("Segoe UI", 12F, FontStyle.Bold);
    public static readonly Font FontBody = new("Segoe UI", 9.75F, FontStyle.Regular);
    public static readonly Font FontBodyBold = new("Segoe UI", 9.75F, FontStyle.Bold);
    public static readonly Font FontSmall = new("Segoe UI", 8.5F, FontStyle.Regular);
    public static readonly Font FontKpiValue = new("Segoe UI", 22F, FontStyle.Bold);
    public static readonly Font FontNav = new("Segoe UI", 10F, FontStyle.Regular);
    public static readonly Font FontBrand = new("Segoe UI", 14F, FontStyle.Bold);
}
