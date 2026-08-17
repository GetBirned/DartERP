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
        // No gridlines on the original hand-drawn chart either — keeps the
        // tile as plain horizontal bars instead of a boxed-in plot area.
        _chart.PrimaryXAxis.DrawGrid = false;
        _chart.PrimaryYAxis.DrawGrid = false;
        if (valueAxisFormat is not null)
            _chart.PrimaryXAxis.Format = valueAxisFormat;

        Body.Controls.Add(_chart);
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
