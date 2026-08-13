using DartERP.Core.Enums;

namespace DartERP.WinForms.Styling;

/// <summary>
/// Maps domain status enums to a consistent badge color across the app.
/// </summary>
public static class StatusColors
{
    public static Color For(PurchaseOrderStatus status) => status switch
    {
        PurchaseOrderStatus.Draft => Theme.NeutralGray,
        PurchaseOrderStatus.Submitted => Theme.WarningAmber,
        PurchaseOrderStatus.Approved => Theme.AccentBlue,
        PurchaseOrderStatus.Received => Theme.SuccessGreen,
        PurchaseOrderStatus.Cancelled => Theme.DangerRed,
        _ => Theme.NeutralGray,
    };

    public static Color For(WorkOrderStatus status) => status switch
    {
        WorkOrderStatus.Planned => Theme.NeutralGray,
        WorkOrderStatus.Released => Theme.WarningAmber,
        WorkOrderStatus.InProduction => Theme.AccentBlue,
        WorkOrderStatus.QualityControl => Color.FromArgb(0x7C, 0x3A, 0xED),
        WorkOrderStatus.Completed => Theme.SuccessGreen,
        WorkOrderStatus.Cancelled => Theme.DangerRed,
        _ => Theme.NeutralGray,
    };

    public static Color For(SerializedItemStatus status) => status switch
    {
        SerializedItemStatus.InProduction => Theme.AccentBlue,
        SerializedItemStatus.InStock => Theme.SuccessGreen,
        SerializedItemStatus.Shipped => Theme.NeutralGray,
        SerializedItemStatus.Scrapped => Theme.DangerRed,
        _ => Theme.NeutralGray,
    };

    public static Color For(QualityResult result) => result switch
    {
        QualityResult.Pending => Theme.WarningAmber,
        QualityResult.Passed => Theme.SuccessGreen,
        QualityResult.Failed => Theme.DangerRed,
        _ => Theme.NeutralGray,
    };
}
