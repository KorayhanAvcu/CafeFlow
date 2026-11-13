using CafeOrder.DtoLayer.Dtos.CustomerDtos;

namespace CafeOrder.BusinessLayer.Services.Customers;

public interface ICustomerService
{
    /// <summary>Müşteri listesi; search verilirse ad veya telefonda arar. Sipariş sayısı ve harcama hesaplanmış gelir.</summary>
    Task<List<ResultCustomerDto>> GetAllAsync(string? search);
}
