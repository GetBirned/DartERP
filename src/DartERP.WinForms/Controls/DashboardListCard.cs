using DartERP.WinForms.Styling;

namespace DartERP.WinForms.Controls;

public record DashboardListRow(string Primary, string Secondary, Color? AccentColor = null);

/// <summary>
/// Compact "attention needed" card for the dashboard: a title and a short
/// stacked list of rows (e.g. recent POs, low-stock products).
/// </summary>
public class DashboardListCard : Panel
{
    private readonly Panel _rowsPanel;
    private readonly string _emptyMessage;

    public DashboardListCard(string title, string emptyMessage)
    {
        _emptyMessage = emptyMessage;
        Dock = DockStyle.Fill;
        BackColor = Theme.CardBackground;
        Margin = new Padding(0, 0, 16, 16);

        var titleLabel = new Label
        {
            Text = title,
            Font = Theme.FontBodyBold,
            ForeColor = Theme.TextPrimary,
            Dock = DockStyle.Top,
            Height = 40,
            Padding = new Padding(16, 12, 0, 0),
        };

        _rowsPanel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(16, 0, 16, 12) };

        Controls.Add(_rowsPanel);
        Controls.Add(titleLabel);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        using var pen = new Pen(Theme.BorderColor);
        e.Graphics.DrawRectangle(pen, 0, 0, Width - 1, Height - 1);
    }

    public void SetRows(IReadOnlyList<DashboardListRow> rows)
    {
        _rowsPanel.Controls.Clear();

        if (rows.Count == 0)
        {
            _rowsPanel.Controls.Add(new Label
            {
                Text = _emptyMessage,
                Font = Theme.FontSmall,
                ForeColor = Theme.TextSecondary,
                Dock = DockStyle.Top,
                Height = 28,
            });
            return;
        }

        // Add bottom-up since each row docks to Top.
        for (var i = rows.Count - 1; i >= 0; i--)
        {
            var row = rows[i];
            var rowPanel = new Panel { Dock = DockStyle.Top, Height = 30 };

            var primaryLabel = new Label
            {
                Text = row.Primary,
                Font = Theme.FontSmall,
                ForeColor = Theme.TextPrimary,
                Dock = DockStyle.Left,
                AutoSize = false,
                Width = 190,
                TextAlign = ContentAlignment.MiddleLeft,
            };

            var secondaryLabel = new Label
            {
                Text = row.Secondary,
                Font = Theme.FontSmall,
                ForeColor = row.AccentColor ?? Theme.TextSecondary,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleRight,
            };

            rowPanel.Controls.Add(secondaryLabel);
            rowPanel.Controls.Add(primaryLabel);
            _rowsPanel.Controls.Add(rowPanel);
        }
    }
}
