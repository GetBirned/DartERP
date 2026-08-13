using DartERP.WinForms.Styling;

namespace DartERP.WinForms.Forms;

/// <summary>
/// Shared helpers for building consistent label+input rows across the
/// various create/edit dialogs, so each form doesn't hand-roll layout.
/// </summary>
public static class FormLayoutHelper
{
    public static Label AddRow(TableLayoutPanel panel, int row, string labelText, Control input)
    {
        var label = new Label
        {
            Text = labelText,
            Font = Theme.FontBody,
            ForeColor = Theme.TextSecondary,
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft,
        };
        panel.Controls.Add(label, 0, row);

        input.Dock = DockStyle.Fill;
        input.Margin = new Padding(0, 4, 0, 4);
        panel.Controls.Add(input, 1, row);

        return label;
    }

    public static Label AddValidationLabel(TableLayoutPanel panel, int row)
    {
        var label = new Label
        {
            Text = string.Empty,
            Font = Theme.FontSmall,
            ForeColor = Theme.DangerRed,
            Dock = DockStyle.Fill,
            AutoSize = false,
            Height = 22,
        };
        panel.Controls.Add(label, 1, row);
        return label;
    }
}
