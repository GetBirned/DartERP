using DartERP.Core.Models;
using DartERP.WinForms.Styling;

namespace DartERP.WinForms.Controls;

/// <summary>
/// Attachments need an "+ Add" action and a per-row "Remove" action —
/// DashboardListCard only renders static text rows, so rather than bolt
/// actions onto that shared control (used by four other dashboard tiles),
/// this extends DashboardCard directly for its title+card chrome and builds
/// a fully custom Body, the same way PieChart/BarChart do.
/// </summary>
public class PurchaseOrderAttachmentsPanel : DashboardCard
{
    private readonly Panel _listPanel;

    public event Action? AddRequested;
    public event Action<PurchaseOrderAttachment>? RemoveRequested;

    public PurchaseOrderAttachmentsPanel() : base("Attachments")
    {
        Dock = DockStyle.Top;
        Height = 220;

        var addButton = new Button { Text = "+ Add File", Dock = DockStyle.Top, Height = 28 }.StyleAsSecondaryButton();
        addButton.Click += (_, _) => AddRequested?.Invoke();

        _listPanel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(0, 8, 0, 0) };

        Body.Controls.Add(_listPanel);
        Body.Controls.Add(addButton);
    }

    public void SetAttachments(IReadOnlyList<PurchaseOrderAttachment> attachments)
    {
        _listPanel.Controls.Clear();

        if (attachments.Count == 0)
        {
            _listPanel.Controls.Add(new Label
            {
                Text = "No files attached yet.",
                Font = Theme.FontSmall,
                ForeColor = Theme.TextSecondary,
                Dock = DockStyle.Top,
                Height = 24,
            });
            return;
        }

        // Add bottom-up since each row docks to Top.
        for (var i = attachments.Count - 1; i >= 0; i--)
        {
            var attachment = attachments[i];
            var row = new Panel { Dock = DockStyle.Top, Height = 54, Padding = new Padding(0, 0, 0, 6) };

            var nameLink = new LinkLabel
            {
                Text = attachment.FileName,
                Font = Theme.FontSmall,
                LinkColor = Theme.TextPrimary,
                ActiveLinkColor = Theme.TextPrimary,
                VisitedLinkColor = Theme.TextPrimary,
                LinkBehavior = LinkBehavior.HoverUnderline,
                Dock = DockStyle.Top,
                Height = 18,
                AutoEllipsis = true,
            };
            nameLink.LinkClicked += (_, _) => OpenFile(attachment.StoredPath);

            var metaLabel = new Label
            {
                Text = $"{attachment.UploadedByUser?.DisplayName ?? "Unknown"} · {attachment.UploadedAt.ToLocalTime():MM/dd/yyyy h:mm tt}",
                Font = Theme.FontSmall,
                ForeColor = Theme.TextSecondary,
                Dock = DockStyle.Top,
                Height = 16,
                AutoEllipsis = true,
            };

            var removeLink = new LinkLabel
            {
                Text = "Remove",
                Font = Theme.FontSmall,
                LinkColor = Theme.DangerRed,
                ActiveLinkColor = Theme.DangerRed,
                VisitedLinkColor = Theme.DangerRed,
                LinkBehavior = LinkBehavior.HoverUnderline,
                Dock = DockStyle.Top,
                Height = 16,
                AutoSize = true,
            };
            removeLink.LinkClicked += (_, _) => RemoveRequested?.Invoke(attachment);

            row.Controls.Add(removeLink);
            row.Controls.Add(metaLabel);
            row.Controls.Add(nameLink);
            _listPanel.Controls.Add(row);
        }
    }

    private static void OpenFile(string path)
    {
        try
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(path) { UseShellExecute = true });
        }
        catch (Exception)
        {
            MessageBox.Show("Unable to open this file. It may have been moved or deleted.", "Attachment", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }
}
