using DartERP.WinForms.Styling;
using Syncfusion.Drawing;
using Syncfusion.Windows.Forms.Chart;

namespace DartERP.WinForms.Controls;

public record PieSlice(string Label, float Value, Color Color);

/// <summary>
/// Donut/pie tile built on Syncfusion's ChartControl instead of the GDI+
/// FillPie/DrawPie I used before — this is the "grid/chart components" ask
/// from the interview listing put to actual use. Per-slice color still
/// comes from StatusColors.For(...) via ChartSeries.Styles[i].Interior, so
/// the chart's colors stay in sync with the status colors used everywhere
/// else in the app (the badges on the Purchase Orders list, for instance).
/// </summary>
public class SfPieChartCard : DashboardCard
{
    private readonly ChartControl _chart;

    public SfPieChartCard(string title) : base(title)
    {
        _chart = new ChartControl
        {
            Dock = DockStyle.Fill,
            BackColor = Theme.CardBackground,
            ShowLegend = true,
        };
        _chart.ChartArea.BorderWidth = 0;
        _chart.ChartArea.BackInterior = new BrushInfo(Theme.CardBackground);
        _chart.ChartArea.GridBackInterior = new BrushInfo(Theme.CardBackground);
        // Right (the default) pushed the legend text past the edge of this
        // narrow dashboard tile — Bottom lays the swatches out in a row
        // underneath the donut instead, which actually fits.
        _chart.Legend.Position = ChartDock.Bottom;
        // Default RepresentationType (SeriesType) draws a mini pie-slice icon
        // per legend entry and labels it with the point's value instead of
        // its category — Rectangle gives a plain color swatch next to the
        // actual status name, which is what a legend should show.
        _chart.Legend.RepresentationType = ChartLegendRepresentationType.Rectangle;
        _chart.Legend.Font = Theme.FontSmall;
        _chart.Legend.TextColor = Theme.TextPrimary;
        _chart.Legend.BackInterior = new BrushInfo(Theme.CardBackground);
        _chart.Legend.ShowBorder = false;

        Body.Controls.Add(_chart);
    }

    public void SetData(IReadOnlyList<PieSlice> slices)
    {
        var visible = slices.Where(s => s.Value > 0).ToList();

        _chart.Series.Clear();
        var series = new ChartSeries("Status", ChartSeriesType.Pie);
        foreach (var slice in visible)
            series.Points.Add(slice.Label, (double)slice.Value);
        _chart.Series.Add(series);

        for (var i = 0; i < visible.Count; i++)
        {
            series.Styles[i].Interior = new BrushInfo(visible[i].Color);
            // Points.Add(string, double) stores the label as the point's
            // Category, but the legend reads its text from here instead —
            // without this it shows each slice's raw value ("1", "2"...)
            // rather than the status name.
            series.Styles[i].Text = visible[i].Label;
            // Legend.TextColor (set once, above) turned out to only affect
            // the legend title, not each entry's label — same "shared
            // property doesn't actually drive rendering" trap as
            // CellStyleInfo.BackColor. Each legend entry's text color
            // actually comes from its point's own style.
            series.Styles[i].TextColor = Theme.TextPrimary;
        }
    }
}
