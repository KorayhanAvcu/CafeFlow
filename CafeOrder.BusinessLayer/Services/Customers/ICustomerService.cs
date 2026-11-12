using CafeOrder.DtoLayer.Dtos.CustomerDtos;

namespace CafeOrder.BusinessLayer.Services.Customers;

public interface ICustomerService
{
    Task<List<ResultCustomerDto>> GetAllAsync(string? search);
}
