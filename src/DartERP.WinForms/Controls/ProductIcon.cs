using DartERP.Core.Enums;
using DartERP.WinForms.Styling;

namespace DartERP.WinForms.Controls;

/// <summary>Small category pictogram for standalone use (e.g. next to the category picker on the product edit form).</summary>
public class ProductIcon : Control
{
    private ProductCategory _category;

    public ProductIcon(ProductCategory category)
    {
        _category = category;
        Size = new Size(22, 22);
        DoubleBuffered = true;
    }

    public ProductCategory Category
    {
        get => _category;
        set { _category = value; Invalidate(); }
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);

        // Always draw a square glyph, right-anchored, even if the control
        // itself is wider than tall (used as left-padding when this sits
        // next to another control in a Dock=Right layout).
        var side = Height - 5;
        var bounds = new Rectangle(Width - side - 2, 2, side, side);
        ProductIconRenderer.Draw(e.Graphics, _category, bounds, Theme.TextSecondary);
    }
}
