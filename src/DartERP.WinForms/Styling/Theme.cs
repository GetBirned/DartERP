namespace DartERP.WinForms.Styling;

/// <summary>
/// Central color/font palette so every screen in DartERP looks consistent.
/// </summary>
public static class Theme
{
    public static readonly Color SidebarBackground = Color.FromArgb(0x1E, 0x29, 0x3B);
    public static readonly Color SidebarHover = Color.FromArgb(0x2A, 0x38, 0x50);
    public static readonly Color SidebarSelected = Color.FromArgb(0x2E, 0x63, 0xEB);
    public static readonly Color SidebarText = Color.FromArgb(0xCB, 0xD5, 0xE1);
    public static readonly Color SidebarTextSelected = Color.White;

    public static readonly Color AppBackground = Color.FromArgb(0xF3, 0xF4, 0xF6);
    public static readonly Color CardBackground = Color.White;
    public static readonly Color BorderColor = Color.FromArgb(0xE2, 0xE8, 0xF0);

    public static readonly Color TextPrimary = Color.FromArgb(0x11, 0x18, 0x27);
    public static readonly Color TextSecondary = Color.FromArgb(0x6B, 0x72, 0x80);

    public static readonly Color AccentBlue = Color.FromArgb(0x25, 0x63, 0xEB);
    public static readonly Color SuccessGreen = Color.FromArgb(0x05, 0x96, 0x69);
    public static readonly Color WarningAmber = Color.FromArgb(0xD9, 0x77, 0x06);
    public static readonly Color DangerRed = Color.FromArgb(0xDC, 0x26, 0x26);
    public static readonly Color NeutralGray = Color.FromArgb(0x9C, 0xA3, 0xAF);

    public static readonly Font FontHeader = new("Segoe UI", 18F, FontStyle.Bold);
    public static readonly Font FontSubheader = new("Segoe UI", 12F, FontStyle.Bold);
    public static readonly Font FontBody = new("Segoe UI", 9.75F, FontStyle.Regular);
    public static readonly Font FontBodyBold = new("Segoe UI", 9.75F, FontStyle.Bold);
    public static readonly Font FontSmall = new("Segoe UI", 8.5F, FontStyle.Regular);
    public static readonly Font FontKpiValue = new("Segoe UI", 22F, FontStyle.Bold);
    public static readonly Font FontNav = new("Segoe UI", 10F, FontStyle.Regular);
    public static readonly Font FontBrand = new("Segoe UI", 14F, FontStyle.Bold);
}
