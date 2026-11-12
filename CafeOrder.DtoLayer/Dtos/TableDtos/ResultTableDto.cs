namespace CafeOrder.DtoLayer.Dtos.TableDtos;

/// <summary>Masa listesinde ekrana giden veri.</summary>
public class ResultTableDto
{
    public int Id { get; set; }
    public int Number { get; set; }
    public string Area { get; set; } = string.Empty;
    public string QrToken { get; set; } = string.Empty;
    public bool IsActive { get; set; }

    /// <summary>Bu masadan verilmiş sipariş sayısı.</summary>
    public int OrderCount { get; set; }
}
