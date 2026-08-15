using System.Drawing.Drawing2D;
using DartERP.WinForms.Styling;

namespace DartERP.WinForms.Controls;

public record BarSegment(string Label, decimal Value);

/// <summary>
/// Hand-drawn horizontal bar chart, GDI+ only — same rationale as
/// PieChart. Horizontal rather than vertical bars specifically because
/// this sits in a fairly narrow dashboard tile: a handful of horizontal
/// rows reads far better in that footprint than squeezed vertical columns
/// with rotated labels underneath.
/// </summary>
public class BarChart : DashboardCard
{
    private readonly Func<decimal, string> _valueFormatter;
    private List<BarSegment> _bars = [];

    public BarChart(string title, Func<decimal, string>? valueFormatter = null) : base(title)
    {
        _valueFormatter = valueFormatter ?? (v => v.ToString("N0"));
        Body.Paint += Body_Paint;
    }

    public void SetData(IReadOnlyList<BarSegment> bars)
    {
        _bars = bars.ToList();
        Body.Invalidate();
    }

    private void Body_Paint(object? sender, PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;

        // Body.Padding only affects docked child controls, not manual
        // drawing in a Paint handler — same gotcha as LetterSpacedLabel.
        // Everything below is positioned relative to this inset rectangle
        // instead of raw Body.Width/Height, so labels get the same 16px
        // margin every other dashboard card has instead of sitting flush
        // against the edge.
        var bounds = new Rectangle(
            Body.Padding.Left, Body.Padding.Top,
            Math.Max(0, Body.Width - Body.Padding.Horizontal),
            Math.Max(0, Body.Height - Body.Padding.Vertical));

        if (_bars.Count == 0 || _bars.All(b => b.Value == 0))
        {
            TextRenderer.DrawText(g, "No data yet.", Theme.FontSmall, bounds, Theme.TextSecondary,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
            return;
        }

        const float labelWidth = 104f;
        const float valueWidth = 64f;
        var maxValue = _bars.Max(b => b.Value);
        var rowHeight = (float)bounds.Height / _bars.Count;
        var barHeight = Math.Min(16f, rowHeight * 0.5f);
        var barAreaWidth = Math.Max(0, bounds.Width - labelWidth - valueWidth);
        var barAreaLeft = bounds.Left + labelWidth;

        for (var i = 0; i < _bars.Count; i++)
        {
            var bar = _bars[i];
            var rowCenterY = bounds.Top + rowHeight * i + rowHeight / 2f;

            var labelRect = new Rectangle(bounds.Left, (int)(rowCenterY - 8), (int)labelWidth, 16);
            TextRenderer.DrawText(g, bar.Label, Theme.FontSmall, labelRect, Theme.TextSecondary,
                TextFormatFlags.NoPrefix | TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);

            using (var trackBrush = new SolidBrush(Theme.AppBackground))
                g.FillRectangle(trackBrush, barAreaLeft, rowCenterY - barHeight / 2, barAreaWidth, barHeight);

            var barWidth = maxValue == 0 ? 0 : (float)(bar.Value / maxValue) * barAreaWidth;
            if (barWidth > 0)
            {
                using var barBrush = new SolidBrush(Theme.AccentPrimary);
                g.FillRectangle(barBrush, barAreaLeft, rowCenterY - barHeight / 2, barWidth, barHeight);
            }

            var valueRect = new Rectangle((int)(barAreaLeft + barAreaWidth + 6), (int)(rowCenterY - 8), (int)valueWidth - 6, 16);
            TextRenderer.DrawText(g, _valueFormatter(bar.Value), Theme.FontSmall, valueRect, Theme.TextPrimary,
                TextFormatFlags.NoPrefix | TextFormatFlags.Right | TextFormatFlags.VerticalCenter);
        }
    }
}
