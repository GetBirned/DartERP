using DartERP.Core.Enums;
using DartERP.Core.Models;
using DartERP.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace DartERP.Infrastructure.Seed;

/// <summary>
/// Populates a freshly-migrated database with realistic fictional demo data.
/// Idempotent: no-ops if Customers already has rows.
/// </summary>
public static class DbSeeder
{
    public static async Task SeedAsync(DartErpDbContext context)
    {
        if (await context.Customers.AnyAsync())
            return;

        var customers = SeedCustomers();
        var vendors = SeedVendors();
        var products = SeedProducts();

        context.Customers.AddRange(customers);
        context.Vendors.AddRange(vendors);
        context.Products.AddRange(products);
        await context.SaveChangesAsync();

        var purchaseOrders = SeedPurchaseOrders(vendors, products);
        context.PurchaseOrders.AddRange(purchaseOrders);
        await context.SaveChangesAsync();

        var workOrders = SeedWorkOrders(products);
        context.WorkOrders.AddRange(workOrders);
        await context.SaveChangesAsync();

        var serializedItems = SeedSerializedItems(workOrders);
        context.SerializedItems.AddRange(serializedItems);
        await context.SaveChangesAsync();

        var inspections = SeedQualityInspections(serializedItems);
        context.QualityInspections.AddRange(inspections);
        await context.SaveChangesAsync();

        var dispositions = SeedDispositions(serializedItems, customers);
        context.Dispositions.AddRange(dispositions);
        await context.SaveChangesAsync();
    }

    private static List<Customer> SeedCustomers() =>
    [
        new() { CustomerNumber = "CUST-1001", CompanyName = "Granite State Sporting Supply", ContactName = "Karen Whitfield", Email = "karen.whitfield@gssupply.example", Phone = "603-555-0142", Address = "88 Elm Street", City = "Manchester", State = "NH", IsActive = true },
        new() { CustomerNumber = "CUST-1002", CompanyName = "Northeast Distribution Group", ContactName = "Marcus Deleon", Email = "m.deleon@nedistgroup.example", Phone = "860-555-0118", Address = "412 Industrial Pkwy", City = "Hartford", State = "CT", IsActive = true },
        new() { CustomerNumber = "CUST-1003", CompanyName = "White Mountain Outfitters", ContactName = "Sarah Kincaid", Email = "sarah@whitemountainoutfitters.example", Phone = "603-555-0177", Address = "27 Summit Road", City = "Concord", State = "NH", IsActive = true },
        new() { CustomerNumber = "CUST-1004", CompanyName = "Atlantic Sporting Goods", ContactName = "Daniel Okafor", Email = "dan.okafor@atlanticsg.example", Phone = "207-555-0163", Address = "930 Harbor Ave", City = "Portland", State = "ME", IsActive = true },
        new() { CustomerNumber = "CUST-1005", CompanyName = "Cascade Retail Partners", ContactName = "Emily Foster", Email = "efoster@cascaderetail.example", Phone = "503-555-0129", Address = "1150 Riverside Dr", City = "Portland", State = "OR", IsActive = true },
        new() { CustomerNumber = "CUST-1006", CompanyName = "Blue Ridge Wholesale", ContactName = "Tom Whitaker", Email = "twhitaker@blueridgewholesale.example", Phone = "828-555-0104", Address = "56 Ridge Line Rd", City = "Asheville", State = "NC", IsActive = false },
    ];

    private static List<Vendor> SeedVendors() =>
    [
        new() { VendorNumber = "VEND-2001", CompanyName = "Precision Materials Inc.", ContactName = "Rob Callahan", Email = "rob@precisionmaterials.example", Phone = "603-555-0201", VendorType = VendorType.RawMaterials, IsActive = true },
        new() { VendorNumber = "VEND-2002", CompanyName = "Granite Industrial Supply", ContactName = "Lisa Nguyen", Email = "lnguyen@graniteindustrial.example", Phone = "603-555-0212", VendorType = VendorType.Components, IsActive = true },
        new() { VendorNumber = "VEND-2003", CompanyName = "Northeast Components", ContactName = "Mike Sorensen", Email = "msorensen@necomponents.example", Phone = "617-555-0189", VendorType = VendorType.Components, IsActive = true },
        new() { VendorNumber = "VEND-2004", CompanyName = "Summit Packaging", ContactName = "Angela Ruiz", Email = "aruiz@summitpackaging.example", Phone = "603-555-0233", VendorType = VendorType.Packaging, IsActive = true },
        new() { VendorNumber = "VEND-2005", CompanyName = "Apex Tooling Solutions", ContactName = "Greg Palmer", Email = "gpalmer@apextooling.example", Phone = "508-555-0147", VendorType = VendorType.Services, IsActive = true },
        new() { VendorNumber = "VEND-2006", CompanyName = "Keystone Logistics", ContactName = "Nina Osei", Email = "nosei@keystonelogistics.example", Phone = "717-555-0198", VendorType = VendorType.Services, IsActive = false },
    ];

    private static List<Product> SeedProducts() =>
    [
        new() { SKU = "DERP-1001", ProductName = "Model Alpha", Category = ProductCategory.FinishedProduct, Description = "Full-size finished product, standard configuration.", UnitCost = 425.00m, SalePrice = 699.00m, QuantityOnHand = 12, ReorderLevel = 10, IsSerialized = true, IsActive = true },
        new() { SKU = "DERP-1002", ProductName = "Model Bravo", Category = ProductCategory.FinishedProduct, Description = "Compact finished product, premium finish.", UnitCost = 510.00m, SalePrice = 849.00m, QuantityOnHand = 6, ReorderLevel = 8, IsSerialized = true, IsActive = true },
        new() { SKU = "DERP-1003", ProductName = "Model Charlie Compact", Category = ProductCategory.FinishedProduct, Description = "Compact finished product, entry configuration.", UnitCost = 390.00m, SalePrice = 649.00m, QuantityOnHand = 20, ReorderLevel = 10, IsSerialized = true, IsActive = true },
        new() { SKU = "DERP-2001", ProductName = "Steel Billet Stock", Category = ProductCategory.RawMaterial, Description = "Raw steel billet stock for machining.", UnitCost = 45.00m, SalePrice = 0m, QuantityOnHand = 500, ReorderLevel = 100, IsSerialized = false, IsActive = true },
        new() { SKU = "DERP-2002", ProductName = "Polymer Grip Housing", Category = ProductCategory.Component, Description = "Molded polymer housing component.", UnitCost = 18.00m, SalePrice = 0m, QuantityOnHand = 300, ReorderLevel = 75, IsSerialized = false, IsActive = true },
        new() { SKU = "DERP-2003", ProductName = "Precision Spring Set", Category = ProductCategory.Component, Description = "Matched set of precision internal springs.", UnitCost = 6.50m, SalePrice = 0m, QuantityOnHand = 40, ReorderLevel = 50, IsSerialized = false, IsActive = true },
        new() { SKU = "DERP-2004", ProductName = "Recoil Assembly Kit", Category = ProductCategory.Component, Description = "Complete recoil assembly component kit.", UnitCost = 32.00m, SalePrice = 0m, QuantityOnHand = 150, ReorderLevel = 40, IsSerialized = false, IsActive = true },
        new() { SKU = "DERP-3001", ProductName = "Retail Packaging Box", Category = ProductCategory.Packaging, Description = "Branded retail packaging, standard size.", UnitCost = 3.25m, SalePrice = 0m, QuantityOnHand = 800, ReorderLevel = 200, IsSerialized = false, IsActive = true },
        new() { SKU = "DERP-3002", ProductName = "Foam Case Insert", Category = ProductCategory.Packaging, Description = "Custom-cut foam case insert.", UnitCost = 5.75m, SalePrice = 0m, QuantityOnHand = 60, ReorderLevel = 100, IsSerialized = false, IsActive = true },
    ];

    private static List<PurchaseOrder> SeedPurchaseOrders(List<Vendor> vendors, List<Product> products)
    {
        Product P(string sku) => products.First(p => p.SKU == sku);
        Vendor V(string number) => vendors.First(v => v.VendorNumber == number);

        List<PurchaseOrder> orders =
        [
            BuildPo("PO-10041", V("VEND-2001"), -2, PurchaseOrderStatus.Draft,
                (P("DERP-2001"), 200, 44.00m)),

            BuildPo("PO-10042", V("VEND-2003"), -5, PurchaseOrderStatus.Submitted,
                (P("DERP-2004"), 100, 31.50m),
                (P("DERP-2003"), 150, 6.25m)),

            BuildPo("PO-10043", V("VEND-2002"), -9, PurchaseOrderStatus.Approved,
                (P("DERP-2002"), 250, 17.75m)),

            BuildPo("PO-10044", V("VEND-2004"), -14, PurchaseOrderStatus.Received,
                (P("DERP-3001"), 500, 3.10m),
                (P("DERP-3002"), 100, 5.60m)),

            BuildPo("PO-10045", V("VEND-2001"), -21, PurchaseOrderStatus.Received,
                (P("DERP-2001"), 300, 43.50m)),

            BuildPo("PO-10046", V("VEND-2005"), -8, PurchaseOrderStatus.Cancelled,
                (P("DERP-2004"), 50, 33.00m)),

            BuildPo("PO-10047", V("VEND-2003"), -1, PurchaseOrderStatus.Draft,
                (P("DERP-2003"), 200, 6.20m)),
        ];

        return orders;
    }

    private static PurchaseOrder BuildPo(
        string number, Vendor vendor, int orderDateOffsetDays, PurchaseOrderStatus status,
        params (Product Product, int Quantity, decimal UnitCost)[] lineSpecs)
    {
        var po = new PurchaseOrder
        {
            PurchaseOrderNumber = number,
            Vendor = vendor,
            OrderDate = DateTime.UtcNow.AddDays(orderDateOffsetDays),
            ExpectedDate = DateTime.UtcNow.AddDays(orderDateOffsetDays + 14),
            Status = status,
            Notes = string.Empty,
        };

        foreach (var (product, quantity, unitCost) in lineSpecs)
        {
            var lineTotal = quantity * unitCost;
            po.Lines.Add(new PurchaseOrderLine
            {
                Product = product,
                Quantity = quantity,
                UnitCost = unitCost,
                LineTotal = lineTotal,
            });
        }

        po.TotalAmount = po.Lines.Sum(l => l.LineTotal);
        return po;
    }

    private static List<WorkOrder> SeedWorkOrders(List<Product> products)
    {
        Product P(string sku) => products.First(p => p.SKU == sku);

        return
        [
            new() { WorkOrderNumber = "WO-10021", Product = P("DERP-1001"), Quantity = 12, StartDate = DateTime.UtcNow.AddDays(-20), DueDate = DateTime.UtcNow.AddDays(-6), Status = WorkOrderStatus.Completed, Notes = "Standard production run." },
            new() { WorkOrderNumber = "WO-10022", Product = P("DERP-1001"), Quantity = 10, StartDate = DateTime.UtcNow.AddDays(-4), DueDate = DateTime.UtcNow.AddDays(5), Status = WorkOrderStatus.InProduction, Notes = string.Empty },
            new() { WorkOrderNumber = "WO-10023", Product = P("DERP-1002"), Quantity = 8, StartDate = DateTime.UtcNow.AddDays(-2), DueDate = DateTime.UtcNow.AddDays(3), Status = WorkOrderStatus.Released, Notes = string.Empty },
            new() { WorkOrderNumber = "WO-10024", Product = P("DERP-1003"), Quantity = 10, StartDate = DateTime.UtcNow.AddDays(-15), DueDate = DateTime.UtcNow.AddDays(-3), Status = WorkOrderStatus.Completed, Notes = "Rush order for Cascade Retail Partners." },
            new() { WorkOrderNumber = "WO-10025", Product = P("DERP-1002"), Quantity = 12, StartDate = DateTime.UtcNow.AddDays(1), DueDate = DateTime.UtcNow.AddDays(4), Status = WorkOrderStatus.Planned, Notes = string.Empty },
            new() { WorkOrderNumber = "WO-10026", Product = P("DERP-1001"), Quantity = 6, StartDate = DateTime.UtcNow.AddDays(-6), DueDate = DateTime.UtcNow.AddDays(2), Status = WorkOrderStatus.QualityControl, Notes = "Awaiting final inspection." },
        ];
    }

    private static List<SerializedItem> SeedSerializedItems(List<WorkOrder> workOrders)
    {
        var items = new List<SerializedItem>();
        var serial = 1001;

        WorkOrder WO(string number) => workOrders.First(w => w.WorkOrderNumber == number);

        // WO-10021 (Completed): mix of in-stock and shipped units.
        var wo21 = WO("WO-10021");
        for (var i = 0; i < wo21.Quantity; i++)
        {
            items.Add(new SerializedItem
            {
                SerialNumber = $"DERP-2026-{serial++:D6}",
                Product = wo21.Product,
                WorkOrder = wo21,
                Status = i < 8 ? SerializedItemStatus.InStock : SerializedItemStatus.Shipped,
                CreatedDate = wo21.DueDate,
            });
        }

        // WO-10024 (Completed): mostly in stock, a couple shipped.
        var wo24 = WO("WO-10024");
        for (var i = 0; i < wo24.Quantity; i++)
        {
            items.Add(new SerializedItem
            {
                SerialNumber = $"DERP-2026-{serial++:D6}",
                Product = wo24.Product,
                WorkOrder = wo24,
                Status = i < 7 ? SerializedItemStatus.InStock : SerializedItemStatus.Shipped,
                CreatedDate = wo24.DueDate,
            });
        }

        // WO-10026 (QualityControl): still in production, awaiting inspection.
        var wo26 = WO("WO-10026");
        for (var i = 0; i < wo26.Quantity; i++)
        {
            items.Add(new SerializedItem
            {
                SerialNumber = $"DERP-2026-{serial++:D6}",
                Product = wo26.Product,
                WorkOrder = wo26,
                Status = SerializedItemStatus.InProduction,
                CreatedDate = DateTime.UtcNow.AddDays(-1),
            });
        }

        return items;
    }

    private static List<QualityInspection> SeedQualityInspections(List<SerializedItem> serializedItems)
    {
        var inspections = new List<QualityInspection>();
        var inspectors = new[] { "J. Alvarez", "T. Brennan", "R. Chen" };
        var rand = new Random(42);

        // Completed work order units: mostly passed, one failed for realism.
        var completedUnits = serializedItems
            .Where(s => s.Status is SerializedItemStatus.InStock or SerializedItemStatus.Shipped)
            .ToList();

        for (var i = 0; i < completedUnits.Count; i++)
        {
            var result = i == 2 ? QualityResult.Failed : QualityResult.Passed;
            inspections.Add(new QualityInspection
            {
                SerializedItem = completedUnits[i],
                InspectionDate = completedUnits[i].CreatedDate,
                Inspector = inspectors[rand.Next(inspectors.Length)],
                Result = result,
                Notes = result == QualityResult.Failed ? "Surface finish out of tolerance; reworked." : string.Empty,
            });
        }

        // In-production units still awaiting inspection.
        var pendingUnits = serializedItems.Where(s => s.Status == SerializedItemStatus.InProduction);
        foreach (var unit in pendingUnits)
        {
            inspections.Add(new QualityInspection
            {
                SerializedItem = unit,
                InspectionDate = DateTime.UtcNow,
                Inspector = inspectors[rand.Next(inspectors.Length)],
                Result = QualityResult.Pending,
                Notes = string.Empty,
            });
        }

        return inspections;
    }

    private static List<Disposition> SeedDispositions(List<SerializedItem> serializedItems, List<Customer> customers)
    {
        // Every already-Shipped item in the seed data should have a bound-book
        // entry explaining where it went — otherwise Serialized Inventory and
        // the A&D Log would disagree with each other on first launch.
        var shippedUnits = serializedItems.Where(s => s.Status == SerializedItemStatus.Shipped).ToList();
        var activeCustomers = customers.Where(c => c.IsActive).ToList();
        var dispositions = new List<Disposition>();

        for (var i = 0; i < shippedUnits.Count; i++)
        {
            var unit = shippedUnits[i];
            var customer = activeCustomers[i % activeCustomers.Count];
            var isTransfer = i == shippedUnits.Count - 1;

            dispositions.Add(new Disposition
            {
                SerializedItem = unit,
                Customer = customer,
                DispositionDate = unit.CreatedDate.AddDays(2 + i),
                Type = isTransfer ? DispositionType.Transferred : DispositionType.Sold,
                Notes = isTransfer ? "Interstate transfer, distributor-to-dealer." : string.Empty,
            });
        }

        return dispositions;
    }
}
