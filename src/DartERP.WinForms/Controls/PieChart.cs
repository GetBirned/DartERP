using System.Drawing.Drawing2D;
using DartERP.WinForms.Styling;

namespace DartERP.WinForms.Controls;

public record PieSlice(string Label, float Value, Color Color);

/// <summary>
/// Hand-drawn donut chart + legend, GDI+ only — same call as the rest of
/// this app's custom-drawn controls (StatusBadge, ProductIconRenderer):
/// a charting library would mean fighting a third-party theming API to
/// hit the exact brand palette for two or three chart types, when plain
/// FillPie/DrawPie gets there directly with pixel-exact control.
/// </summary>
public class PieChart : DashboardCard
{
    private List<PieSlice> _slices = [];

    public PieChart(string title) : base(title)
    {
        Body.Paint += Body_Paint;
    }

    public void SetData(IReadOnlyList<PieSlice> slices)
    {
        _slices = slices.Where(s => s.Value > 0).ToList();
        Body.Invalidate();
    }

    private void Body_Paint(object? sender, PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;

        if (_slices.Count == 0)
        {
            TextRenderer.DrawText(g, "No data yet.", Theme.FontSmall, Body.ClientRectangle, Theme.TextSecondary,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
            return;
        }

        var total = _slices.Sum(s => s.Value);
        var diameter = Math.Max(0, Math.Min(Body.Height - 8, 110));
        var donutRect = new RectangleF(0, (Body.Height - diameter) / 2f, diameter, diameter);

        var startAngle = -90f;
        using (var separatorPen = new Pen(Theme.CardBackground, 2))
        {
            foreach (var slice in _slices)
            {
                var sweep = 360f * (slice.Value / total);
                using var brush = new SolidBrush(slice.Color);
                g.FillPie(brush, donutRect.X, donutRect.Y, donutRect.Width, donutRect.Height, startAngle, sweep);
                g.DrawPie(separatorPen, donutRect.X, donutRect.Y, donutRect.Width, donutRect.Height, startAngle, sweep);
                startAngle += sweep;
            }
        }

        // Punch the donut hole last so it sits on top of every slice.
        var holeDiameter = diameter * 0.55f;
        var holeRect = new RectangleF(
            donutRect.X + (diameter - holeDiameter) / 2, donutRect.Y + (diameter - holeDiameter) / 2,
            holeDiameter, holeDiameter);
        using (var holeBrush = new SolidBrush(Theme.CardBackground))
            g.FillEllipse(holeBrush, holeRect);

        var legendX = donutRect.Right + 18f;
        var legendY = Math.Max(0f, (Body.Height - _slices.Count * 20) / 2f);
        const float swatchSize = 10f;

        foreach (var slice in _slices)
        {
            using (var swatchBrush = new SolidBrush(slice.Color))
                g.FillRectangle(swatchBrush, legendX, legendY + 3, swatchSize, swatchSize);

            var text = $"{slice.Label} ({slice.Value:0})";
            var textRect = new Rectangle((int)(legendX + swatchSize + 6), (int)legendY, (int)(Body.Width - legendX - swatchSize - 6), 20);
            TextRenderer.DrawText(g, text, Theme.FontSmall, textRect, Theme.TextPrimary,
                TextFormatFlags.NoPrefix | TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);

            legendY += 20;
        }
    }
}
