using System.Drawing.Drawing2D;

namespace DartERP.WinForms.Styling;

/// <summary>
/// Shared rounded-rectangle helpers. WinForms has no border-radius, so
/// "rounded" here means: build a GraphicsPath and either paint with it
/// (StatusBadge does this for its pill) or clip the control's own Region
/// to it so children actually get cut off at the rounded edge instead of
/// just having a rounded border painted over square corners.
/// </summary>
public static class RoundedCorners
{
    public static GraphicsPath CreatePath(Rectangle bounds, int radius)
    {
        var path = new GraphicsPath();
        var diameter = radius * 2;
        bounds.Width -= 1;
        bounds.Height -= 1;

        path.AddArc(bounds.X, bounds.Y, diameter, diameter, 180, 90);
        path.AddArc(bounds.Right - diameter, bounds.Y, diameter, diameter, 270, 90);
        path.AddArc(bounds.Right - diameter, bounds.Bottom - diameter, diameter, diameter, 0, 90);
        path.AddArc(bounds.X, bounds.Bottom - diameter, diameter, diameter, 90, 90);
        path.CloseFigure();
        return path;
    }

    /// <summary>
    /// Clips a control's region to a rounded rectangle and keeps it in sync
    /// as the control resizes (needed for anything sized by the docking
    /// layout engine rather than a fixed Width/Height at construction).
    /// </summary>
    public static T ApplyRoundedRegion<T>(this T control, int radius) where T : Control
    {
        void UpdateRegion()
        {
            if (control.Width <= 0 || control.Height <= 0)
                return;

            using var path = CreatePath(new Rectangle(0, 0, control.Width, control.Height), radius);
            control.Region?.Dispose();
            control.Region = new Region(path);
        }

        UpdateRegion();
        control.Resize += (_, _) => UpdateRegion();
        return control;
    }
}
