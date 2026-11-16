using CafeOrder.DtoLayer.Dtos.OrderDtos;

namespace CafeOrder.BusinessLayer.Services.Dashboard;

/// <summary>Admin panel (dashboard) sayfasının rakamları.</summary>
public interface IDashboardService
{
    /// <summary>Bugünkü sipariş ve ciro, açık siparişler, son 7 gün, durum sayıları, en çok satanlar, son 6 sipariş.</summary>
    Task<ResultDashboardDto> GetDashboardAsync();
}
