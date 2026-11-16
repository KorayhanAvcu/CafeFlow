using AutoMapper;
using CafeOrder.BusinessLayer.Exceptions;
using CafeOrder.DataAccessLayer.Repositories.Categories;
using CafeOrder.DataAccessLayer.Repositories.Orders;
using CafeOrder.DataAccessLayer.Repositories.ProductOptions;
using CafeOrder.DataAccessLayer.Repositories.Products;
using CafeOrder.DataAccessLayer.UnitOfWork;
using CafeOrder.DtoLayer.Dtos.ProductDtos;
using CafeOrder.DtoLayer.Dtos.ProductOptionDtos;
using CafeOrder.EntityLayer.Entities;

namespace CafeOrder.BusinessLayer.Services.Products;

/// <summary>Ürün işleri: müşteri menüsü ve admin ürün yönetimi.</summary>
public class ProductService : IProductService
{
    /// <summary>Ürüne verilebilecek rozetler (admin formundaki liste).</summary>
    public static readonly IReadOnlyList<string> Badges = new[] { "Çok Satan", "Yeni", "Popüler", "Şefin Seçimi" };

    private readonly IProductRepository _productRepository;
    private readonly ICategoryRepository _categoryRepository;
    private readonly IProductOptionRepository _productOptionRepository;
    private readonly IOrderRepository _orderRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public ProductService(
        IProductRepository productRepository,
        ICategoryRepository categoryRepository,
        IProductOptionRepository productOptionRepository,
        IOrderRepository orderRepository,
        IUnitOfWork unitOfWork,
        IMapper mapper)
    {
        _productRepository = productRepository;
        _categoryRepository = categoryRepository;
        _productOptionRepository = productOptionRepository;
        _orderRepository = orderRepository;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    // ---------------- Müşteri sayfaları ----------------

    /// <summary>Menü sayfası: kategori (slug) ve arama filtresi.</summary>
    public async Task<List<ResultProductDto>> GetMenuAsync(string? category, string? search)
    {
        var products = await _productRepository.GetMenuAsync(category, search);

        return _mapper.Map<List<ResultProductDto>>(products);
    }

    /// <summary>Ana sayfadaki öne çıkanlar: rozeti olan ürünler.</summary>
    public async Task<List<ResultProductDto>> GetFeaturedAsync(int count)
    {
        var products = await _productRepository.GetFeaturedAsync(count);

        return _mapper.Map<List<ResultProductDto>>(products);
    }

    /// <summary>Menüde görünen bir ürün; pasifse veya yoksa null.</summary>
    public async Task<ResultProductDto?> GetActiveByIdAsync(int id)
    {
        var product = await _productRepository.GetActiveByIdAsync(id);
        if (product == null)
        {
            return null;
        }
        return _mapper.Map<ResultProductDto>(product);
    }

    /// <summary>Ürün detayındaki "Pair it with": aynı kategoriden diğer ürünler.</summary>
    public async Task<List<ResultProductDto>> GetRelatedAsync(int productId, int categoryId, int count)
    {
        var products = await _productRepository.GetRelatedAsync(productId, categoryId, count);

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
        var products = await _productRepository.GetAdminListAsync(null, null);
        return _mapper.Map<List<ResultProductDto>>(products);
    }

    /// <summary>Ürün sayısı (SQL COUNT; admin menüsündeki rozet).</summary>
    public async Task<int> CountAsync()
    {
        return await _productRepository.CountAsync();
    }

    /// <summary>Admin ürün listesi: kategori Id'si ve ada göre filtreler (filtre SQL'de uygulanır).</summary>
    public async Task<List<ResultProductDto>> GetAdminListAsync(int? categoryId, string? search)
    {
        var products = await _productRepository.GetAdminListAsync(categoryId, search);
        return _mapper.Map<List<ResultProductDto>>(products);
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
        await _productRepository.AddAsync(product);
        await _unitOfWork.SaveChangesAsync();
    }

    /// <summary>Formdan gelen değerleri veritabanındaki kayda kopyalar ve kaydeder.</summary>
    public async Task UpdateAsync(UpdateProductDto dto)
    {
        var product = await GetEntityAsync(dto.Id);

        // DTO'daki alanları var olan kaydın üstüne yaz.
        _mapper.Map(dto, product);

        await CheckAsync(product);
        await _unitOfWork.SaveChangesAsync();
    }

    /// <summary>Ürünün aktif/pasif durumunu değiştirip kaydeder.</summary>
    public async Task ChangeStatusAsync(int id, bool isActive)
    {
        var product = await GetEntityAsync(id);
        product.IsActive = isActive;
        await _unitOfWork.SaveChangesAsync();
    }

    /// <summary>Geçmiş siparişte geçen ürün silinmez; eski siparişler bozulmasın.</summary>
    public async Task DeleteAsync(int id)
    {
        var product = await GetEntityAsync(id);

        var usedInOrders = await _orderRepository.AnyContainingProductAsync(id);
        if (usedInOrders)
        {
            throw new BusinessException(product.Name + " geçmiş siparişlerde var, silinemez. Bunun yerine pasif yapın.");
        }

        _productRepository.Remove(product);
        await _unitOfWork.SaveChangesAsync();
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
