using System;
using Volo.Abp;
using Volo.Abp.Domain.Entities.Auditing;

namespace SmartPantry.Products;

public class Product : FullAuditedAggregateRoot<Guid>
{
    public string Barcode { get; private set; }
    public string Name { get; private set; }

    protected Product()
    {
        // Requerido por EF Core
    }

    public Product(Guid id, string barcode, string name) : base(id)
    {
        SetBarcode(barcode);
        SetName(name);
    }

    public void SetBarcode(string barcode)
    {
        Barcode = Check.NotNullOrWhiteSpace(
            barcode, nameof(barcode), ProductConsts.MaxBarcodeLength
        ).Trim();
    }

    public void SetName(string name)
    {
        Name = Check.NotNullOrWhiteSpace(
            name, nameof(name), ProductConsts.MaxNameLength
        ).Trim();
    }
}