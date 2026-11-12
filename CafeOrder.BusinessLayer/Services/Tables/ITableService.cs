using CafeOrder.DtoLayer.Dtos.TableDtos;

namespace CafeOrder.BusinessLayer.Services.Tables;

public interface ITableService
{
    Task<List<ResultTableDto>> GetAllAsync();
    Task<ResultTableDto> GetByIdAsync(int id);
    Task<ResultTableDto?> GetByQrAsync(string? qrToken);
    Task CreateAsync(CreateTableDto dto);
    Task UpdateAsync(UpdateTableDto dto);
    Task RegenerateQrAsync(int id);
    Task ChangeStatusAsync(int id, bool isActive);
    Task DeleteAsync(int id);
}
