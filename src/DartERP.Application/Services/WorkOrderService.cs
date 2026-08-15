using DartERP.Application.Validation;
using DartERP.Core.Enums;
using DartERP.Core.Interfaces;
using DartERP.Core.Models;

namespace DartERP.Application.Services;

public class WorkOrderService
{
    private readonly IWorkOrderRepository _repository;
    private readonly IProductRepository _productRepository;
    private readonly CurrentUserContext _currentUserContext;

    public WorkOrderService(IWorkOrderRepository repository, IProductRepository productRepository, CurrentUserContext currentUserContext)
    {
        _repository = repository;
        _productRepository = productRepository;
        _currentUserContext = currentUserContext;
    }

    public Task<List<WorkOrderStatusHistory>> GetStatusHistoryAsync(int workOrderId) => _repository.GetStatusHistoryAsync(workOrderId);

    public Task<List<WorkOrder>> GetAllWithProductAsync() => _repository.GetAllWithProductAsync();

    public Task<WorkOrder?> GetByIdAsync(int id) => _repository.GetByIdAsync(id);

    public Task<List<WorkOrder>> GetDueSoonAsync(int days) => _repository.GetDueSoonAsync(days);

    public Task<int> GetOpenCountAsync() => _repository.GetOpenCountAsync();

    public Task<int> GetUnitsInProductionAsync() => _repository.GetUnitsInProductionAsync();

    public async Task<WorkOrder> CreateAsync(int productId, int quantity, DateTime startDate, DateTime dueDate, string notes, WorkOrderStatus status)
    {
        await ValidateAsync(productId, quantity, startDate, dueDate);

        var workOrder = new WorkOrder
        {
            WorkOrderNumber = await _repository.GetNextWorkOrderNumberAsync(),
            ProductId = productId,
            Quantity = quantity,
            StartDate = startDate,
            DueDate = dueDate,
            Status = status,
            Notes = notes,
        };

        await _repository.AddAsync(workOrder);

        await _repository.AddStatusHistoryAsync(new WorkOrderStatusHistory
        {
            WorkOrderId = workOrder.WorkOrderId,
            FromStatus = null,
            ToStatus = workOrder.Status,
            ChangedByUserId = _currentUserContext.CurrentUser!.UserId,
        });

        return workOrder;
    }

    public async Task UpdateAsync(WorkOrder existing, int productId, int quantity, DateTime startDate, DateTime dueDate, string notes, WorkOrderStatus status)
    {
        if (IsLocked(existing.Status))
            throw new ValidationException("Completed or cancelled work orders can't be edited.");

        await ValidateAsync(productId, quantity, startDate, dueDate);

        var oldStatus = existing.Status;

        existing.ProductId = productId;
        existing.Quantity = quantity;
        existing.StartDate = startDate;
        existing.DueDate = dueDate;
        existing.Notes = notes;
        existing.Status = status;

        await _repository.UpdateAsync(existing);

        if (oldStatus != status)
        {
            await _repository.AddStatusHistoryAsync(new WorkOrderStatusHistory
            {
                WorkOrderId = existing.WorkOrderId,
                FromStatus = oldStatus,
                ToStatus = status,
                ChangedByUserId = _currentUserContext.CurrentUser!.UserId,
            });
        }
    }

    public static bool IsLocked(WorkOrderStatus status) =>
        status is WorkOrderStatus.Completed or WorkOrderStatus.Cancelled;

    private async Task ValidateAsync(int productId, int quantity, DateTime startDate, DateTime dueDate)
    {
        if (productId <= 0)
            throw new ValidationException("Select a product before saving.");

        var product = await _productRepository.GetByIdAsync(productId)
            ?? throw new ValidationException("The selected product no longer exists.");

        if (!product.IsActive)
            throw new ValidationException("This product is inactive and cannot be used on a work order.");

        if (quantity <= 0)
            throw new ValidationException("Quantity must be greater than zero.");

        if (dueDate.Date < startDate.Date)
            throw new ValidationException("Due date cannot be before the start date.");
    }
}
