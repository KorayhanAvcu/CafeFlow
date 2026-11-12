namespace CafeOrder.DtoLayer.Dtos.TableDtos;

/// <summary>Yeni masa formundan gelen veri. QR kodu sunucuda üretilir.</summary>
public class CreateTableDto
{
    public int Number { get; set; }
    public string Area { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
}
