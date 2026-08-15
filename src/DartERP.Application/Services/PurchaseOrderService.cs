using DartERP.Application.Validation;
using DartERP.Core.DTOs;
using DartERP.Core.Enums;
using DartERP.Core.Interfaces;
using DartERP.Core.Models;

namespace DartERP.Application.Services;

public class PurchaseOrderService
{
    private readonly IPurchaseOrderRepository _repository;
    private readonly IVendorRepository _vendorRepository;
    private readonly CurrentUserContext _currentUserContext;

    public PurchaseOrderService(IPurchaseOrderRepository repository, IVendorRepository vendorRepository, CurrentUserContext currentUserContext)
    {
        _repository = repository;
        _vendorRepository = vendorRepository;
        _currentUserContext = currentUserContext;
    }

    public Task<List<PurchaseOrderStatusHistory>> GetStatusHistoryAsync(int purchaseOrderId) => _repository.GetStatusHistoryAsync(purchaseOrderId);

    public Task<List<PurchaseOrderAttachment>> GetAttachmentsAsync(int purchaseOrderId) => _repository.GetAttachmentsAsync(purchaseOrderId);

    public Task AddAttachmentAsync(int purchaseOrderId, string fileName, string storedPath, long fileSizeBytes) =>
        _repository.AddAttachmentAsync(new PurchaseOrderAttachment
        {
            PurchaseOrderId = purchaseOrderId,
            FileName = fileName,
            StoredPath = storedPath,
            FileSizeBytes = fileSizeBytes,
            UploadedByUserId = _currentUserContext.CurrentUser!.UserId,
        });

    public Task RemoveAttachmentAsync(int attachmentId) => _repository.DeleteAttachmentAsync(attachmentId);

    public Task<List<PurchaseOrder>> GetAllWithVendorAsync() => _repository.GetAllWithVendorAsync();

    public Task<PurchaseOrder?> GetWithLinesAsync(int id) => _repository.GetWithLinesAsync(id);

    public Task<List<PurchaseOrder>> GetRecentAsync(int count) => _repository.GetRecentAsync(count);

    public Task<int> GetOpenCountAsync() => _repository.GetOpenCountAsync();

    public async Task<PurchaseOrder> CreateAsync(
        int vendorId, DateTime? expectedDate, string notes, PurchaseOrderStatus status, List<PurchaseOrderLineInput> lines)
    {
        await ValidateVendorAsync(vendorId);
        ValidateLines(lines, status);

        var po = new PurchaseOrder
        {
            PurchaseOrderNumber = await _repository.GetNextPurchaseOrderNumberAsync(),
            VendorId = vendorId,
            OrderDate = DateTime.UtcNow,
            ExpectedDate = expectedDate,
            Status = status,
            Notes = notes,
        };

        foreach (var line in lines)
        {
            po.Lines.Add(new PurchaseOrderLine
            {
                ProductId = line.ProductId,
                Quantity = line.Quantity,
                UnitCost = line.UnitCost,
                LineTotal = line.Quantity * line.UnitCost,
            });
        }
        po.TotalAmount = po.Lines.Sum(l => l.LineTotal);

        await _repository.AddAsync(po);

        await _repository.AddStatusHistoryAsync(new PurchaseOrderStatusHistory
        {
            PurchaseOrderId = po.PurchaseOrderId,
            FromStatus = null,
            ToStatus = po.Status,
            ChangedByUserId = _currentUserContext.CurrentUser!.UserId,
        });

        return po;
    }

    public async Task UpdateAsync(
        int purchaseOrderId, int vendorId, DateTime? expectedDate, string notes, PurchaseOrderStatus status, List<PurchaseOrderLineInput> lines)
    {
        await ValidateVendorAsync(vendorId);
        ValidateLines(lines, status);

        var existing = await _repository.GetByIdAsync(purchaseOrderId);
        var oldStatus = existing?.Status;

        var lineEntities = lines
            .Select(l => new PurchaseOrderLine
            {
                ProductId = l.ProductId,
                Quantity = l.Quantity,
                UnitCost = l.UnitCost,
                LineTotal = l.Quantity * l.UnitCost,
            })
            .ToList();

        var header = new PurchaseOrder
        {
            PurchaseOrderId = purchaseOrderId,
            VendorId = vendorId,
            ExpectedDate = expectedDate,
            Status = status,
            Notes = notes,
            TotalAmount = lineEntities.Sum(l => l.LineTotal),
        };

        await _repository.UpdateWithLinesAsync(header, lineEntities);

        if (oldStatus is not null && oldStatus != status)
        {
            await _repository.AddStatusHistoryAsync(new PurchaseOrderStatusHistory
            {
                PurchaseOrderId = purchaseOrderId,
                FromStatus = oldStatus,
                ToStatus = status,
                ChangedByUserId = _currentUserContext.CurrentUser!.UserId,
            });
        }
    }

    private async Task ValidateVendorAsync(int vendorId)
    {
        if (vendorId <= 0)
            throw new ValidationException("Select a vendor before saving.");

        var vendor = await _vendorRepository.GetByIdAsync(vendorId)
            ?? throw new ValidationException("The selected vendor no longer exists.");

        if (!vendor.IsActive)
            throw new ValidationException("This vendor is inactive and cannot be used on a purchase order.");
    }

    private static void ValidateLines(List<PurchaseOrderLineInput> lines, PurchaseOrderStatus status)
    {
        if (status != PurchaseOrderStatus.Draft && lines.Count == 0)
            throw new ValidationException("Add at least one line before submitting this purchase order.");

        foreach (var line in lines)
        {
            if (line.ProductId <= 0)
                throw new ValidationException("Every line must have a product selected.");

            if (line.Quantity <= 0)
                throw new ValidationException("Quantity must be greater than zero on every line.");

            if (line.UnitCost < 0)
                throw new ValidationException("Unit cost cannot be negative.");
        }
    }
}
