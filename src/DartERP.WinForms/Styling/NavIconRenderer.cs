using System.Drawing.Drawing2D;

namespace DartERP.WinForms.Styling;

/// <summary>
/// One small GDI+ pictogram per left-nav module, keyed by the exact module
/// name string used in MainForm.BuildModuleFactories. Same "hand-drawn,
/// no image assets" approach as ProductIconRenderer — a plain line icon per
/// screen, nothing more.
/// </summary>
public static class NavIconRenderer
{
    public static void Draw(Graphics g, string moduleName, Rectangle bounds, Color color)
    {
        g.SmoothingMode = SmoothingMode.AntiAlias;
        using var pen = new Pen(color, 1.5f);

        switch (moduleName)
        {
            case "Dashboard":
                DrawDashboard(g, pen, bounds);
                break;
            case "Customers":
                DrawPerson(g, pen, bounds);
                break;
            case "Vendors":
                DrawStorefront(g, pen, bounds);
                break;
            case "Products":
                DrawBox(g, pen, bounds);
                break;
            case "Inventory":
                DrawShelf(g, pen, bounds);
                break;
            case "Purchase Orders":
                DrawDocument(g, pen, bounds);
                break;
            case "Work Orders":
                DrawGear(g, pen, bounds);
                break;
            case "Serialized Inventory":
                DrawTag(g, pen, bounds);
                break;
            case "Quality Control":
                DrawShieldCheck(g, pen, bounds);
                break;
            case "A&D Log":
                DrawLedger(g, pen, bounds);
                break;
            case "Database":
                DrawDatabase(g, pen, bounds);
                break;
            case "Tech Glossary":
                DrawBook(g, pen, bounds);
                break;
            case "Settings":
                DrawSliders(g, pen, bounds);
                break;
            default:
                g.DrawEllipse(pen, bounds);
                break;
        }
    }

    private static void DrawDashboard(Graphics g, Pen pen, Rectangle b)
    {
        var gap = b.Width / 6;
        var cell = (b.Width - gap) / 2;
        g.DrawRectangle(pen, b.Left, b.Top, cell, cell);
        g.DrawRectangle(pen, b.Left + cell + gap, b.Top, cell, cell);
        g.DrawRectangle(pen, b.Left, b.Top + cell + gap, cell, cell);
        g.DrawRectangle(pen, b.Left + cell + gap, b.Top + cell + gap, cell, cell);
    }

    private static void DrawPerson(Graphics g, Pen pen, Rectangle b)
    {
        var headSize = b.Width * 0.42f;
        g.DrawEllipse(pen, b.Left + (b.Width - headSize) / 2, b.Top, headSize, headSize);

        var shoulderTop = b.Top + headSize + 2;
        var path = new GraphicsPath();
        path.AddArc(b.Left, shoulderTop, b.Width, b.Height, 180, 180);
        g.DrawPath(pen, path);
    }

    private static void DrawStorefront(Graphics g, Pen pen, Rectangle b)
    {
        var roofBottom = b.Top + b.Height * 0.35f;
        g.DrawLine(pen, b.Left, roofBottom, b.Left + b.Width / 2f, b.Top);
        g.DrawLine(pen, b.Left + b.Width / 2f, b.Top, b.Right, roofBottom);
        g.DrawLine(pen, b.Left, roofBottom, b.Right, roofBottom);

        var bodyRect = new RectangleF(b.Left + 1, roofBottom, b.Width - 2, b.Bottom - roofBottom);
        g.DrawRectangle(pen, bodyRect.X, bodyRect.Y, bodyRect.Width, bodyRect.Height);
        var doorX = b.Left + b.Width / 2f;
        g.DrawLine(pen, doorX, bodyRect.Bottom, doorX, bodyRect.Top + bodyRect.Height * 0.35f);
    }

    private static void DrawBox(Graphics g, Pen pen, Rectangle b)
    {
        g.DrawRectangle(pen, b);
        var flapY = b.Top + b.Height / 3;
        g.DrawLine(pen, b.Left, flapY, b.Right, flapY);
        var midX = b.Left + b.Width / 2;
        g.DrawLine(pen, midX, b.Top, midX, flapY);
    }

    private static void DrawShelf(Graphics g, Pen pen, Rectangle b)
    {
        var rowHeight = b.Height / 3f;
        for (var i = 0; i < 3; i++)
        {
            var y = b.Top + i * rowHeight;
            g.DrawLine(pen, b.Left, y, b.Right, y);
        }
        g.DrawLine(pen, b.Left, b.Top, b.Left, b.Bottom);
        g.DrawLine(pen, b.Right, b.Top, b.Right, b.Bottom);
    }

    private static void DrawDocument(Graphics g, Pen pen, Rectangle b)
    {
        var foldSize = b.Width * 0.28f;
        var docWidth = b.Width * 0.8f;
        var left = b.Left + (b.Width - docWidth) / 2;

        Point[] outline =
        [
            new Point((int)left, b.Top),
            new Point((int)(left + docWidth - foldSize), b.Top),
            new Point((int)(left + docWidth), (int)(b.Top + foldSize)),
            new Point((int)(left + docWidth), b.Bottom),
            new Point((int)left, b.Bottom),
        ];
        g.DrawPolygon(pen, outline);

        var lineInset = left + docWidth * 0.18f;
        var lineWidth = docWidth * 0.5f;
        for (var i = 1; i <= 2; i++)
        {
            var y = b.Top + foldSize + i * (b.Height - foldSize) / 4f;
            g.DrawLine(pen, lineInset, y, lineInset + lineWidth, y);
        }
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

    private static void DrawTag(Graphics g, Pen pen, Rectangle b)
    {
        var path = new GraphicsPath();
        var tipX = b.Left + b.Width * 0.35f;
        path.AddLine(tipX, b.Top, b.Right, b.Top);
        path.AddLine(b.Right, b.Top, b.Right, b.Bottom - b.Height * 0.1f);
        path.AddLine(b.Right, b.Bottom - b.Height * 0.1f, b.Left + b.Width * 0.15f, b.Bottom);
        path.AddLine(b.Left + b.Width * 0.15f, b.Bottom, b.Left, b.Top + b.Height * 0.5f);
        path.CloseFigure();
        g.DrawPath(pen, path);

        var holeSize = b.Width * 0.12f;
        g.DrawEllipse(pen, tipX + b.Width * 0.08f, b.Top + b.Height * 0.12f, holeSize, holeSize);
    }

    private static void DrawShieldCheck(Graphics g, Pen pen, Rectangle b)
    {
        var path = new GraphicsPath();
        path.AddLine(b.Left, b.Top + b.Height * 0.15f, b.Left + b.Width / 2f, b.Top);
        path.AddLine(b.Left + b.Width / 2f, b.Top, b.Right, b.Top + b.Height * 0.15f);
        path.AddLine(b.Right, b.Top + b.Height * 0.15f, b.Right, b.Top + b.Height * 0.55f);
        path.AddCurve([
            new PointF(b.Right, b.Top + b.Height * 0.55f),
            new PointF(b.Left + b.Width / 2f, b.Bottom),
            new PointF(b.Left, b.Top + b.Height * 0.55f),
        ]);
        path.AddLine(b.Left, b.Top + b.Height * 0.55f, b.Left, b.Top + b.Height * 0.15f);
        path.CloseFigure();
        g.DrawPath(pen, path);

        PointF[] check =
        [
            new PointF(b.Left + b.Width * 0.28f, b.Top + b.Height * 0.45f),
            new PointF(b.Left + b.Width * 0.44f, b.Top + b.Height * 0.62f),
            new PointF(b.Left + b.Width * 0.74f, b.Top + b.Height * 0.30f),
        ];
        g.DrawLines(pen, check);
    }

    private static void DrawLedger(Graphics g, Pen pen, Rectangle b)
    {
        g.DrawRectangle(pen, b);
        var spineX = b.Left + b.Width / 2f;
        g.DrawLine(pen, spineX, b.Top, spineX, b.Bottom);

        var lineWidth = b.Width * 0.3f;
        for (var i = 0; i < 2; i++)
        {
            var y = b.Top + b.Height * (0.3f + i * 0.35f);
            g.DrawLine(pen, b.Left + b.Width * 0.12f, y, b.Left + b.Width * 0.12f + lineWidth, y);
            g.DrawLine(pen, spineX + b.Width * 0.08f, y, spineX + b.Width * 0.08f + lineWidth, y);
        }
    }

    private static void DrawDatabase(Graphics g, Pen pen, Rectangle b)
    {
        var capHeight = b.Height * 0.32f;
        var sideTop = b.Top + capHeight / 2f;
        var sideBottom = b.Bottom - capHeight / 2f;

        g.DrawEllipse(pen, b.Left, b.Top, b.Width, capHeight);
        g.DrawLine(pen, b.Left, sideTop, b.Left, sideBottom);
        g.DrawLine(pen, b.Right, sideTop, b.Right, sideBottom);
        g.DrawArc(pen, b.Left, b.Bottom - capHeight, b.Width, capHeight, 0, 180);

        var midY = sideTop + (sideBottom - sideTop) / 2f - capHeight / 2f;
        g.DrawArc(pen, b.Left, midY, b.Width, capHeight, 0, 180);
    }

    private static void DrawBook(Graphics g, Pen pen, Rectangle b)
    {
        var centerX = b.Left + b.Width / 2f;
        var top = b.Top + b.Height * 0.15f;
        var bottom = b.Bottom - b.Height * 0.1f;

        using var leftPage = new GraphicsPath();
        leftPage.AddBezier(
            new PointF(centerX, top),
            new PointF(b.Left, top),
            new PointF(b.Left, bottom),
            new PointF(centerX, bottom));
        g.DrawPath(pen, leftPage);

        using var rightPage = new GraphicsPath();
        rightPage.AddBezier(
            new PointF(centerX, top),
            new PointF(b.Right, top),
            new PointF(b.Right, bottom),
            new PointF(centerX, bottom));
        g.DrawPath(pen, rightPage);

        g.DrawLine(pen, centerX, top, centerX, bottom);
    }

    private static void DrawSliders(Graphics g, Pen pen, Rectangle b)
    {
        var rowHeight = b.Height / 3f;
        float[] knobPositions = [0.65f, 0.35f, 0.55f];

        for (var i = 0; i < 3; i++)
        {
            var y = b.Top + rowHeight * (i + 0.5f);
            g.DrawLine(pen, b.Left, y, b.Right, y);

            var knobX = b.Left + b.Width * knobPositions[i];
            var knobRadius = b.Height * 0.09f;
            using var brush = new SolidBrush(pen.Color);
            g.FillEllipse(brush, knobX - knobRadius, y - knobRadius, knobRadius * 2, knobRadius * 2);
        }
    }
}
