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

        // Body.Padding only affects docked child controls, not manual
        // drawing in a Paint handler — same gotcha as LetterSpacedLabel.
        // Everything below is positioned relative to this inset rectangle
        // instead of raw Body.Width/Height, so the chart actually gets the
        // same 16px margin every other dashboard card has.
        var bounds = new Rectangle(
            Body.Padding.Left, Body.Padding.Top,
            Math.Max(0, Body.Width - Body.Padding.Horizontal),
            Math.Max(0, Body.Height - Body.Padding.Vertical));

        if (_slices.Count == 0)
        {
            TextRenderer.DrawText(g, "No data yet.", Theme.FontSmall, bounds, Theme.TextSecondary,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
            return;
        }

        const float swatchSize = 10f;
        const float legendGap = 18f;

        // Measure the legend up front so the donut+legend pair can be
        // centered as one block, instead of the donut sitting flush left
        // with a lopsided gap of empty space wherever the (short) legend
        // text happens to end.
        var legendTextWidth = _slices
            .Select(s => TextRenderer.MeasureText(g, $"{s.Label} ({s.Value:0})", Theme.FontSmall).Width)
            .DefaultIfEmpty(0)
            .Max();
        var legendWidth = swatchSize + 6 + legendTextWidth;

        var diameter = Math.Max(0, Math.Min(bounds.Height - 8, 110));
        var contentWidth = diameter + legendGap + legendWidth;
        var startX = bounds.Left + Math.Max(0, (bounds.Width - contentWidth) / 2f);

        var total = _slices.Sum(s => s.Value);
        var donutRect = new RectangleF(startX, bounds.Top + (bounds.Height - diameter) / 2f, diameter, diameter);

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

        var legendX = donutRect.Right + legendGap;
        var legendY = Math.Max(bounds.Top, bounds.Top + (bounds.Height - _slices.Count * 20) / 2f);

        foreach (var slice in _slices)
        {
            using (var swatchBrush = new SolidBrush(slice.Color))
                g.FillRectangle(swatchBrush, legendX, legendY + 3, swatchSize, swatchSize);

            var text = $"{slice.Label} ({slice.Value:0})";
            var textRect = new Rectangle((int)(legendX + swatchSize + 6), (int)legendY, (int)legendTextWidth + 4, 20);
            TextRenderer.DrawText(g, text, Theme.FontSmall, textRect, Theme.TextPrimary,
                TextFormatFlags.NoPrefix | TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);

            legendY += 20;
        }
    }
}
