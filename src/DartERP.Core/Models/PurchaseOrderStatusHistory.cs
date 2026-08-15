using DartERP.Core.Enums;

namespace DartERP.Core.Models;

public class PurchaseOrderStatusHistory
{
    public int PurchaseOrderStatusHistoryId { get; set; }
    public int PurchaseOrderId { get; set; }
    public PurchaseOrderStatus? FromStatus { get; set; }
    public PurchaseOrderStatus ToStatus { get; set; }
    public int ChangedByUserId { get; set; }
    public DateTime ChangedAt { get; set; } = DateTime.UtcNow;

    public PurchaseOrder? PurchaseOrder { get; set; }
    public User? ChangedByUser { get; set; }
}
