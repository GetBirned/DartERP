using System.Text;
using Syncfusion.WinForms.DataGrid;

namespace DartERP.WinForms.Local;

/// <summary>
/// Reads straight from the grid's bound row objects via reflection on each
/// column's MappingName. Every screen already pre-projects its rows into a
/// plain display-row type with already-formatted string properties (see
/// CustomerListControl's CustomerRow for the pattern), so reflecting those
/// property values is equivalent to reading exactly what's on screen —
/// there's no separate "rendered text" API on SfDataGrid the way
/// DataGridView.FormattedValue gave the old version of this class.
/// </summary>
public static class CsvExporter
{
    public static void ExportGrid(SfDataGrid grid, string suggestedFileName)
    {
        using var dialog = new SaveFileDialog { Filter = "CSV Files (*.csv)|*.csv", FileName = suggestedFileName };
        if (dialog.ShowDialog() != DialogResult.OK)
            return;

        var columns = grid.Columns.Where(c => c.Visible).ToList();

        var sb = new StringBuilder();
        sb.Append(string.Join(",", columns.Select(c => EscapeField(c.HeaderText))));
        sb.Append("\r\n");

        foreach (var record in grid.View.Records)
        {
            var data = record.Data;
            var fields = columns.Select(c =>
            {
                var property = data.GetType().GetProperty(c.MappingName);
                var value = property?.GetValue(data)?.ToString() ?? string.Empty;
                return EscapeField(value);
            });
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
