using AutoMapper;
using CafeOrder.DataAccessLayer.Repositories.Customers;
using CafeOrder.DtoLayer.Dtos.CustomerDtos;

namespace CafeOrder.BusinessLayer.Services.Customers;

/// <summary>Müşteriler sipariş verirken otomatik oluşur. Admin sadece listeler.</summary>
public class CustomerService : ICustomerService
{
    private readonly ICustomerRepository _customerRepository;
    private readonly IMapper _mapper;

    public CustomerService(ICustomerRepository customerRepository, IMapper mapper)
    {
        _customerRepository = customerRepository;
        _mapper = mapper;
    }

    /// <summary>Sipariş sayısı, toplam harcama ve son sipariş tarihi GeneralMapping'de hesaplanır.</summary>
    public async Task<List<ResultCustomerDto>> GetAllAsync(string? search)
    {
        var customers = await _customerRepository.SearchWithOrdersAsync(search);
        return _mapper.Map<List<ResultCustomerDto>>(customers);
    }
}
