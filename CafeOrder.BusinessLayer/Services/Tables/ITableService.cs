using CafeOrder.DtoLayer.Dtos.TableDtos;

namespace CafeOrder.BusinessLayer.Services.Tables;

public interface ITableService
{
    /// <summary>Bütün masalar, sipariş sayılarıyla.</summary>
    Task<List<ResultTableDto>> GetAllAsync();
    /// <summary>Masa sayfasındaki form: editId verilirse o masanın dolu formu, değilse numarası "en büyük + 1" olan yeni masa formu.</summary>
    Task<UpdateTableDto> GetFormAsync(int? editId);
    /// <summary>Masa formunun kaydet düğmesi: Id 0 ise yeni masa ekler, değilse günceller.</summary>
    Task SaveAsync(UpdateTableDto dto);
    /// <summary>Okutulan QR kodu aktif bir masaya aitse o masayı, değilse null döndürür.</summary>
    Task<ResultTableDto?> GetByQrAsync(string? qrToken);
    /// <summary>Yeni masa ekler ve rastgele QR kodu üretir.</summary>
    Task CreateAsync(CreateTableDto dto);
    /// <summary>Masayı günceller; numara değiştiyse yeni QR kodu üretir.</summary>
    Task UpdateAsync(UpdateTableDto dto);
    /// <summary>Masaya yeni QR kodu üretir; eski kart çalışmaz.</summary>
    Task RegenerateQrAsync(int id);
    /// <summary>Masayı aktif veya pasif yapar (pasif masa QR siparişi almaz).</summary>
    Task ChangeStatusAsync(int id, bool isActive);
    /// <summary>Masayı siler; geçmiş siparişi varsa BusinessException.</summary>
    Task DeleteAsync(int id);
}
