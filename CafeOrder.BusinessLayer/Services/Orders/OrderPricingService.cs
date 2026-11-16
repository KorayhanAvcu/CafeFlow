using CafeOrder.BusinessLayer.Pricing;
using CafeOrder.BusinessLayer.Pricing.Fees;
using CafeOrder.DataAccessLayer.Repositories.ProductOptions;
using CafeOrder.DataAccessLayer.Repositories.Products;
using CafeOrder.DtoLayer.Dtos.OrderDtos;
using CafeOrder.EntityLayer.Entities;
using CafeOrder.EntityLayer.Enums;
using AutoMapper;

namespace CafeOrder.BusinessLayer.Services.Orders;

/// <summary>
/// Fiyat hesabı. DECORATOR ile birim fiyat (IUnitPriceCalculator), STRATEGY ile kanala göre ücret.
/// Hepsi DI'dan gelir; sipariş kalemleri AutoMapper ile üründen ve seçeneklerden oluşur. Bu sınıfta new yok.
/// </summary>
public class OrderPricingService : IOrderPricingService
{
    private readonly IProductRepository _productRepository;
    private readonly IProductOptionRepository _productOptionRepository;
    private readonly IReadOnlyDictionary<OrderChannel, IDeliveryFeeStrategy> _feeStrategies;
    private readonly IUnitPriceCalculator _unitPrice;
    private readonly IMapper _mapper;

    public OrderPricingService(
        IProductRepository productRepository,
        IProductOptionRepository productOptionRepository,
        IEnumerable<IDeliveryFeeStrategy> feeStrategies,
        IUnitPriceCalculator unitPrice,
        IMapper mapper)
    {
        _productRepository = productRepository;
        _productOptionRepository = productOptionRepository;
        // Aynı kanal için iki strateji kaydedilirse burada hata verir (yanlış kaydı erken yakalar).
        _feeStrategies = feeStrategies.ToDictionary(s => s.Channel);
        _unitPrice = unitPrice;
        _mapper = mapper;
    }

    /// <summary>Sepeti fiyatlandırır, kaydetmez.</summary>
    public async Task<ResultOrderDto> CalculateAsync(OrderChannel channel, List<CartItemDto> items)
    {
        Order order = _mapper.Map<Order>(channel);   // sadece kanalı belli, geçici bir sipariş (kaydedilmez)
        await ApplyPricesAsync(order, items);
        return _mapper.Map<ResultOrderDto>(order);
    }

    /// <summary>
    /// Asıl hesap. Tarayıcıdan gelen fiyat hiç kullanılmaz; fiyatlar veritabanından okunur.
    /// </summary>
    public async Task ApplyPricesAsync(Order order, List<CartItemDto> items)
    {
        // 1) Sepetteki ürünleri ve bütün seçenekleri veritabanından al.
        var products = await _productRepository.GetByIdsWithCategoryAsync(items.Select(i => i.ProductId));
        var allOptions = await _productOptionRepository.GetAllAsync();

        // 2) Her sepet satırı için bir sipariş kalemi oluştur.
        order.Details.Clear();
        foreach (CartItemDto item in items)
        {
            Product? product = products.FirstOrDefault(p => p.Id == item.ProductId);
            if (product == null)
            {
                continue;   // ürün silinmiş: atla
            }

            List<ProductOption> chosenOptions = allOptions.Where(o => item.OptionIds.Contains(o.Id)).ToList();
            decimal unitPrice = _unitPrice.Calculate(product, chosenOptions);   // DECORATOR

            // Kalem üründen oluşur: ProductId, o anki ad ve taban fiyatın kopyası (OrderMappings).
            OrderDetail detail = _mapper.Map<OrderDetail>(product);
            detail.Product = product;   // AYNI izlenen ürün nesnesi bağlanır (kopya olsaydı EF onu yeni ürün sanıp INSERT ederdi)
            detail.UnitPrice = unitPrice;
            detail.Quantity = item.Quantity;
            detail.LineTotal = unitPrice * item.Quantity;

            // Seçeneklerin o anki kopyası (grup, değer, fiyat farkı).
            detail.Options = _mapper.Map<List<OrderDetailOption>>(chosenOptions);

            order.Details.Add(detail);
        }

        // 3) Ara toplam
        order.Subtotal = order.Details.Sum(d => d.LineTotal);

        // 4) STRATEGY: sipariş türüne göre ücret kuralı seçilir.
        order.DeliveryFee = order.Details.Count == 0 ? 0 : GetFeeStrategy(order.Channel).Calculate(order.Subtotal);
        order.Total = order.Subtotal + order.DeliveryFee;
    }

    /// <summary>STRATEGY: kanala kayıtlı ücret stratejisini verir.</summary>
    private IDeliveryFeeStrategy GetFeeStrategy(OrderChannel channel)
    {
        if (_feeStrategies.TryGetValue(channel, out IDeliveryFeeStrategy? strategy))
        {
            return strategy;
        }
        throw new InvalidOperationException(channel + " kanalı için IDeliveryFeeStrategy kaydı yok.");
    }
}
