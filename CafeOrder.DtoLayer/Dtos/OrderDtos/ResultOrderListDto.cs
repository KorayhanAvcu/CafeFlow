using CafeOrder.EntityLayer.Enums;

namespace CafeOrder.DtoLayer.Dtos.OrderDtos;

/// <summary>Admin sipariş listesi sayfasının bütün verisi: bu sayfadaki siparişler, sayılar ve sayfalama.</summary>
public class ResultOrderListDto
{
    /// <summary>Bu sayfada gösterilecek siparişler (en fazla 10).</summary>
    public List<ResultOrderDto> Orders { get; set; } = new List<ResultOrderDto>();

    /// <summary>Üstteki kartlar: her durumdaki sipariş sayısı (filtreden önce).</summary>
    public Dictionary<OrderStatus, int> StatusCounts { get; set; } = new Dictionary<OrderStatus, int>();

    /// <summary>Uygulanan filtreler (formu dolu göstermek için).</summary>
    public OrderFilterDto Filter { get; set; } = new OrderFilterDto();

    /// <summary>Filtreye uyan toplam sipariş sayısı.</summary>
    public int Total { get; set; }

    public int TotalPages { get; set; } = 1;
}
