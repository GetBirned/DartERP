using DartERP.WinForms.Styling;

namespace DartERP.WinForms.Controls;

/// <summary>
/// Dashboard KPI tile: a title, a big value, and an optional accent color.
/// </summary>
public class KpiCard : Panel
{
    private readonly Label _titleLabel;
    private readonly Label _valueLabel;
    private Color _accentColor = Theme.AccentBlue;

    public KpiCard(string title, string value)
    {
        Size = new Size(220, 100);
        BackColor = Theme.CardBackground;
        Padding = new Padding(16, 14, 16, 14);
        Margin = new Padding(0, 0, 16, 16);

        _titleLabel = new Label
        {
            Text = title,
            Font = Theme.FontBody,
            ForeColor = Theme.TextSecondary,
            AutoSize = false,
            Dock = DockStyle.Top,
            Height = 24,
        };

        _valueLabel = new Label
        {
            Text = value,
            Font = Theme.FontKpiValue,
            ForeColor = Theme.TextPrimary,
            AutoSize = false,
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.BottomLeft,
        };

        Controls.Add(_valueLabel);
        Controls.Add(_titleLabel);
    }

    public string Value
    {
        get => _valueLabel.Text;
        set => _valueLabel.Text = value;
    }

    public Color AccentColor
    {
        get => _accentColor;
        set { _accentColor = value; Invalidate(); }
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        using var borderPen = new Pen(Theme.BorderColor);
        e.Graphics.DrawRectangle(borderPen, 0, 0, Width - 1, Height - 1);
        using var accentBrush = new SolidBrush(_accentColor);
        e.Graphics.FillRectangle(accentBrush, 0, 0, 4, Height);
    }
}
