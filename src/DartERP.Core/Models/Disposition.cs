using DartERP.Core.Enums;

namespace DartERP.Core.Models;

/// <summary>
/// ATF-style Acquisition &amp; Disposition record: where a serialized item
/// went, when, and how. "Acquisition" is already covered by SerializedItem
/// itself (it exists the moment it's produced against a WorkOrder) — this
/// is the disposition half of the bound-book pair. A serialized item can
/// have more than one over its life (e.g. sold, then returned).
/// </summary>
public class Disposition
{
    public int DispositionId { get; set; }
    public int SerializedItemId { get; set; }
    public DateTime DispositionDate { get; set; } = DateTime.UtcNow;
    public DispositionType Type { get; set; }

    /// <summary>Who the item went to. Required for Sold/Transferred, not applicable for Destroyed/Returned.</summary>
    public int? CustomerId { get; set; }

    public string Notes { get; set; } = string.Empty;

    public SerializedItem? SerializedItem { get; set; }
    public Customer? Customer { get; set; }
}
