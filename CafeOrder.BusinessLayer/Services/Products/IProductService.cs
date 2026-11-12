using CafeOrder.DtoLayer.Dtos.ProductDtos;
using CafeOrder.DtoLayer.Dtos.ProductOptionDtos;

namespace CafeOrder.BusinessLayer.Services.Products;

public interface IProductService
{
    // Müşteri sayfaları
    Task<List<ResultProductDto>> GetMenuAsync(string? category, string? search);
    Task<List<ResultProductDto>> GetFeaturedAsync(int count);
    Task<ResultProductDto?> GetActiveByIdAsync(int id);
    Task<List<ResultProductDto>> GetRelatedAsync(int productId, int categoryId, int count);
    Task<List<ResultProductOptionDto>> GetOptionsAsync();

    // Admin
    Task<List<ResultProductDto>> GetAllAsync();
    Task<ResultProductDto> GetByIdAsync(int id);
    Task CreateAsync(CreateProductDto dto);
    Task UpdateAsync(UpdateProductDto dto);
    Task ChangeStatusAsync(int id, bool isActive);
    Task DeleteAsync(int id);
}
