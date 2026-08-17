using DartERP.WinForms.Styling;
using Syncfusion.Drawing;
using Syncfusion.Windows.Forms.Chart;

namespace DartERP.WinForms.Controls;

public record BarSegment(string Label, decimal Value);

/// <summary>
/// Horizontal bar tile built on Syncfusion's ChartControl (ChartSeriesType.Bar
/// is the horizontal variant — Column is vertical) instead of the GDI+
/// FillRectangle bars I used before. Horizontal for the same reason as
/// before: this sits in a fairly narrow dashboard tile, and a handful of
/// horizontal rows reads better here than squeezed vertical columns.
/// </summary>
public class SfBarChartCard : DashboardCard
{
    private readonly ChartControl _chart;
    private readonly ToolTip _toolTip = new();
    private int _lastToolTipIndex = -1;

    public SfBarChartCard(string title, string? valueAxisFormat = null) : base(title)
    {
        _chart = new ChartControl
        {
            Dock = DockStyle.Fill,
            BackColor = Theme.CardBackground,
            ShowLegend = false,
            // Syncfusion's own tooltip display pipeline never reliably
            // reflects what's set through Styles[i].ToolTip,
            // PointsToolTipFormat, or ChartRegion.ToolTip on this package
            // version — plenty of empirical proof below on what doesn't
            // work. So ShowToolTips stays off and a plain WinForms ToolTip
            // is driven by hand instead, off the one API that DOES report
            // the correct hovered point: ChartRegionMouseEnter/Leave.
            ShowToolTips = false,
        };
        _chart.ChartArea.BorderWidth = 0;
        _chart.ChartArea.BackInterior = new BrushInfo(Theme.CardBackground);
        _chart.ChartArea.GridBackInterior = new BrushInfo(Theme.CardBackground);
        _chart.PrimaryXAxis.ForeColor = Theme.TextSecondary;
        _chart.PrimaryYAxis.ForeColor = Theme.TextSecondary;
        _chart.PrimaryXAxis.Font = Theme.FontSmall;
        _chart.PrimaryYAxis.Font = Theme.FontSmall;
        // No gridlines on the original hand-drawn chart either — keeps the
        // tile as plain horizontal bars instead of a boxed-in plot area.
        _chart.PrimaryXAxis.DrawGrid = false;
        _chart.PrimaryYAxis.DrawGrid = false;
        // "$#,##0,K" rather than plain "C0" — a trailing comma right before
        // the end of a .NET custom numeric format divides by 1000, so
        // $25,000 renders as $25K instead of $25,000, which is what stops
        // the axis's tick labels from overlapping each other.
        if (valueAxisFormat is not null)
            _chart.PrimaryXAxis.Format = valueAxisFormat;

        // ChartControl.Margin is silently ignored under Dock.Fill — same
        // "the property doesn't actually drive anything" trap as
        // Style.CellStyle.BackColor and Legend.TextColor elsewhere in this
        // migration — so the trailing gap the rightmost tick label needs
        // comes from a plain WinForms Panel's Padding instead, which is
        // guaranteed to be honored.
        var wrapper = new Panel { Dock = DockStyle.Fill, Padding = new Padding(0, 0, 32, 0) };
        wrapper.Controls.Add(_chart);
        Body.Controls.Add(wrapper);

        // ChartSeries.Styles[i].ToolTip is silently ignored for a Bar
        // series (always falls back to just the point's category).
        // PointsToolTipFormat makes the tooltip stop appearing entirely,
        // regardless of which tokens it references or where it's assigned.
        // ChartRegion.ToolTip (set from ChartRegionMouseEnter, confirmed via
        // logging to correctly report the hovered point's real
        // PointIndex — unlike Styles/PointsToolTipFormat, this one isn't
        // looked up through any index-transforming array) also has no
        // visible effect — ShowToolTips' own display pipeline just doesn't
        // read it. So this drives a plain WinForms ToolTip by hand instead,
        // using PointIndex as the one piece of Syncfusion data that's
        // actually trustworthy.
        _chart.ChartRegionMouseEnter += (_, e) =>
        {
            if (e.Region.Type == ChartRegionType.SeriesPoint && e.Region.PointIndex >= 0 && e.Region.PointIndex < _bars.Count)
            {
                if (_lastToolTipIndex == e.Region.PointIndex)
                    return;
                _lastToolTipIndex = e.Region.PointIndex;
                var bar = _bars[e.Region.PointIndex];
                _toolTip.Show($"{bar.Label}: {bar.Value:C0}", _chart, e.Point.X + 16, e.Point.Y + 16, 4000);
            }
            else
            {
                _lastToolTipIndex = -1;
                _toolTip.Hide(_chart);
            }
        };
        _chart.MouseLeave += (_, _) =>
        {
            _lastToolTipIndex = -1;
            _toolTip.Hide(_chart);
        };
    }

    private IReadOnlyList<BarSegment> _bars = Array.Empty<BarSegment>();

    public void SetData(IReadOnlyList<BarSegment> bars)
    {
        _bars = bars;

        _chart.Series.Clear();
        var series = new ChartSeries("Value", ChartSeriesType.Bar);
        series.Style.Interior = new BrushInfo(Theme.AccentPrimary);
        foreach (var bar in bars)
            series.Points.Add(bar.Label, (double)bar.Value);
        _chart.Series.Add(series);
    }
}
