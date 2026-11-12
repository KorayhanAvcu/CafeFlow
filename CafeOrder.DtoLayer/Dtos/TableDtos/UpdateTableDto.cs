namespace CafeOrder.DtoLayer.Dtos.TableDtos;

/// <summary>Masa düzenleme formunun verisi. Id = 0 ise kayıt yeni demektir.</summary>
public class UpdateTableDto
{
    public int Id { get; set; }
    public int Number { get; set; }
    public string Area { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
}
