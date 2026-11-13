using MapsterMapper;
using CafeOrder.BusinessLayer.Exceptions;
using CafeOrder.DataAccessLayer.Repositories.Categories;
using CafeOrder.DataAccessLayer.Repositories.Orders;
using CafeOrder.DataAccessLayer.Repositories.ProductOptions;
using CafeOrder.DataAccessLayer.Repositories.Products;
using CafeOrder.DtoLayer.Dtos.ProductDtos;
using CafeOrder.DtoLayer.Dtos.ProductOptionDtos;
using CafeOrder.EntityLayer.Entities;
using Microsoft.EntityFrameworkCore;

namespace CafeOrder.BusinessLayer.Services.Products;

/// <summary>Ürün işleri: müşteri menüsü ve admin ürün yönetimi.</summary>
public class ProductService : IProductService
{
    /// <summary>Ürüne verilebilecek rozetler (admin formundaki liste).</summary>
    public static readonly List<string> Badges = new List<string> { "Çok Satan", "Yeni", "Popüler", "Şefin Seçimi" };

    private readonly IProductRepository _productRepository;
    private readonly ICategoryRepository _categoryRepository;
    private readonly IProductOptionRepository _productOptionRepository;
    private readonly IOrderRepository _orderRepository;
    private readonly IMapper _mapper;

    public ProductService(
        IProductRepository productRepository,
        ICategoryRepository categoryRepository,
        IProductOptionRepository productOptionRepository,
        IOrderRepository orderRepository,
        IMapper mapper)
    {
        _productRepository = productRepository;
        _categoryRepository = categoryRepository;
        _productOptionRepository = productOptionRepository;
        _orderRepository = orderRepository;
        _mapper = mapper;
    }

    // ---------------- Müşteri sayfaları ----------------

    /// <summary>Menüde görünen ürünler: ürün de kategorisi de aktif olmalı.</summary>
    private IQueryable<Product> ActiveProducts()
    {
        return _productRepository.GetQueryable()
            .Include(p => p.Category)
            .Where(p => p.IsActive && p.Category.IsActive);
    }

    /// <summary>Menü sayfası: kategori (slug) ve arama filtresi.</summary>
    public async Task<List<ResultProductDto>> GetMenuAsync(string? category, string? search)
    {
        IQueryable<Product> query = ActiveProducts();

        if (!string.IsNullOrEmpty(category))
        {
            query = query.Where(p => p.Category.Slug == category);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(p => p.Name.Contains(search) || p.Category.Name.Contains(search));
        }

        var products = await query
            .OrderBy(p => p.Category.DisplayOrder)
            .ThenBy(p => p.Id)
            .ToListAsync();

        return _mapper.Map<List<ResultProductDto>>(products);
    }

    /// <summary>Ana sayfadaki öne çıkanlar: rozeti olan ürünler.</summary>
    public async Task<List<ResultProductDto>> GetFeaturedAsync(int count)
    {
        var products = await ActiveProducts()
            .Where(p => p.Badge != null)
            .OrderBy(p => p.Category.DisplayOrder)
            .ThenBy(p => p.Id)
            .Take(count)
            .ToListAsync();

        return _mapper.Map<List<ResultProductDto>>(products);
    }

    /// <summary>Menüde görünen bir ürün; pasifse veya yoksa null.</summary>
    public async Task<ResultProductDto?> GetActiveByIdAsync(int id)
    {
        var product = await ActiveProducts().FirstOrDefaultAsync(p => p.Id == id);
        if (product == null)
        {
            return null;
        }
        return _mapper.Map<ResultProductDto>(product);
    }

    /// <summary>Ürün detayındaki "Pair it with": aynı kategoriden diğer ürünler.</summary>
    public async Task<List<ResultProductDto>> GetRelatedAsync(int productId, int categoryId, int count)
    {
        var products = await ActiveProducts()
            .Where(p => p.CategoryId == categoryId && p.Id != productId)
            .OrderBy(p => p.Id)
            .Take(count)
            .ToListAsync();

        return _mapper.Map<List<ResultProductDto>>(products);
    }

    /// <summary>Boyut, süt, shot, şurup seçenekleri.</summary>
    public async Task<List<ResultProductOptionDto>> GetOptionsAsync()
    {
        var options = await _productOptionRepository.GetAllAsync();
        return _mapper.Map<List<ResultProductOptionDto>>(options);
    }

    // ---------------- Admin ----------------

    /// <summary>Admin listesi: bütün ürünleri kategorileriyle, kategori sırası ve ada göre getirir.</summary>
    public async Task<List<ResultProductDto>> GetAllAsync()
    {
        var products = await _productRepository.GetQueryable()
            .Include(p => p.Category)
            .OrderBy(p => p.Category.DisplayOrder)
            .ThenBy(p => p.Name)
            .ToListAsync();

        return _mapper.Map<List<ResultProductDto>>(products);
    }

    /// <summary>Admin ürün listesi: bütün ürünleri getirir, kategori Id'si ve ada göre (büyük/küçük harf fark etmeden) filtreler.</summary>
    public async Task<List<ResultProductDto>> GetAdminListAsync(int? categoryId, string? search)
    {
        var products = await GetAllAsync();
        if (categoryId != null)
        {
            products = products.Where(p => p.CategoryId == categoryId).ToList();
        }
        if (!string.IsNullOrWhiteSpace(search))
        {
            products = products.Where(p => p.Name.Contains(search, StringComparison.CurrentCultureIgnoreCase)).ToList();
        }
        return products;
    }


    /// <summary>
    /// Admin formu için veri. id null ise boş form döner (Id = 0 → "yeni kayıt", aktif işaretli);
    /// değilse o ürünün değerleriyle dolu form döner.
    /// </summary>
    public async Task<UpdateProductDto> GetFormAsync(int? id)
    {
        if (id == null)
        {
            UpdateProductDto form = new UpdateProductDto();
            form.IsActive = true;
            return form;
        }
        var product = await GetEntityAsync(id.Value);
        return _mapper.Map<UpdateProductDto>(product);
    }

    /// <summary>Formun kaydet düğmesi: Id 0 ise yeni ürün olarak ekler, değilse var olanı günceller.</summary>
    public async Task SaveAsync(UpdateProductDto dto)
    {
        if (dto.Id == 0)
        {
            await CreateAsync(_mapper.Map<CreateProductDto>(dto));
        }
        else
        {
            await UpdateAsync(dto);
        }
    }

    /// <summary>Yeni ürün ekler: DTO'yu entity'ye çevirir, kuralları kontrol eder, kaydeder.</summary>
    public async Task CreateAsync(CreateProductDto dto)
    {
        // DTO → Entity
        Product product = _mapper.Map<Product>(dto);
        await CheckAsync(product);
        await _productRepository.CreateAsync(product);
    }

    /// <summary>Formdan gelen değerleri veritabanındaki kayda kopyalar ve kaydeder.</summary>
    public async Task UpdateAsync(UpdateProductDto dto)
    {
        var product = await GetEntityAsync(dto.Id);

        // DTO'daki alanları var olan kaydın üstüne yaz.
        _mapper.Map(dto, product);

        await CheckAsync(product);
        await _productRepository.UpdateAsync(product);
    }

    /// <summary>Ürünün aktif/pasif durumunu değiştirip kaydeder.</summary>
    public async Task ChangeStatusAsync(int id, bool isActive)
    {
        var product = await GetEntityAsync(id);
        product.IsActive = isActive;
        await _productRepository.UpdateAsync(product);
    }

    /// <summary>Geçmiş siparişte geçen ürün silinmez; eski siparişler bozulmasın.</summary>
    public async Task DeleteAsync(int id)
    {
        var product = await GetEntityAsync(id);

        var usedInOrders = await _orderRepository.GetQueryable()
            .AnyAsync(o => o.Details.Any(d => d.ProductId == id));
        if (usedInOrders)
        {
            throw new BusinessException(product.Name + " geçmiş siparişlerde var, silinemez. Bunun yerine pasif yapın.");
        }

        await _productRepository.DeleteAsync(product);
    }

    /// <summary>Veritabanındaki kaydı getirir; yoksa hata verir.</summary>
    private async Task<Product> GetEntityAsync(int id)
    {
        var product = await _productRepository.GetByIdAsync(id);
        if (product == null)
        {
            throw new BusinessException("Ürün bulunamadı.");
        }
        return product;
    }

    /// <summary>Kaydetmeden önceki kontroller.</summary>
    private async Task CheckAsync(Product product)
    {
        product.Name = (product.Name ?? "").Trim();
        product.Description = CleanText(product.Description);
        product.ImageUrl = CleanText(product.ImageUrl);
        product.Badge = CleanText(product.Badge);

        if (product.Name.Length < 2 || product.Name.Length > 150)
        {
            throw new BusinessException("Ürün adı 2-150 karakter olmalı.");
        }
        if (product.BasePrice <= 0 || product.BasePrice > 10000)
        {
            throw new BusinessException("Fiyat 0'dan büyük ve en fazla 10.000 ₺ olmalı.");
        }
        if (product.Badge != null && !Badges.Contains(product.Badge))
        {
            throw new BusinessException("Lütfen listedeki rozetlerden birini seçin.");
        }
        if (product.Description != null && product.Description.Length > 500)
        {
            throw new BusinessException("Açıklama çok uzun (en fazla 500 karakter).");
        }

        var categoryExists = await _categoryRepository.AnyAsync(c => c.Id == product.CategoryId);
        if (!categoryExists)
        {
            throw new BusinessException("Lütfen bir kategori seçin.");
        }

        var nameUsed = await _productRepository.AnyAsync(p => p.Name == product.Name && p.Id != product.Id);
        if (nameUsed)
        {
            throw new BusinessException("\"" + product.Name + "\" adında bir ürün zaten var.");
        }
    }

    /// <summary>Boş metni null yapar, doluysa baştaki/sondaki boşlukları siler.</summary>
    private static string? CleanText(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return null;
        }
        return text.Trim();
    }
}
