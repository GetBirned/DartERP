using DartERP.WinForms.Styling;

namespace DartERP.WinForms.Controls;

/// <summary>
/// Left-nav sidebar item. Owner-drawn so we get a clean selected/hover
/// state without fighting the standard WinForms Button chrome.
/// </summary>
public class NavButton : Panel
{
    private const int IconSize = 18;

    private bool _isSelected;
    private bool _isHovered;

    public event EventHandler? NavClicked;

    public NavButton(string text)
    {
        Text = text;
        Height = 44;
        Dock = DockStyle.Top;
        Cursor = Cursors.Hand;
        DoubleBuffered = true;

        Click += (_, _) => NavClicked?.Invoke(this, EventArgs.Empty);
        MouseEnter += (_, _) => { _isHovered = true; Invalidate(); };
        MouseLeave += (_, _) => { _isHovered = false; Invalidate(); };
    }

    public new string Text { get; }

    public bool IsSelected
    {
        get => _isSelected;
        set
        {
            _isSelected = value;
            Invalidate();
        }
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var background = _isSelected ? Theme.SidebarSelected : (_isHovered ? Theme.SidebarHover : Theme.SidebarBackground);
        e.Graphics.Clear(background);

        if (_isSelected)
        {
            using var accentBrush = new SolidBrush(Theme.SidebarTextSelected);
            e.Graphics.FillRectangle(accentBrush, 0, 0, 4, Height);
        }

        var iconColor = _isSelected ? Theme.SidebarTextSelected : Theme.SidebarText;
        var iconBounds = new Rectangle(24, (Height - IconSize) / 2, IconSize, IconSize);
        NavIconRenderer.Draw(e.Graphics, Text, iconBounds, iconColor);

        var textRect = new Rectangle(iconBounds.Right + 12, 0, Width - iconBounds.Right - 12, Height);
        // NoPrefix: without it, "&" in a nav label (e.g. "A&D Log") is
        // treated as a mnemonic-accelerator prefix and silently stripped,
        // underlining the next character instead of just displaying "&".
        TextRenderer.DrawText(e.Graphics, Text, Theme.FontNav, textRect, iconColor,
            TextFormatFlags.VerticalCenter | TextFormatFlags.Left | TextFormatFlags.NoPrefix);
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);
        OnClick(EventArgs.Empty);
    }
}
