using System.Runtime.InteropServices;

namespace DartERP.WinForms.Controls;

/// <summary>
/// A Label with letter-spacing (tracking) — WinForms has no such property
/// on its own, Label/TextRenderer/DrawString all just don't expose one.
/// This uses the native GDI SetTextCharacterExtra API instead, which is the
/// one place that knob actually exists. Reserved for header-role text: a
/// little extra tracking on a bigger, bolder weight is what makes Inter
/// read as its own typeface at a glance instead of "close enough to Segoe
/// UI you can't tell the difference."
/// </summary>
public class LetterSpacedLabel : Label
{
    [DllImport("gdi32.dll")]
    private static extern int SetTextCharacterExtra(IntPtr hdc, int nCharExtra);

    [DllImport("gdi32.dll")]
    private static extern IntPtr SelectObject(IntPtr hdc, IntPtr hObject);

    [DllImport("gdi32.dll")]
    private static extern bool DeleteObject(IntPtr hObject);

    [DllImport("gdi32.dll")]
    private static extern int SetTextColor(IntPtr hdc, int crColor);

    [DllImport("gdi32.dll")]
    private static extern int SetBkMode(IntPtr hdc, int mode);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern int DrawText(IntPtr hdc, string text, int count, ref RECT rect, uint format);

    [StructLayout(LayoutKind.Sequential)]
    private struct RECT
    {
        public int Left, Top, Right, Bottom;
    }

    private const int Transparent = 1;
    private const uint DtLeft = 0x0;
    private const uint DtCenter = 0x1;
    private const uint DtRight = 0x2;
    private const uint DtVCenter = 0x4;
    private const uint DtSingleLine = 0x20;
    private const uint DtNoPrefix = 0x800;

    public int LetterSpacing { get; set; } = 1;

    public LetterSpacedLabel()
    {
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, true);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        e.Graphics.Clear(BackColor);

        // GetHdc() hands off the drawing surface to raw GDI for a moment —
        // nothing else on this Graphics can run until ReleaseHdc(), and the
        // HFONT/old-font-handle dance below is just how GDI selects a font
        // into a device context (there's no "just pass a Font" overload at
        // this level, unlike GDI+).
        var hFont = Font.ToHfont();
        var hdc = e.Graphics.GetHdc();
        try
        {
            var oldFont = SelectObject(hdc, hFont);
            SetTextCharacterExtra(hdc, LetterSpacing);
            SetBkMode(hdc, Transparent);
            SetTextColor(hdc, ColorTranslator.ToWin32(ForeColor));

            var rect = new RECT { Left = 0, Top = 0, Right = ClientSize.Width, Bottom = ClientSize.Height };
            var flags = DtSingleLine | DtNoPrefix | DtVCenter | AlignmentFlag(TextAlign);
            DrawText(hdc, Text, Text.Length, ref rect, flags);

            SelectObject(hdc, oldFont);
        }
        finally
        {
            e.Graphics.ReleaseHdc(hdc);
            DeleteObject(hFont);
        }
    }

    private static uint AlignmentFlag(ContentAlignment align) => align switch
    {
        ContentAlignment.MiddleCenter or ContentAlignment.TopCenter or ContentAlignment.BottomCenter => DtCenter,
        ContentAlignment.MiddleRight or ContentAlignment.TopRight or ContentAlignment.BottomRight => DtRight,
        _ => DtLeft,
    };
}
