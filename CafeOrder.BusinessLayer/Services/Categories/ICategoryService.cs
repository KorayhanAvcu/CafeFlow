using CafeOrder.DtoLayer.Dtos.CategoryDtos;

namespace CafeOrder.BusinessLayer.Services.Categories;

public interface ICategoryService
{
    Task<List<ResultCategoryDto>> GetAllAsync();
    Task<List<ResultCategoryDto>> GetActiveAsync();
    Task<ResultCategoryDto> GetByIdAsync(int id);
    Task CreateAsync(CreateCategoryDto dto);
    Task UpdateAsync(UpdateCategoryDto dto);
    Task ChangeStatusAsync(int id, bool isActive);
    Task DeleteAsync(int id);
}
