using CafeOrder.BusinessLayer.Exceptions;
using CafeOrder.BusinessLayer.Pricing;
using CafeOrder.BusinessLayer.Services.Orders;
using CafeOrder.BusinessLayer.Services.Products;
using CafeOrder.BusinessLayer.Services.Tables;
using CafeOrder.DtoLayer.Dtos.OrderDtos;
using CafeOrder.DtoLayer.Dtos.ProductDtos;
using CafeOrder.EntityLayer.Enums;

namespace CafeOrder.BusinessLayer.Services.Cart;

/// <summary>
/// Sepet kuralları: ekleme, adet değiştirme, sepet sayfasını hazırlama ve sipariş verme.
/// Fiyat burada da tutulmaz; her seferinde OrderService hesaplar.
/// </summary>
public class CartService : ICartService
{
    private const int MaxQuantity = 20;

    private readonly IProductService _productService;
    private readonly IOrderService _orderService;
    private readonly ITableService _tableService;
    private readonly PricingOptions _pricing;

    public CartService(IProductService productService, IOrderService orderService, ITableService tableService, PricingOptions pricing)
    {
        _productService = productService;
        _orderService = orderService;
        _tableService = tableService;
        _pricing = pricing;
    }

    /// <summary>
    /// Ürünü sepete ekler. Özelleştirilemeyen üründe gelen seçenekleri atar; seçenekleri sıralar ki
    /// aynı seçim farklı sırayla gelse de aynı satır sayılsın. Aynı ürün + aynı seçenekler zaten varsa
    /// adedini artırır, yoksa yeni satır açar. Adet 1 ile 20 arasına sıkıştırılır.
    /// </summary>
    public async Task<string> AddItemAsync(List<CartItemDto> cart, AddToCartDto dto)
    {
        var product = await _productService.GetActiveByIdAsync(dto.ProductId);
        if (product == null)
        {
            throw new BusinessException("Ürün bulunamadı ya da artık satışta değil.");
        }

        // Seçilen seçeneklerin Id'lerini tek listede topla (boyut, süt, şurup tek değer; ekstra shot liste).
        List<int> options = new List<int>();
        if (product.IsCustomizable)
        {
            options.AddRange(dto.OptionIds);
            if (dto.SizeId != null) options.Add(dto.SizeId.Value);
            if (dto.MilkId != null) options.Add(dto.MilkId.Value);
            if (dto.SyrupId != null) options.Add(dto.SyrupId.Value);
            options.Sort();
        }

        int quantity = Math.Clamp(dto.Quantity, 1, MaxQuantity);

        CartItemDto? sameLine = cart.FirstOrDefault(item => item.ProductId == dto.ProductId && item.OptionIds.SequenceEqual(options));
        if (sameLine != null)
        {
            sameLine.Quantity = Math.Min(MaxQuantity, sameLine.Quantity + quantity);
        }
        else
        {
            CartItemDto newLine = new CartItemDto();
            newLine.ProductId = dto.ProductId;
            newLine.OptionIds = options;
            newLine.Quantity = quantity;
            cart.Add(newLine);
        }

        return product.Name;
    }

    /// <summary>Sepetteki index'inci satırın adedini değiştirir (en fazla 20); adet 0 veya altıysa satırı siler. Geçersiz index'i yok sayar.</summary>
    public void UpdateQuantity(List<CartItemDto> cart, int index, int quantity)
    {
        if (index < 0 || index >= cart.Count)
        {
            return;
        }
        if (quantity <= 0)
        {
            cart.RemoveAt(index);
        }
        else
        {
            cart[index].Quantity = Math.Min(MaxQuantity, quantity);
        }
    }

    /// <summary>
    /// Sepet sayfasının bütün verisi. Sepet seçilen türe göre fiyatlandırılır (tür seçilmediyse ücretsiz kanal gibi);
    /// silinmiş ürünler sepetten çıkarılır (controller temizlenmiş listeyi Session'a geri yazar);
    /// QR kodu tekrar doğrulanır; formda yazılanlar ve hata mesajı sayfaya geri verilir.
    /// </summary>
    public async Task<ResultCartDto> GetCartAsync(List<CartItemDto> cart, OrderChannel? channel, string? qrToken, CreateOrderDto? form, string? error)
    {
        ResultCartDto result = new ResultCartDto();
        result.Order = await _orderService.CalculateAsync(channel ?? OrderChannel.Pickup, cart);
        result.Items = cart.Where(item => result.Order.Details.Any(d => d.ProductId == item.ProductId)).ToList();
        result.Channel = channel;
        result.Table = await _tableService.GetByQrAsync(qrToken);
        result.Form = form ?? new CreateOrderDto();
        result.Error = error;
        result.FreeDeliveryFrom = _pricing.FreeDeliveryFrom;
        return result;
    }

    /// <summary>
    /// Siparişi verir. Sipariş türü formdan değil Session'dan gelir (güvenlik için); seçilmemişse en sıkı kuralları olan
    /// Teslimat kabul edilir. Asıl iş OrderService.PlaceOrderAsync'te: kural bozulursa BusinessException fırlar.
    /// </summary>
    public async Task<ResultOrderDto> CheckoutAsync(List<CartItemDto> cart, OrderChannel? channel, string? qrToken, CreateOrderDto form)
    {
        form.Channel = channel ?? OrderChannel.Delivery;
        return await _orderService.PlaceOrderAsync(form, cart, qrToken);
    }
}
