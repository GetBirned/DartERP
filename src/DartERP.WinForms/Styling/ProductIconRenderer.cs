using System.Drawing.Drawing2D;
using DartERP.Core.Enums;

namespace DartERP.WinForms.Styling;

/// <summary>
/// Draws a small, deliberately generic pictogram per product category —
/// a diamond for raw material, a gear for components, a box for packaging,
/// a checkmark badge for finished goods. These are plain inventory-category
/// symbols, not depictions of any specific product: nothing here references
/// what a "finished product" actually is, consistent with this project's
/// scope (business records only, no product design/engineering content).
/// </summary>
public static class ProductIconRenderer
{
    public static void Draw(Graphics g, ProductCategory category, Rectangle bounds, Color color)
    {
        g.SmoothingMode = SmoothingMode.AntiAlias;
        using var pen = new Pen(color, 1.6f);

        switch (category)
        {
            case ProductCategory.RawMaterial:
                DrawDiamond(g, pen, bounds);
                break;
            case ProductCategory.Component:
                DrawGear(g, pen, bounds);
                break;
            case ProductCategory.Packaging:
                DrawBox(g, pen, bounds);
                break;
            case ProductCategory.FinishedProduct:
                DrawFinishedBadge(g, pen, bounds);
                break;
            default:
                g.DrawEllipse(pen, bounds);
                break;
        }
    }

    private static void DrawDiamond(Graphics g, Pen pen, Rectangle b)
    {
        Point[] points =
        [
            new Point(b.Left + b.Width / 2, b.Top),
            new Point(b.Right, b.Top + b.Height / 2),
            new Point(b.Left + b.Width / 2, b.Bottom),
            new Point(b.Left, b.Top + b.Height / 2),
        ];
        g.DrawPolygon(pen, points);
    }

    private static void DrawGear(Graphics g, Pen pen, Rectangle b)
    {
        var center = new PointF(b.Left + b.Width / 2f, b.Top + b.Height / 2f);
        var outerRadius = b.Width / 2f - 2;
        var innerRadius = outerRadius * 0.45f;

        g.DrawEllipse(pen, center.X - outerRadius, center.Y - outerRadius, outerRadius * 2, outerRadius * 2);
        g.DrawEllipse(pen, center.X - innerRadius, center.Y - innerRadius, innerRadius * 2, innerRadius * 2);

        for (var i = 0; i < 6; i++)
        {
            var angle = i * (Math.PI / 3);
            var x1 = center.X + (float)Math.Cos(angle) * outerRadius;
            var y1 = center.Y + (float)Math.Sin(angle) * outerRadius;
            var x2 = center.X + (float)Math.Cos(angle) * (outerRadius + 2.5f);
            var y2 = center.Y + (float)Math.Sin(angle) * (outerRadius + 2.5f);
            g.DrawLine(pen, x1, y1, x2, y2);
        }
    }

    private static void DrawBox(Graphics g, Pen pen, Rectangle b)
    {
        g.DrawRectangle(pen, b);
        var flapY = b.Top + b.Height / 3;
        g.DrawLine(pen, b.Left, flapY, b.Right, flapY);
        var midX = b.Left + b.Width / 2;
        g.DrawLine(pen, midX, b.Top, midX, flapY);
    }

    private static void DrawFinishedBadge(Graphics g, Pen pen, Rectangle b)
    {
        g.DrawEllipse(pen, b);
        PointF[] check =
        [
            new PointF(b.Left + b.Width * 0.26f, b.Top + b.Height * 0.52f),
            new PointF(b.Left + b.Width * 0.44f, b.Top + b.Height * 0.70f),
            new PointF(b.Left + b.Width * 0.76f, b.Top + b.Height * 0.30f),
        ];
        g.DrawLines(pen, check);
    }
}
