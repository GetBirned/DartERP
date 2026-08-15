using System.Drawing.Drawing2D;
using DartERP.WinForms.Styling;

namespace DartERP.WinForms.Controls;

/// <summary>
/// A password field with a reveal toggle — WinForms TextBox has no
/// built-in affordance for this. The eye glyph is hand-drawn via GDI+
/// rather than an emoji/image, same as every other icon in this app
/// (NavIconRenderer, ProductIconRenderer): a Unicode eye character
/// renders inconsistently across fonts and would look out of place next
/// to the app's own line-icon style.
///
/// Wraps a borderless inner TextBox in a bordered host Panel — the outer
/// Panel supplies the FixedSingle border that a real TextBox would have,
/// so the eye button reads as part of the same field instead of a
/// separate control bolted on next to it.
/// </summary>
public class PasswordTextBox : Panel
{
    private readonly Panel _eyeButton;
    private bool _revealed;

    public TextBox Input { get; } = new()
    {
        BorderStyle = BorderStyle.None,
        UseSystemPasswordChar = true,
        Dock = DockStyle.Fill,
        Font = Theme.FontBody,
        BackColor = Theme.CardBackground,
        ForeColor = Theme.TextPrimary,
    };

    public new string Text
    {
        get => Input.Text;
        set => Input.Text = value;
    }

    public PasswordTextBox()
    {
        BorderStyle = BorderStyle.FixedSingle;
        BackColor = Theme.CardBackground;
        Height = 30;

        _eyeButton = new Panel { Dock = DockStyle.Right, Width = 28, Cursor = Cursors.Hand };
        _eyeButton.Paint += EyeButton_Paint;
        _eyeButton.Click += (_, _) => ToggleReveal();

        Controls.Add(Input);
        Controls.Add(_eyeButton);
    }

    public void Clear() => Input.Clear();

    private void ToggleReveal()
    {
        _revealed = !_revealed;
        Input.UseSystemPasswordChar = !_revealed;
        _eyeButton.Invalidate();
    }

    private void EyeButton_Paint(object? sender, PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        using var pen = new Pen(Theme.TextSecondary, 1.3f);

        var cx = _eyeButton.Width / 2f;
        var cy = _eyeButton.Height / 2f;
        var rect = new RectangleF(cx - 8f, cy - 5f, 16f, 10f);

        g.DrawEllipse(pen, rect);

        const float pupilSize = 4f;
        using var pupilBrush = new SolidBrush(Theme.TextSecondary);
        g.FillEllipse(pupilBrush, cx - pupilSize / 2, cy - pupilSize / 2, pupilSize, pupilSize);

        // Hidden (the default) gets a strike-through — the familiar
        // "eye with a slash" for a password that's currently masked.
        if (!_revealed)
            g.DrawLine(pen, rect.Left - 2, rect.Bottom + 2, rect.Right + 2, rect.Top - 2);
    }
}
