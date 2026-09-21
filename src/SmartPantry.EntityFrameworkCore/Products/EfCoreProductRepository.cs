using Microsoft.EntityFrameworkCore;
using SmartPantry.Products;
using System;
using System.Linq;
using System.Threading.Tasks;
using Volo.Abp.Domain.Repositories.EntityFrameworkCore;
using Volo.Abp.EntityFrameworkCore;

namespace SmartPantry.EntityFrameworkCore.Products;

public class EfCoreProductRepository :
    EfCoreRepository<SmartPantryDbContext, Product, Guid>,
    IProductRepository
{
    public EfCoreProductRepository(IDbContextProvider<SmartPantryDbContext> dbContextProvider)
        : base(dbContextProvider)
    {
    }

    public async Task<bool> BarcodeExistsAsync(string barcode)
    {
        var dbSet = await GetDbSetAsync();
        return await dbSet.AnyAsync(p => p.Barcode == barcode);
    }
}