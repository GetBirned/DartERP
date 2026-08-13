namespace DartERP.Core.DTOs;

/// <summary>
/// Working-copy shape for a PO line while it's being edited in the UI,
/// before it's turned into a tracked <see cref="Models.PurchaseOrderLine"/>.
/// </summary>
public class PurchaseOrderLineInput
{
    public int ProductId { get; set; }
    public int Quantity { get; set; }
    public decimal UnitCost { get; set; }
}
