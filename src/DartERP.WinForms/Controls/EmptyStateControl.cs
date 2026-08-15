using DartERP.WinForms.Styling;

namespace DartERP.WinForms.Controls;

/// <summary>
/// Centered "nothing here yet" placeholder shown instead of an empty grid.
/// </summary>
public class EmptyStateControl : Panel
{
    private readonly LetterSpacedLabel _titleLabel;
    private readonly Label _subtitleLabel;

    public EmptyStateControl(string title, string subtitle)
    {
        Dock = DockStyle.Fill;
        BackColor = Theme.CardBackground;

        _titleLabel = new LetterSpacedLabel
        {
            Text = title,
            Font = Theme.FontSubheader,
            ForeColor = Theme.TextPrimary,
            AutoSize = false,
            TextAlign = ContentAlignment.MiddleCenter,
            Dock = DockStyle.Top,
            Height = 32,
        };

        _subtitleLabel = new Label
        {
            Text = subtitle,
            Font = Theme.FontBody,
            ForeColor = Theme.TextSecondary,
            AutoSize = false,
            TextAlign = ContentAlignment.MiddleCenter,
            Dock = DockStyle.Top,
            Height = 24,
        };

        var spacer = new Panel { Dock = DockStyle.Top, Height = 40 };

        Controls.Add(_subtitleLabel);
        Controls.Add(_titleLabel);
        Controls.Add(spacer);
    }

    public string Title
    {
        get => _titleLabel.Text;
        set => _titleLabel.Text = value;
    }

    public string Subtitle
    {
        get => _subtitleLabel.Text;
        set => _subtitleLabel.Text = value;
    }
}
