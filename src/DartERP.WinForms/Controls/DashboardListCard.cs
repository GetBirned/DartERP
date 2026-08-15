using DartERP.WinForms.Styling;

namespace DartERP.WinForms.Controls;

public record DashboardListRow(string Primary, string Secondary, Color? AccentColor = null);

/// <summary>
/// Compact "attention needed" card for the dashboard: a title and a short
/// stacked list of rows (e.g. recent POs, low-stock products).
/// </summary>
public class DashboardListCard : DashboardCard
{
    private readonly string _emptyMessage;

    public DashboardListCard(string title, string emptyMessage) : base(title)
    {
        _emptyMessage = emptyMessage;
    }

    public void SetRows(IReadOnlyList<DashboardListRow> rows)
    {
        Body.Controls.Clear();

        if (rows.Count == 0)
        {
            Body.Controls.Add(new Label
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
                Width = 210,
                AutoEllipsis = true,
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
            Body.Controls.Add(rowPanel);
        }
    }
}
