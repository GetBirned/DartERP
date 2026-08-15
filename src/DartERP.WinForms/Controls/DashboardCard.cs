using System.Drawing.Drawing2D;
using DartERP.WinForms.Styling;

namespace DartERP.WinForms.Controls;

/// <summary>
/// Shared chrome for every dashboard tile — a title bar over a rounded
/// card, with a padded body underneath for the tile's own content
/// (a row list, a chart, whatever). Pulled out once DashboardListCard,
/// PieChart, and BarChart all wanted the exact same title+card wrapper —
/// three copies of the same OnPaint override was the signal to share it.
/// </summary>
public abstract class DashboardCard : Panel
{
    protected Panel Body { get; }

    protected DashboardCard(string title)
    {
        Dock = DockStyle.Fill;
        BackColor = Theme.CardBackground;
        Margin = new Padding(0, 0, 16, 16);
        this.ApplyRoundedRegion(10);

        var titleLabel = new Label
        {
            Text = title,
            Font = Theme.FontBodyBold,
            ForeColor = Theme.TextPrimary,
            Dock = DockStyle.Top,
            Height = 40,
            Padding = new Padding(16, 12, 0, 0),
        };

        Body = new Panel { Dock = DockStyle.Fill, Padding = new Padding(16, 0, 16, 12) };

        Controls.Add(Body);
        Controls.Add(titleLabel);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        using var pen = new Pen(Theme.BorderColor);
        using var path = RoundedCorners.CreatePath(ClientRectangle, 10);
        e.Graphics.DrawPath(pen, path);
    }
}
