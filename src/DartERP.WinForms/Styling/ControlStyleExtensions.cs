namespace DartERP.WinForms.Styling;

/// <summary>
/// Small styling helpers applied consistently across forms so screens
/// don't each hand-roll button/grid appearance.
/// </summary>
public static class ControlStyleExtensions
{
    public static Button StyleAsPrimaryButton(this Button button)
    {
        button.FlatStyle = FlatStyle.Flat;
        button.FlatAppearance.BorderSize = 0;
        button.BackColor = Theme.AccentBlue;
        button.ForeColor = Color.White;
        button.Font = Theme.FontBodyBold;
        button.Cursor = Cursors.Hand;
        button.Height = 34;
        button.FlatAppearance.MouseOverBackColor = Color.FromArgb(0x1D, 0x4E, 0xD8);
        return button;
    }

    public static Button StyleAsSecondaryButton(this Button button)
    {
        button.FlatStyle = FlatStyle.Flat;
        button.FlatAppearance.BorderSize = 1;
        button.FlatAppearance.BorderColor = Theme.BorderColor;
        button.BackColor = Color.White;
        button.ForeColor = Theme.TextPrimary;
        button.Font = Theme.FontBody;
        button.Cursor = Cursors.Hand;
        button.Height = 34;
        button.FlatAppearance.MouseOverBackColor = Theme.AppBackground;
        return button;
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
        return button;
    }

    public static void StyleAsDataGrid(this DataGridView grid)
    {
        grid.BorderStyle = BorderStyle.None;
        grid.BackgroundColor = Color.White;
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

        grid.DefaultCellStyle.SelectionBackColor = Color.FromArgb(0xDB, 0xEA, 0xFE);
        grid.DefaultCellStyle.SelectionForeColor = Theme.TextPrimary;
        grid.DefaultCellStyle.Padding = new Padding(6, 0, 6, 0);
        grid.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(0xFA, 0xFA, 0xFB);
    }

    public static TextBox StyleAsInput(this TextBox textBox)
    {
        textBox.Font = Theme.FontBody;
        textBox.BorderStyle = BorderStyle.FixedSingle;
        return textBox;
    }

    public static ComboBox StyleAsInput(this ComboBox comboBox)
    {
        comboBox.Font = Theme.FontBody;
        comboBox.FlatStyle = FlatStyle.Flat;
        comboBox.DropDownStyle = ComboBoxStyle.DropDownList;
        return comboBox;
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
