using Syncfusion.WinForms.Core;
using Syncfusion.WinForms.DataGrid;
using Syncfusion.WinForms.DataGrid.Enums;

namespace DartERP.WinForms.Styling;

/// <summary>
/// Small styling helpers applied consistently across forms so screens
/// don't each hand-roll button/grid appearance.
/// </summary>
public static class ControlStyleExtensions
{
    /// <summary>
    /// Hand-set to Theme.cs rather than one of Syncfusion's prebuilt themes
    /// (Office2019, Fluent, etc.) — a prebuilt theme means another package
    /// and a color scheme that won't match this app's tan-and-black brand.
    /// Every screen rebuilds from scratch on a light/dark toggle (see
    /// MainForm.RebuildShell), so a freshly-styled grid re-themes for free.
    /// </summary>
    public static void StyleAsSfDataGrid(this SfDataGrid grid)
    {
        grid.AllowEditing = false;
        grid.AllowSorting = true;
        grid.AllowResizingColumns = true;
        grid.SelectionMode = GridSelectionMode.Single;
        grid.NavigationMode = NavigationMode.Row;
        grid.RowHeight = 34;
        grid.HeaderRowHeight = 38;
        grid.Font = Theme.FontBody;

        grid.ThemeName = string.Empty;
        grid.BackColor = Theme.CardBackground;
        grid.Style.BorderStyle = System.Windows.Forms.BorderStyle.None;
        grid.Style.BorderColor = Theme.BorderColor;
        grid.Style.CellStyle.BackColor = Theme.CardBackground;
        grid.Style.CellStyle.TextColor = Theme.TextPrimary;
        grid.Style.CellStyle.Font.Facename = Theme.FontBody.Name;
        grid.Style.CellStyle.Font.Size = Theme.FontBody.Size;
        grid.Style.CellStyle.VerticalAlignment = System.Windows.Forms.VisualStyles.VerticalAlignment.Center;

        grid.Style.HeaderStyle.BackColor = Theme.AppBackground;
        grid.Style.HeaderStyle.TextColor = Theme.TextSecondary;
        grid.Style.HeaderStyle.Font.Facename = Theme.FontBodyBold.Name;
        grid.Style.HeaderStyle.Font.Size = Theme.FontBodyBold.Size;
        grid.Style.HeaderStyle.Font.Bold = true;
        grid.Style.HeaderStyle.VerticalAlignment = System.Windows.Forms.VisualStyles.VerticalAlignment.Center;

        grid.Style.SelectionStyle.BackColor = Theme.SelectionHighlight;
        grid.Style.SelectionStyle.TextColor = Theme.TextPrimary;

        // Style.CellStyle.BackColor above sets the static default, but
        // SfDataGrid doesn't actually paint cells with it — QueryCellStyle
        // is what real per-cell rendering reads from, so the theme colors
        // (and the alternating-row tint, since there's no AlternatingRowStyle
        // property the way DataGridView had one) both have to be reasserted
        // here. Registered before each screen's own QueryCellStyle handler
        // (subscribed later, in BuildColumns), so a screen's column-specific
        // logic (e.g. coloring the Status text) runs after this baseline and
        // layers on top instead of being overwritten by it.
        grid.QueryCellStyle += (_, e) =>
        {
            var background = e.RowIndex % 2 == 1 ? Theme.AlternateRowBackground : Theme.CardBackground;
            // BackColor alone doesn't paint the cell — CellStyleInfo has a
            // separate Interior brush (same split as ChartStyleInfo) that
            // actually drives the fill, so both have to be set together.
            e.Style.BackColor = background;
            e.Style.Interior = new BrushInfo(background);
            e.Style.TextColor = Theme.TextPrimary;
        };
    }

    public static Button StyleAsPrimaryButton(this Button button)
    {
        // Primary CTAs use the near-black brand chrome color rather than the
        // tan accent — tan reads as a light pastel, so white button text on
        // top of it would be nearly illegible. Reusing SidebarBackground
        // keeps this in sync with the sidebar's black across both themes
        // instead of hardcoding a third near-black value.
        button.FlatStyle = FlatStyle.Flat;
        button.FlatAppearance.BorderSize = 0;
        button.BackColor = Theme.SidebarBackground;
        button.ForeColor = Color.White;
        button.Font = Theme.FontBodyBold;
        button.Cursor = Cursors.Hand;
        button.Height = 34;
        button.FlatAppearance.MouseOverBackColor = Theme.SidebarHover;
        return button.ApplyRoundedRegion(10);
    }

    public static Button StyleAsSecondaryButton(this Button button)
    {
        button.FlatStyle = FlatStyle.Flat;
        button.FlatAppearance.BorderSize = 1;
        button.FlatAppearance.BorderColor = Theme.BorderColor;
        button.BackColor = Theme.CardBackground;
        button.ForeColor = Theme.TextPrimary;
        button.Font = Theme.FontBody;
        button.Cursor = Cursors.Hand;
        button.Height = 34;
        button.FlatAppearance.MouseOverBackColor = Theme.AppBackground;
        return button.ApplyRoundedRegion(10);
    }

    public static Button StyleAsDangerButton(this Button button)
    {
        button.FlatStyle = FlatStyle.Flat;
        button.FlatAppearance.BorderSize = 0;
        button.BackColor = Theme.DangerRed;
        button.ForeColor = Color.White;
        button.Font = Theme.FontBodyBold;
        button.Cursor = Cursors.Hand;
        button.Height = 34;
        button.FlatAppearance.MouseOverBackColor = Color.FromArgb(0xB9, 0x1C, 0x1C);
        return button.ApplyRoundedRegion(10);
    }

    public static void StyleAsDataGrid(this DataGridView grid)
    {
        grid.BorderStyle = BorderStyle.None;
        grid.BackgroundColor = Theme.CardBackground;
        grid.GridColor = Theme.BorderColor;
        grid.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
        grid.RowHeadersVisible = false;
        grid.AllowUserToAddRows = false;
        grid.AllowUserToDeleteRows = false;
        grid.AllowUserToResizeRows = false;
        grid.ReadOnly = true;
        grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        grid.MultiSelect = false;
        grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        grid.RowTemplate.Height = 34;
        grid.Font = Theme.FontBody;

        grid.EnableHeadersVisualStyles = false;
        grid.ColumnHeadersDefaultCellStyle.BackColor = Theme.AppBackground;
        grid.ColumnHeadersDefaultCellStyle.ForeColor = Theme.TextSecondary;
        grid.ColumnHeadersDefaultCellStyle.SelectionBackColor = Theme.AppBackground;
        grid.ColumnHeadersDefaultCellStyle.SelectionForeColor = Theme.TextSecondary;
        grid.ColumnHeadersDefaultCellStyle.Font = Theme.FontBodyBold;
        grid.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;
        grid.ColumnHeadersHeight = 38;
        grid.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None;

        grid.DefaultCellStyle.SelectionBackColor = Theme.SelectionHighlight;
        grid.DefaultCellStyle.SelectionForeColor = Theme.TextPrimary;
        grid.DefaultCellStyle.BackColor = Theme.CardBackground;
        grid.DefaultCellStyle.ForeColor = Theme.TextPrimary;
        grid.DefaultCellStyle.Padding = new Padding(6, 0, 6, 0);
        grid.AlternatingRowsDefaultCellStyle.BackColor = Theme.AlternateRowBackground;
    }

    public static TextBox StyleAsInput(this TextBox textBox)
    {
        textBox.Font = Theme.FontBody;
        textBox.BorderStyle = BorderStyle.FixedSingle;
        textBox.BackColor = Theme.CardBackground;
        textBox.ForeColor = Theme.TextPrimary;
        return textBox;
    }

    public static ComboBox StyleAsInput(this ComboBox comboBox)
    {
        comboBox.Font = Theme.FontBody;
        comboBox.FlatStyle = FlatStyle.Flat;
        comboBox.DropDownStyle = ComboBoxStyle.DropDownList;
        comboBox.BackColor = Theme.CardBackground;
        comboBox.ForeColor = Theme.TextPrimary;
        return comboBox;
    }

    /// <summary>
    /// NumericUpDown's spin-button glyphs are native-rendered regardless of
    /// color settings, but the text portion themes correctly — unlike
    /// DateTimePicker, which is skipped here because its native rendering
    /// mostly ignores BackColor/ForeColor entirely (a known WinForms
    /// limitation, not worth fighting for one date field per form).
    /// </summary>
    public static NumericUpDown StyleAsInput(this NumericUpDown numericUpDown)
    {
        numericUpDown.Font = Theme.FontBody;
        numericUpDown.BackColor = Theme.CardBackground;
        numericUpDown.ForeColor = Theme.TextPrimary;
        return numericUpDown;
    }

    /// <summary>
    /// Renders a ComboBox bound to an enum's values as spaced display text
    /// ("InProduction" -> "In Production") instead of the raw member name.
    /// </summary>
    public static ComboBox EnableEnumDisplayFormat(this ComboBox comboBox)
    {
        comboBox.FormattingEnabled = true;
        comboBox.Format += (_, e) =>
        {
            if (e.ListItem is Enum enumValue)
                e.Value = EnumDisplay.For(enumValue);
        };
        return comboBox;
    }
}
