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

    public SfBarChartCard(string title, string? valueAxisFormat = null) : base(title)
    {
        _chart = new ChartControl
        {
            Dock = DockStyle.Fill,
            BackColor = Theme.CardBackground,
            ShowLegend = false,
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
    }

    public void SetData(IReadOnlyList<BarSegment> bars)
    {
        _chart.Series.Clear();
        var series = new ChartSeries("Value", ChartSeriesType.Bar);
        series.Style.Interior = new BrushInfo(Theme.AccentPrimary);
        foreach (var bar in bars)
            series.Points.Add(bar.Label, (double)bar.Value);
        _chart.Series.Add(series);
    }
}
