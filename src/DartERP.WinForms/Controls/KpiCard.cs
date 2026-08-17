using DartERP.WinForms.Styling;

namespace DartERP.WinForms.Controls;

/// <summary>
/// Dashboard KPI tile: a title, a big value, and an optional accent color.
/// </summary>
public class KpiCard : Panel
{
    private readonly Label _titleLabel;
    private readonly Label _valueLabel;
    private Color _accentColor = Theme.AccentPrimary;
    private bool _isHovered;

    public KpiCard(string title, string value)
    {
        Size = new Size(220, 100);
        BackColor = Theme.CardBackground;
        Padding = new Padding(16, 14, 16, 14);
        Margin = new Padding(0, 0, 16, 16);
        this.ApplyRoundedRegion(10);

        // Every KpiCard on the dashboard is clickable (navigates to the
        // matching module), so the hover state is unconditional rather than
        // gated behind a flag — a background tint blended toward the same
        // SelectionHighlight color the sidebar's active nav item and grid
        // row selection already use, plus a matching border, so "this is
        // interactive" reads the same way it does everywhere else in the app.
        MouseEnter += (_, _) => SetHovered(true);
        MouseLeave += (_, _) => SetHovered(false);

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

        // Clicks on the child labels don't bubble to the panel's own Click
        // event by default — needed so the whole card (not just the sliver
        // of bare panel around the labels) counts as clickable when a
        // caller wires up navigation via this.Click. Same story for
        // MouseEnter/Leave — without wiring the labels too, moving the
        // cursor onto the (large) value label reads as leaving the card.
        _titleLabel.Click += (_, e) => OnClick(e);
        _valueLabel.Click += (_, e) => OnClick(e);
        _titleLabel.MouseEnter += (_, _) => SetHovered(true);
        _titleLabel.MouseLeave += (_, _) => SetHovered(false);
        _valueLabel.MouseEnter += (_, _) => SetHovered(true);
        _valueLabel.MouseLeave += (_, _) => SetHovered(false);
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

    private void SetHovered(bool hovered)
    {
        if (_isHovered == hovered)
            return;
        _isHovered = hovered;
        BackColor = hovered ? Blend(Theme.CardBackground, Theme.SelectionHighlight, 0.18f) : Theme.CardBackground;
        Invalidate();
    }

    private static Color Blend(Color from, Color to, float amount) => Color.FromArgb(
        (int)(from.R + (to.R - from.R) * amount),
        (int)(from.G + (to.G - from.G) * amount),
        (int)(from.B + (to.B - from.B) * amount));

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

        // The left accent bar is a plain FillRectangle, but since it's clipped
        // to this control's rounded Region, its top/bottom edges come out
        // rounded to match automatically — no extra path math needed there.
        using var accentBrush = new SolidBrush(_accentColor);
        e.Graphics.FillRectangle(accentBrush, 0, 0, 4, Height);

        using var borderPen = new Pen(_isHovered ? Theme.SelectionHighlight : Theme.BorderColor);
        using var borderPath = RoundedCorners.CreatePath(ClientRectangle, 10);
        e.Graphics.DrawPath(borderPen, borderPath);
    }
}
