namespace DartERP.WinForms.Styling;

/// <summary>
/// Small styling helpers applied consistently across forms so screens
/// don't each hand-roll button/grid appearance.
/// </summary>
public static class ControlStyleExtensions
{
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
        return button.ApplyRoundedRegion(6);
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
        return button.ApplyRoundedRegion(6);
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
        return button.ApplyRoundedRegion(6);
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
