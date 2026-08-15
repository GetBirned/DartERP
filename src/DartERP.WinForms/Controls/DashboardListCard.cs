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
    private readonly bool _stacked;

    /// <summary>
    /// Side-by-side (title | value) fits the dashboard's wide tiles, where
    /// row content is a short name plus a dollar amount. The status-history
    /// panels on the PO/Work Order edit forms are much narrower and their
    /// primary text ("Draft → Submitted") and secondary text (who/when) are
    /// both variable-length, so those pass stacked: true to get two full-width
    /// lines instead of a cramped two-column split.
    /// </summary>
    public DashboardListCard(string title, string emptyMessage, bool stacked = false) : base(title)
    {
        _emptyMessage = emptyMessage;
        _stacked = stacked;
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

            if (_stacked)
            {
                var rowPanel = new Panel { Dock = DockStyle.Top, Height = 42, Padding = new Padding(0, 0, 0, 6) };

                var secondaryLabel = new Label
                {
                    Text = row.Secondary,
                    Font = Theme.FontSmall,
                    ForeColor = row.AccentColor ?? Theme.TextSecondary,
                    Dock = DockStyle.Top,
                    Height = 18,
                    AutoEllipsis = true,
                    TextAlign = ContentAlignment.MiddleLeft,
                };

                var primaryLabel = new Label
                {
                    Text = row.Primary,
                    Font = Theme.FontSmall,
                    ForeColor = Theme.TextPrimary,
                    Dock = DockStyle.Top,
                    Height = 18,
                    AutoEllipsis = true,
                    TextAlign = ContentAlignment.MiddleLeft,
                };

                rowPanel.Controls.Add(secondaryLabel);
                rowPanel.Controls.Add(primaryLabel);
                Body.Controls.Add(rowPanel);
            }
            else
            {
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
}
