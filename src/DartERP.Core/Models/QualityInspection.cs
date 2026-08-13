using DartERP.Core.Enums;

namespace DartERP.Core.Models;

public class QualityInspection
{
    public int QualityInspectionId { get; set; }
    public int SerializedItemId { get; set; }
    public DateTime InspectionDate { get; set; } = DateTime.UtcNow;
    public string Inspector { get; set; } = string.Empty;
    public QualityResult Result { get; set; } = QualityResult.Pending;
    public string Notes { get; set; } = string.Empty;

    public SerializedItem? SerializedItem { get; set; }
}
