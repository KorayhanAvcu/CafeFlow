using CafeOrder.BusinessLayer.States;
using CafeOrder.DataAccessLayer.Repositories.Orders;
using CafeOrder.DtoLayer.Dtos.OrderDtos;
using CafeOrder.EntityLayer.Enums;
using MapsterMapper;

namespace CafeOrder.BusinessLayer.Services.Orders;

/// <summary>
/// Sipariş okuma işleri. Filtreleme, sayma ve sayfalama SQL'de yapılır;
/// bütün siparişler belleğe çekilmez.
/// </summary>
public class OrderQueryService : IOrderQueryService
{
    private const int PageSize = 10;

    private readonly IOrderRepository _orderRepository;
    private readonly IMapper _mapper;

    public OrderQueryService(IOrderRepository orderRepository, IMapper mapper)
    {
        _orderRepository = orderRepository;
        _mapper = mapper;
    }

    /// <summary>
    /// Admin sipariş listesi. Üstteki kartlar için her durumdaki sayı (filtreden önce), sonra
    /// durum, kanal ve aramaya (sipariş no, müşteri adı, telefon) göre filtreli, 10'arlı bir sayfa.
    /// </summary>
    public async Task<ResultOrderListDto> GetAdminListAsync(OrderFilterDto filter)
    {
        ResultOrderListDto result = new ResultOrderListDto();
        result.Filter = filter;
        result.StatusCounts = await GetStatusCountsAsync();

        // Sayfalama: 23 sipariş → 3 sayfa. Sayfa numarası 1 ile son sayfa arasına sıkıştırılır.
        result.Total = await _orderRepository.CountFilteredAsync(filter.Status, filter.Channel, filter.Q);
        result.TotalPages = Math.Max(1, (int)Math.Ceiling(result.Total / (double)PageSize));
        filter.Page = Math.Clamp(filter.Page, 1, result.TotalPages);

        var orders = await _orderRepository.GetFilteredPageAsync(
            filter.Status, filter.Channel, filter.Q, (filter.Page - 1) * PageSize, PageSize);
        result.Orders = _mapper.Map<List<ResultOrderDto>>(orders);
        return result;
    }

    /// <summary>Siparişi Id ile, kalemleri/seçenekleri/geçmişiyle birlikte getirir ve DTO'ya çevirir. Yoksa null.</summary>
    public async Task<ResultOrderDto?> GetByIdAsync(int id)
    {
        var order = await _orderRepository.GetWithDetailsAsync(id);
        if (order == null)
        {
            return null;
        }
        ResultOrderDto dto = _mapper.Map<ResultOrderDto>(order);

        // STATE: bu durumdan hangi durumlara geçilebilir? Admin sayfası sadece bunların düğmesini gösterir.
        dto.AllowedNextStatuses = OrderState.For(order.Status).GetAllowedNext();
        return dto;
    }

    /// <summary>Siparişi sipariş numarasıyla getirir (müşterinin onay sayfası). Yoksa null.</summary>
    public async Task<ResultOrderDto?> GetByOrderNoAsync(string orderNo)
    {
        var order = await _orderRepository.GetWithDetailsByOrderNoAsync(orderNo);
        if (order == null)
        {
            return null;
        }
        return _mapper.Map<ResultOrderDto>(order);
    }

    /// <summary>Verilen durumdaki sipariş sayısını SQL COUNT ile döndürür.</summary>
    public async Task<int> CountByStatusAsync(OrderStatus status)
    {
        return await _orderRepository.CountAsync(o => o.Status == status);
    }

    /// <summary>Her durumun sipariş sayısı (tek SQL GROUP BY); siparişi olmayan durumlar 0 olarak eklenir.</summary>
    public async Task<Dictionary<OrderStatus, int>> GetStatusCountsAsync()
    {
        var counts = await _orderRepository.GetStatusCountsAsync();
        Dictionary<OrderStatus, int> result = new Dictionary<OrderStatus, int>();
        foreach (OrderStatus status in Enum.GetValues<OrderStatus>())
        {
            result[status] = counts.GetValueOrDefault(status);
        }
        return result;
    }
}
