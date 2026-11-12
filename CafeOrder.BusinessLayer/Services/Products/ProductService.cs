using AutoMapper;
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
    public static readonly List<string> Badges = new List<string> { "Best Seller", "New", "Popular", "Chef's Choice" };

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

        List<Product> products = await query
            .OrderBy(p => p.Category.DisplayOrder)
            .ThenBy(p => p.Id)
            .ToListAsync();

        return _mapper.Map<List<ResultProductDto>>(products);
    }

    /// <summary>Ana sayfadaki öne çıkanlar: rozeti olan ürünler.</summary>
    public async Task<List<ResultProductDto>> GetFeaturedAsync(int count)
    {
        List<Product> products = await ActiveProducts()
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
        Product? product = await ActiveProducts().FirstOrDefaultAsync(p => p.Id == id);
        if (product == null)
        {
            return null;
        }
        return _mapper.Map<ResultProductDto>(product);
    }

    /// <summary>Ürün detayındaki "Pair it with": aynı kategoriden diğer ürünler.</summary>
    public async Task<List<ResultProductDto>> GetRelatedAsync(int productId, int categoryId, int count)
    {
        List<Product> products = await ActiveProducts()
            .Where(p => p.CategoryId == categoryId && p.Id != productId)
            .OrderBy(p => p.Id)
            .Take(count)
            .ToListAsync();

        return _mapper.Map<List<ResultProductDto>>(products);
    }

    /// <summary>Boyut, süt, shot, şurup seçenekleri.</summary>
    public async Task<List<ResultProductOptionDto>> GetOptionsAsync()
    {
        List<ProductOption> options = await _productOptionRepository.GetAllAsync();
        return _mapper.Map<List<ResultProductOptionDto>>(options);
    }

    // ---------------- Admin ----------------

    public async Task<List<ResultProductDto>> GetAllAsync()
    {
        List<Product> products = await _productRepository.GetQueryable()
            .Include(p => p.Category)
            .OrderBy(p => p.Category.DisplayOrder)
            .ThenBy(p => p.Name)
            .ToListAsync();

        return _mapper.Map<List<ResultProductDto>>(products);
    }

    public async Task<ResultProductDto> GetByIdAsync(int id)
    {
        Product product = await GetEntityAsync(id);
        return _mapper.Map<ResultProductDto>(product);
    }

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
        Product product = await GetEntityAsync(dto.Id);

        // DTO'daki alanları var olan kaydın üstüne yaz.
        _mapper.Map(dto, product);

        await CheckAsync(product);
        await _productRepository.UpdateAsync(product);
    }

    public async Task ChangeStatusAsync(int id, bool isActive)
    {
        Product product = await GetEntityAsync(id);
        product.IsActive = isActive;
        await _productRepository.UpdateAsync(product);
    }

    /// <summary>Geçmiş siparişte geçen ürün silinmez; eski siparişler bozulmasın.</summary>
    public async Task DeleteAsync(int id)
    {
        Product product = await GetEntityAsync(id);

        bool usedInOrders = await _orderRepository.GetQueryable()
            .AnyAsync(o => o.Details.Any(d => d.ProductId == id));
        if (usedInOrders)
        {
            throw new BusinessException(product.Name + " appears in past orders. Make it inactive instead.");
        }

        await _productRepository.DeleteAsync(product);
    }

    /// <summary>Veritabanındaki kaydı getirir; yoksa hata verir.</summary>
    private async Task<Product> GetEntityAsync(int id)
    {
        Product? product = await _productRepository.GetByIdAsync(id);
        if (product == null)
        {
            throw new BusinessException("Product not found.");
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
            throw new BusinessException("Product name must be 2-150 characters.");
        }
        if (product.BasePrice <= 0 || product.BasePrice > 10000)
        {
            throw new BusinessException("Price must be greater than 0 and at most 10,000 ₺.");
        }
        if (product.Badge != null && !Badges.Contains(product.Badge))
        {
            throw new BusinessException("Please choose one of the listed badges.");
        }
        if (product.Description != null && product.Description.Length > 500)
        {
            throw new BusinessException("The description is too long.");
        }

        bool categoryExists = await _categoryRepository.AnyAsync(c => c.Id == product.CategoryId);
        if (!categoryExists)
        {
            throw new BusinessException("Please choose a category.");
        }

        bool nameUsed = await _productRepository.AnyAsync(p => p.Name == product.Name && p.Id != product.Id);
        if (nameUsed)
        {
            throw new BusinessException("\"" + product.Name + "\" already exists.");
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
