using DartERP.WinForms.Styling;

namespace DartERP.WinForms.Controls;

/// <summary>
/// Small rounded status pill (e.g. "Approved", "Passed", "Below Reorder")
/// used across grids and detail forms for at-a-glance status.
/// </summary>
public class StatusBadge : Label
{
    private Color _accentColor = Theme.NeutralGray;

    public StatusBadge()
    {
        AutoSize = false;
        TextAlign = ContentAlignment.MiddleCenter;
        Font = Theme.FontSmall;
        Height = 24;
        Width = 96;
        Padding = new Padding(4, 0, 4, 0);
        SetColors();
    }

    public Color AccentColor
    {
        get => _accentColor;
        set
        {
            _accentColor = value;
            SetColors();
            Invalidate();
        }
    }

    private void SetColors()
    {
        BackColor = Color.FromArgb(28, _accentColor.R, _accentColor.G, _accentColor.B);
        ForeColor = ControlPaint.Dark(_accentColor, 0.15f);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        using var path = RoundedCorners.CreatePath(ClientRectangle, Height / 2);
        using var brush = new SolidBrush(Color.FromArgb(35, _accentColor.R, _accentColor.G, _accentColor.B));
        e.Graphics.FillPath(brush, path);

        TextRenderer.DrawText(e.Graphics, Text, Font, ClientRectangle, ControlPaint.Dark(_accentColor, 0.1f),
            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
    }
}
