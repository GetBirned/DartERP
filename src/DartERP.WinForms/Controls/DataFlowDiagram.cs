using System.Drawing.Drawing2D;
using DartERP.WinForms.Styling;

namespace DartERP.WinForms.Controls;

/// <summary>
/// Hand-drawn pipeline showing how a single call actually travels through
/// this app's layers, left to right — the concrete mechanism behind the
/// repository pattern, not just the abstract layer boxes. Same GDI+-only
/// approach as LayeredArchitectureDiagram.
/// </summary>
public class DataFlowDiagram : Panel
{
    private static readonly (string Title, string Subtitle)[] Steps =
    [
        ("WinForms UI", "Forms & Controls"),
        ("Application Service", "Business rules"),
        ("Repository", "IRepository<T>"),
        ("EF Core DbContext", "Change tracking"),
        ("SQL Server", "LocalDB"),
    ];

    public DataFlowDiagram()
    {
        Height = 130;
        Dock = DockStyle.Top;
        BackColor = Theme.CardBackground;
        DoubleBuffered = true;
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;

        const int boxHeight = 64;
        const int arrowGap = 30;
        const int margin = 12;

        var boxWidth = (Width - margin * 2 - arrowGap * (Steps.Length - 1)) / Steps.Length;
        var y = (Height - boxHeight) / 2;
        var x = margin;

        var boxes = new Rectangle[Steps.Length];
        for (var i = 0; i < Steps.Length; i++)
        {
            boxes[i] = new Rectangle(x, y, boxWidth, boxHeight);
            x += boxWidth + arrowGap;
        }

        for (var i = 0; i < boxes.Length - 1; i++)
        {
            var centerY = boxes[i].Top + boxes[i].Height / 2f;
            DrawArrow(g, new PointF(boxes[i].Right, centerY), new PointF(boxes[i + 1].Left, centerY));
        }

        for (var i = 0; i < boxes.Length; i++)
            DrawBox(g, boxes[i], Steps[i].Title, Steps[i].Subtitle);
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

        var titleRect = new RectangleF(rect.X + 2, rect.Y + 4, rect.Width - 4, rect.Height * 0.5f);
        var subtitleRect = new RectangleF(rect.X + 2, rect.Y + rect.Height * 0.5f, rect.Width - 4, rect.Height * 0.46f);
        g.DrawString(title, Theme.FontSmall, titleBrush, titleRect, format);
        g.DrawString(subtitle, Theme.FontSmall, subtitleBrush, subtitleRect, format);
    }

    private static void DrawArrow(Graphics g, PointF from, PointF to)
    {
        using var pen = new Pen(Theme.TextSecondary, 1.5f) { CustomEndCap = new AdjustableArrowCap(4, 6) };
        g.DrawLine(pen, from, to);
    }
}
