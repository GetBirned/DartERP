using DartERP.Core.Enums;

namespace DartERP.Core.Models;

public class WorkOrderStatusHistory
{
    public int WorkOrderStatusHistoryId { get; set; }
    public int WorkOrderId { get; set; }
    public WorkOrderStatus? FromStatus { get; set; }
    public WorkOrderStatus ToStatus { get; set; }
    public int ChangedByUserId { get; set; }
    public DateTime ChangedAt { get; set; } = DateTime.UtcNow;

    public WorkOrder? WorkOrder { get; set; }
    public User? ChangedByUser { get; set; }
}
