using System.Drawing.Drawing2D;
using DartERP.WinForms.Styling;

namespace DartERP.WinForms.Controls;

/// <summary>
/// Hand-drawn box diagram of this app's layer dependencies — WinForms depends
/// on Application and Infrastructure, both of which depend only on Core, and
/// Core depends on nothing. Same GDI+-only approach as PieChart/BarChart, no
/// diagramming library, so it themes correctly with the light/dark toggle.
/// </summary>
public class LayeredArchitectureDiagram : Panel
{
    public LayeredArchitectureDiagram()
    {
        Height = 270;
        Dock = DockStyle.Top;
        BackColor = Theme.CardBackground;
        DoubleBuffered = true;
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;

        const int boxHeight = 48;
        const int topY = 14;
        const int rowGap = 46;
        var midY = topY + boxHeight + rowGap;
        var bottomY = midY + boxHeight + rowGap;
        var centerX = Width / 2f;

        var winFormsBox = new Rectangle((int)(centerX - 110), topY, 220, boxHeight);
        var appBox = new Rectangle((int)(centerX - 190), midY, 170, boxHeight);
        var infraBox = new Rectangle((int)(centerX + 20), midY, 170, boxHeight);
        var coreBox = new Rectangle((int)(centerX - 110), bottomY, 220, boxHeight);

        DrawArrow(g, new PointF(appBox.Left + appBox.Width / 2f, winFormsBox.Bottom), new PointF(appBox.Left + appBox.Width / 2f, appBox.Top));
        DrawArrow(g, new PointF(infraBox.Left + infraBox.Width / 2f, winFormsBox.Bottom), new PointF(infraBox.Left + infraBox.Width / 2f, infraBox.Top));
        DrawArrow(g, new PointF(appBox.Left + appBox.Width / 2f, appBox.Bottom), new PointF(coreBox.Left + coreBox.Width * 0.3f, coreBox.Top));
        DrawArrow(g, new PointF(infraBox.Left + infraBox.Width / 2f, infraBox.Bottom), new PointF(coreBox.Left + coreBox.Width * 0.7f, coreBox.Top));

        DrawBox(g, winFormsBox, "WinForms", "Presentation");
        DrawBox(g, appBox, "Application", "Services · Validation");
        DrawBox(g, infraBox, "Infrastructure", "EF Core · Repositories");
        DrawBox(g, coreBox, "Core", "Models · Interfaces · zero deps");
    }

    private static void DrawBox(Graphics g, Rectangle rect, string title, string subtitle)
    {
        using var path = RoundedCorners.CreatePath(rect, 8);
        using var fillBrush = new SolidBrush(Theme.AppBackground);
        using var borderPen = new Pen(Theme.AccentPrimary, 1.5f);
        using var titleBrush = new SolidBrush(Theme.TextPrimary);
        using var subtitleBrush = new SolidBrush(Theme.TextSecondary);
        using var format = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };

        g.FillPath(fillBrush, path);
        g.DrawPath(borderPen, path);

        var titleRect = new RectangleF(rect.X, rect.Y + 4, rect.Width, rect.Height * 0.5f);
        var subtitleRect = new RectangleF(rect.X, rect.Y + rect.Height * 0.5f, rect.Width, rect.Height * 0.46f);
        g.DrawString(title, Theme.FontBodyBold, titleBrush, titleRect, format);
        g.DrawString(subtitle, Theme.FontSmall, subtitleBrush, subtitleRect, format);
    }

    private static void DrawArrow(Graphics g, PointF from, PointF to)
    {
        using var pen = new Pen(Theme.TextSecondary, 1.5f) { CustomEndCap = new AdjustableArrowCap(4, 6) };
        g.DrawLine(pen, from, to);
    }
}
