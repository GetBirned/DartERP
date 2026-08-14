using DartERP.WinForms.Local;
using DartERP.WinForms.Styling;

namespace DartERP.WinForms.Controls;

/// <summary>
/// Holds the dark/light toggle, which used to live inline in MainForm's
/// header bar. It moved here once the header switched to a profile avatar
/// + menu, since there wasn't room (or a good reason) to keep a settings
/// control sitting next to the user's own account controls.
/// </summary>
public class SettingsControl : UserControl
{
    public SettingsControl()
    {
        Dock = DockStyle.Fill;
        BackColor = Theme.AppBackground;

        var card = new Panel
        {
            Dock = DockStyle.Top,
            Height = 160,
            BackColor = Theme.CardBackground,
            Padding = new Padding(24),
        };
        card.ApplyRoundedRegion(10);

        var heading = new Label
        {
            Text = "Appearance",
            Font = Theme.FontSubheader,
            ForeColor = Theme.TextPrimary,
            Dock = DockStyle.Top,
            Height = 28,
        };

        var description = new Label
        {
            Text = "Choose how DartERP looks on this device. Saved locally, so it's remembered next time you sign in.",
            Font = Theme.FontBody,
            ForeColor = Theme.TextSecondary,
            Dock = DockStyle.Top,
            Height = 40,
        };

        var toggleRow = new Panel { Dock = DockStyle.Top, Height = 44, Padding = new Padding(0, 8, 0, 0) };
        toggleRow.Controls.Add(BuildThemeToggle());

        card.Controls.Add(toggleRow);
        card.Controls.Add(description);
        card.Controls.Add(heading);

        Controls.Add(card);
    }

    private static Button BuildThemeToggle()
    {
        var isDark = Theme.CurrentMode == ThemeMode.Dark;
        var toggle = new Button
        {
            Text = isDark ? "Switch to Light Mode" : "Switch to Dark Mode",
            Width = 190,
            Dock = DockStyle.Left,
        }.StyleAsSecondaryButton();

        toggle.Click += (_, _) =>
        {
            var newMode = Theme.CurrentMode == ThemeMode.Dark ? ThemeMode.Light : ThemeMode.Dark;
            var preferences = AppPreferences.Load();
            preferences.Theme = newMode;
            preferences.Save();
            Theme.CurrentMode = newMode;
        };

        return toggle;
    }
}
