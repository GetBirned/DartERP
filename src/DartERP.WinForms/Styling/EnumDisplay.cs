using System.Text;

namespace DartERP.WinForms.Styling;

/// <summary>
/// Renders PascalCase enum values ("InProduction") as spaced display
/// text ("In Production") for grids and labels.
/// </summary>
public static class EnumDisplay
{
    public static string For(Enum value)
    {
        var name = value.ToString();
        var builder = new StringBuilder(name.Length + 4);

        for (var i = 0; i < name.Length; i++)
        {
            if (i > 0 && char.IsUpper(name[i]))
                builder.Append(' ');
            builder.Append(name[i]);
        }

        return builder.ToString();
    }
}
