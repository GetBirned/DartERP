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
    private readonly ToolTip _toolTip = new();
    private int _lastToolTipIndex = -1;

    public SfPieChartCard(string title) : base(title)
    {
        _chart = new ChartControl
        {
            Dock = DockStyle.Fill,
            BackColor = Theme.CardBackground,
            ShowLegend = true,
            // Syncfusion's own tooltip display pipeline never reliably
            // reflects what's set through Styles[i].ToolTip or
            // ChartRegion.ToolTip on this package version, so ShowToolTips
            // stays off and a plain WinForms ToolTip is driven by hand
            // instead — see the ChartRegionMouseEnter handler below.
            ShowToolTips = false,
        };
        _chart.ChartArea.BorderWidth = 0;
        _chart.ChartArea.BackInterior = new BrushInfo(Theme.CardBackground);
        _chart.ChartArea.GridBackInterior = new BrushInfo(Theme.CardBackground);
        // Bottom placement put the legend directly under the donut, and no
        // combination of margin/shadow settings stopped the donut's own
        // bottom edge from visually bleeding into that legend row — nothing
        // in ChartArea's layout API actually reserves clear space between
        // a Pie series and a Bottom-docked legend the way it does for
        // axis-based series. Right sidesteps the problem entirely by never
        // putting them in the same vertical space, and the dashboard's
        // chart column is wide enough now (after the layout pass that fixed
        // the KPI grid) for this to actually fit without truncating text.
        _chart.Legend.Position = ChartDock.Right;
        // Default RepresentationType (SeriesType) draws a mini pie-slice icon
        // per legend entry and labels it with the point's value instead of
        // its category — Rectangle gives a plain color swatch next to the
        // actual status name, which is what a legend should show.
        _chart.Legend.RepresentationType = ChartLegendRepresentationType.Rectangle;
        _chart.Legend.Font = Theme.FontSmall;
        _chart.Legend.TextColor = Theme.TextPrimary;
        _chart.Legend.BackInterior = new BrushInfo(Theme.CardBackground);
        _chart.Legend.ShowBorder = false;

        // ChartSeries.Styles[i].ToolTip looked like the "right" API for
        // per-point tooltip text, but it doesn't reliably key by the
        // point's actual insertion index once you start moving the mouse
        // across a real, rendered chart — it resolved to the hovered
        // category's alphabetical rank instead (confirmed by hovering
        // every slice with a self-identifying tooltip and cross-
        // referencing). ChartRegion.ToolTip (set from
        // ChartRegionMouseEnter) sidesteps that specific bug — its
        // PointIndex is computed fresh from mouse position, not looked up
        // through the Styles array — but ShowToolTips' own display
        // pipeline turned out not to read it at all (same as the bar
        // chart). So this drives a plain WinForms ToolTip by hand instead,
        // using PointIndex as the one piece of Syncfusion data that's
        // actually trustworthy.
        _chart.ChartRegionMouseEnter += (_, e) =>
        {
            if (e.Region.Type == ChartRegionType.SeriesPoint && e.Region.PointIndex >= 0 && e.Region.PointIndex < _visible.Count)
            {
                if (_lastToolTipIndex == e.Region.PointIndex)
                    return;
                _lastToolTipIndex = e.Region.PointIndex;
                var slice = _visible[e.Region.PointIndex];
                _toolTip.Show($"{slice.Label}: {slice.Value:0}", _chart, e.Point.X + 16, e.Point.Y + 16, 4000);
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

        Body.Controls.Add(_chart);
    }

    private List<PieSlice> _visible = new();

    public void SetData(IReadOnlyList<PieSlice> slices)
    {
        _visible = slices.Where(s => s.Value > 0).ToList();

        _chart.Series.Clear();
        var series = new ChartSeries("Status", ChartSeriesType.Pie);
        foreach (var slice in _visible)
            series.Points.Add(slice.Label, (double)slice.Value);
        _chart.Series.Add(series);

        for (var i = 0; i < _visible.Count; i++)
        {
            series.Styles[i].Interior = new BrushInfo(_visible[i].Color);
            // Points.Add(string, double) stores the label as the point's
            // Category, but the legend reads its text from here instead —
            // without this it shows each slice's raw value ("1", "2"...)
            // rather than the status name.
            series.Styles[i].Text = _visible[i].Label;
            // Legend.TextColor (set once, above) turned out to only affect
            // the legend title, not each entry's label — same "shared
            // property doesn't actually drive rendering" trap as
            // CellStyleInfo.BackColor. Each legend entry's text color
            // actually comes from its point's own style.
            series.Styles[i].TextColor = Theme.TextPrimary;
            // Off by default this would drop each slice's shadow straight
            // down onto whatever's below the donut — with the legend
            // docked directly underneath, that showed up as a stray colored
            // crescent bleeding into the legend row.
            series.Styles[i].DisplayShadow = false;
        }
    }
}
