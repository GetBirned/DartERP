using DartERP.Core.Enums;

namespace DartERP.Core.Models;

public class SerializedItem
{
    public int SerializedItemId { get; set; }
    public string SerialNumber { get; set; } = string.Empty;
    public int ProductId { get; set; }
    public int WorkOrderId { get; set; }
    public SerializedItemStatus Status { get; set; } = SerializedItemStatus.InProduction;
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

    public Product? Product { get; set; }
    public WorkOrder? WorkOrder { get; set; }
    public ICollection<QualityInspection> Inspections { get; set; } = new List<QualityInspection>();
    public ICollection<Disposition> Dispositions { get; set; } = new List<Disposition>();
}
