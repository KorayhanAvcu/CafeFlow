using CafeOrder.EntityLayer.Enums;

namespace CafeOrder.DtoLayer.Dtos.OrderDtos;

/// <summary>Admin panel (dashboard) sayfasının bütün verisi. DashboardService.GetDashboardAsync hesaplar.</summary>
public class ResultDashboardDto
{
    public int TodayOrders { get; set; }
    /// <summary>Bugünkü ciro (iptaller hariç).</summary>
    public decimal TodayRevenue { get; set; }
    /// <summary>Son 7 günün cirosu (iptaller hariç).</summary>
    public decimal WeekRevenue { get; set; }
    /// <summary>Alındı, Hazırlanıyor veya Hazır durumundaki siparişler.</summary>
    public int OpenOrders { get; set; }
    public Dictionary<OrderStatus, int> StatusCounts { get; set; } = new Dictionary<OrderStatus, int>();
    public List<ResultBestSellerDto> BestSellers { get; set; } = new List<ResultBestSellerDto>();
    /// <summary>Son 6 sipariş.</summary>
    public List<ResultOrderDto> LatestOrders { get; set; } = new List<ResultOrderDto>();
}
