using System.Text;

namespace DartERP.WinForms.Local;

/// <summary>
/// Reads straight from an already-bound, already-formatted DataGridView
/// rather than re-deriving each screen's display logic a second time —
/// FormattedValue runs the same CellFormatting pipeline used for painting,
/// so the CSV always matches exactly what's on screen (including whatever
/// search/filter/sort is currently applied).
/// </summary>
public static class CsvExporter
{
    public static void ExportGrid(DataGridView grid, string suggestedFileName)
    {
        using var dialog = new SaveFileDialog { Filter = "CSV Files (*.csv)|*.csv", FileName = suggestedFileName };
        if (dialog.ShowDialog() != DialogResult.OK)
            return;

        var columns = grid.Columns.Cast<DataGridViewColumn>()
            .Where(c => c.Visible)
            .OrderBy(c => c.DisplayIndex)
            .ToList();

        var sb = new StringBuilder();
        sb.Append(string.Join(",", columns.Select(c => EscapeField(c.HeaderText))));
        sb.Append("\r\n");

        foreach (DataGridViewRow row in grid.Rows)
        {
            var fields = columns.Select(c => EscapeField(row.Cells[c.Index].FormattedValue?.ToString() ?? string.Empty));
            sb.Append(string.Join(",", fields));
            sb.Append("\r\n");
        }

        try
        {
            File.WriteAllText(dialog.FileName, sb.ToString(), new UTF8Encoding(true));
        }
        catch (IOException)
        {
            MessageBox.Show("Unable to save the file. It may be open in another program.", "Export CSV", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    private static string EscapeField(string value)
    {
        if (value.Contains(',') || value.Contains('"') || value.Contains('\n') || value.Contains('\r'))
            return $"\"{value.Replace("\"", "\"\"")}\"";
        return value;
    }
}
