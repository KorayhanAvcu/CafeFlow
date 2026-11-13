using MapsterMapper;
using CafeOrder.BusinessLayer.Exceptions;
using CafeOrder.DataAccessLayer.Repositories.Tables;
using CafeOrder.DtoLayer.Dtos.TableDtos;
using CafeOrder.EntityLayer.Entities;
using Microsoft.EntityFrameworkCore;

namespace CafeOrder.BusinessLayer.Services.Tables;

/// <summary>Masa ve QR kodu işleri.</summary>
public class TableService : ITableService
{
    private readonly ITableRepository _tableRepository;
    private readonly IMapper _mapper;

    public TableService(ITableRepository tableRepository, IMapper mapper)
    {
        _tableRepository = tableRepository;
        _mapper = mapper;
    }

    /// <summary>Bütün masaları siparişleriyle (sayı için) masa numarasına göre getirir.</summary>
    public async Task<List<ResultTableDto>> GetAllAsync()
    {
        var tables = await _tableRepository.GetQueryable()
            .Include(t => t.Orders)
            .OrderBy(t => t.Number)
            .ToListAsync();

        return _mapper.Map<List<ResultTableDto>>(tables);
    }


    /// <summary>
    /// Masa sayfasının sağındaki form. editId verilirse o masanın dolu formu döner;
    /// verilmezse yeni masa formu döner: Id = 0, aktif, numara = en büyük masa numarası + 1 (hiç masa yoksa 1).
    /// </summary>
    public async Task<UpdateTableDto> GetFormAsync(int? editId)
    {
        if (editId != null)
        {
            var table = await GetEntityAsync(editId.Value);
            return _mapper.Map<UpdateTableDto>(table);
        }

        var anyTable = await _tableRepository.AnyAsync(t => true);
        UpdateTableDto form = new UpdateTableDto();
        form.IsActive = true;
        form.Number = anyTable ? await _tableRepository.GetQueryable().MaxAsync(t => t.Number) + 1 : 1;
        return form;
    }

    /// <summary>Formun kaydet düğmesi: Id 0 ise yeni masa olarak ekler, değilse var olanı günceller.</summary>
    public async Task SaveAsync(UpdateTableDto dto)
    {
        if (dto.Id == 0)
        {
            await CreateAsync(_mapper.Map<CreateTableDto>(dto));
        }
        else
        {
            await UpdateAsync(dto);
        }
    }

    /// <summary>Okutulan QR kodu aktif bir masaya ait mi? Bulamazsa null döner.</summary>
    public async Task<ResultTableDto?> GetByQrAsync(string? qrToken)
    {
        if (string.IsNullOrWhiteSpace(qrToken))
        {
            return null;
        }

        string token = qrToken.Trim().ToUpper();
        var table = await _tableRepository.GetFirstAsync(t => t.QrToken == token && t.IsActive);
        if (table == null)
        {
            return null;
        }
        return _mapper.Map<ResultTableDto>(table);
    }

    /// <summary>Yeni masa ekler: kuralları kontrol eder, rastgele QR kodu üretir, kaydeder.</summary>
    public async Task CreateAsync(CreateTableDto dto)
    {
        // DTO → Entity
        Table table = _mapper.Map<Table>(dto);
        await CheckAsync(table);
        table.QrToken = await NewQrAsync(table.Number);
        await _tableRepository.CreateAsync(table);
    }

    /// <summary>Masa numarası değişirse eski QR kart çalışmasın diye yeni kod üretilir.</summary>
    public async Task UpdateAsync(UpdateTableDto dto)
    {
        var table = await GetEntityAsync(dto.Id);
        int oldNumber = table.Number;

        // DTO'daki alanları var olan kaydın üstüne yaz (QrToken DTO'da yok, değişmez).
        _mapper.Map(dto, table);

        await CheckAsync(table);
        if (table.Number != oldNumber)
        {
            table.QrToken = await NewQrAsync(table.Number);
        }

        await _tableRepository.UpdateAsync(table);
    }

    /// <summary>Kart kaybolduğunda yeni QR kodu üretir.</summary>
    public async Task RegenerateQrAsync(int id)
    {
        var table = await GetEntityAsync(id);
        table.QrToken = await NewQrAsync(table.Number);
        await _tableRepository.UpdateAsync(table);
    }

    /// <summary>Masanın aktif/pasif durumunu değiştirip kaydeder.</summary>
    public async Task ChangeStatusAsync(int id, bool isActive)
    {
        var table = await GetEntityAsync(id);
        table.IsActive = isActive;
        await _tableRepository.UpdateAsync(table);
    }

    /// <summary>Siparişi olan masa silinemez.</summary>
    public async Task DeleteAsync(int id)
    {
        var table = await _tableRepository.GetQueryable()
            .Include(t => t.Orders)
            .FirstOrDefaultAsync(t => t.Id == id);
        if (table == null)
        {
            throw new BusinessException("Masa bulunamadı.");
        }
        if (table.Orders.Count > 0)
        {
            throw new BusinessException("Masa " + table.Number + " geçmiş siparişlerde var, silinemez. Bunun yerine pasif yapın.");
        }

        await _tableRepository.DeleteAsync(table);
    }

    /// <summary>Veritabanındaki kaydı getirir; yoksa hata verir.</summary>
    private async Task<Table> GetEntityAsync(int id)
    {
        var table = await _tableRepository.GetByIdAsync(id);
        if (table == null)
        {
            throw new BusinessException("Table not found.");
        }
        return table;
    }

    /// <summary>Kaydetmeden önceki kontroller.</summary>
    private async Task CheckAsync(Table table)
    {
        table.Area = (table.Area ?? "").Trim();

        if (table.Number < 1 || table.Number > 999)
        {
            throw new BusinessException("Masa numarası 1 ile 999 arasında olmalı.");
        }
        if (table.Area.Length == 0 || table.Area.Length > 50)
        {
            throw new BusinessException("Lütfen bir bölge girin (en fazla 50 karakter).");
        }

        var numberUsed = await _tableRepository.AnyAsync(t => t.Number == table.Number && t.Id != table.Id);
        if (numberUsed)
        {
            throw new BusinessException("Masa " + table.Number + " zaten var.");
        }
    }

    /// <summary>CF-T07-ABC123 gibi rastgele bir QR kodu üretir. Karışmasın diye 0, O, 1, I harfleri yok.</summary>
    private async Task<string> NewQrAsync(int number)
    {
        const string letters = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";

        while (true)
        {
            string random = "";
            for (int i = 0; i < 6; i++)
            {
                int index = Random.Shared.Next(letters.Length);
                random += letters[index];
            }

            string token = "CF-T" + number.ToString("00") + "-" + random;

            // Aynı kod başka masada yoksa kullan.
            var exists = await _tableRepository.AnyAsync(t => t.QrToken == token);
            if (!exists)
            {
                return token;
            }
        }
    }
}
