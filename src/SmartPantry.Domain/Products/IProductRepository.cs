using System;
using System.Threading.Tasks;
using Volo.Abp.Domain.Repositories;

namespace SmartPantry.Products;

public interface IProductRepository : IRepository<Product, Guid>
{
    Task<bool> BarcodeExistsAsync(string barcode);
}