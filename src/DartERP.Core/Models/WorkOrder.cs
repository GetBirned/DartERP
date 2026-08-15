using DartERP.Core.Enums;

namespace DartERP.Core.Models;

public class WorkOrder
{
    public int WorkOrderId { get; set; }
    public string WorkOrderNumber { get; set; } = string.Empty;
    public int ProductId { get; set; }
    public int Quantity { get; set; }
    public DateTime StartDate { get; set; } = DateTime.UtcNow;
    public DateTime DueDate { get; set; }
    public WorkOrderStatus Status { get; set; } = WorkOrderStatus.Planned;
    public string Notes { get; set; } = string.Empty;

    public Product? Product { get; set; }
    public ICollection<SerializedItem> SerializedItems { get; set; } = new List<SerializedItem>();
    public ICollection<WorkOrderStatusHistory> StatusHistory { get; set; } = new List<WorkOrderStatusHistory>();
}
