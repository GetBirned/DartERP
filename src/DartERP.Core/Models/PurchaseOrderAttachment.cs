namespace DartERP.Core.Models;

public class PurchaseOrderAttachment
{
    public int PurchaseOrderAttachmentId { get; set; }
    public int PurchaseOrderId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string StoredPath { get; set; } = string.Empty;
    public long FileSizeBytes { get; set; }
    public int UploadedByUserId { get; set; }
    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;

    public PurchaseOrder? PurchaseOrder { get; set; }
    public User? UploadedByUser { get; set; }
}
