using CafeOrder.BusinessLayer.Services.Orders;
using CafeOrder.DataAccessLayer.Repositories.Orders;
using CafeOrder.DtoLayer.Dtos.OrderDtos;
using CafeOrder.EntityLayer.Enums;
using AutoMapper;

namespace CafeOrder.BusinessLayer.Services.Dashboard;

/// <summary>
/// Admin panel rakamları. Her rakam SQL'de hesaplanır (COUNT, SUM, GROUP BY); bütün siparişler belleğe çekilmez.
/// İptal edilen siparişler ciroya ve en çok satanlara katılmaz.
/// </summary>
public class DashboardService : IDashboardService
{
    private const int BestSellerCount = 5;
    private const int LatestOrderCount = 6;

    private readonly IOrderRepository _orderRepository;
    private readonly IOrderQueryService _orderQueryService;
    private readonly TimeProvider _time;
    private readonly IMapper _mapper;

    public DashboardService(IOrderRepository orderRepository, IOrderQueryService orderQueryService, TimeProvider time, IMapper mapper)
    {
        _orderRepository = orderRepository;
        _orderQueryService = orderQueryService;
        _time = time;
        _mapper = mapper;
    }

    /// <summary>Panel sayfasının bütün rakamları.</summary>
    public async Task<ResultDashboardDto> GetDashboardAsync()
    {
        DateTime today = _time.GetLocalNow().Date;
        DateTime weekStart = today.AddDays(-6);

        ResultDashboardDto dashboard = new ResultDashboardDto();
        dashboard.TodayOrders = await _orderRepository.CountSinceAsync(today);
        dashboard.TodayRevenue = await _orderRepository.GetRevenueSinceAsync(today);
        dashboard.WeekRevenue = await _orderRepository.GetRevenueSinceAsync(weekStart);

        dashboard.StatusCounts = await _orderQueryService.GetStatusCountsAsync();
        dashboard.OpenOrders = dashboard.StatusCounts[OrderStatus.Received]
                             + dashboard.StatusCounts[OrderStatus.Preparing]
                             + dashboard.StatusCounts[OrderStatus.Ready];

        var bestSellers = await _orderRepository.GetBestSellersAsync(BestSellerCount);
        dashboard.BestSellers = bestSellers
            .Select(b => new ResultBestSellerDto { Name = b.ProductName, Quantity = b.Quantity, Revenue = b.Revenue })
            .ToList();

        var latest = await _orderRepository.GetLatestAsync(LatestOrderCount);
        dashboard.LatestOrders = _mapper.Map<List<ResultOrderDto>>(latest);
        return dashboard;
    }
}
