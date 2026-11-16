using CafeOrder.BusinessLayer.Pricing;
using CafeOrder.BusinessLayer.Pricing.Fees;
using CafeOrder.DataAccessLayer.Repositories.ProductOptions;
using CafeOrder.DataAccessLayer.Repositories.Products;
using CafeOrder.DtoLayer.Dtos.OrderDtos;
using CafeOrder.EntityLayer.Entities;
using CafeOrder.EntityLayer.Enums;
using MapsterMapper;

namespace CafeOrder.BusinessLayer.Services.Orders;

/// <summary>
/// Fiyat hesabı. DECORATOR ile birim fiyat, STRATEGY ile kanala göre ücret.
/// Ücret stratejileri DI'dan gelir (IEnumerable&lt;IDeliveryFeeStrategy&gt;); burada new yok.
/// </summary>
public class OrderPricingService : IOrderPricingService
{
    private readonly IProductRepository _productRepository;
    private readonly IProductOptionRepository _productOptionRepository;
    private readonly IReadOnlyDictionary<OrderChannel, IDeliveryFeeStrategy> _feeStrategies;
    private readonly IMapper _mapper;

    public OrderPricingService(
        IProductRepository productRepository,
        IProductOptionRepository productOptionRepository,
        IEnumerable<IDeliveryFeeStrategy> feeStrategies,
        IMapper mapper)
    {
        _productRepository = productRepository;
        _productOptionRepository = productOptionRepository;
        // Aynı kanal için iki strateji kaydedilirse burada hata verir (yanlış kaydı erken yakalar).
        _feeStrategies = feeStrategies.ToDictionary(s => s.Channel);
        _mapper = mapper;
    }

    /// <summary>Sepeti fiyatlandırır, kaydetmez.</summary>
    public async Task<ResultOrderDto> CalculateAsync(OrderChannel channel, List<CartItemDto> items)
    {
        Order order = new Order();
        order.Channel = channel;
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
            decimal unitPrice = CalculateUnitPrice(product, chosenOptions);

            OrderDetail detail = new OrderDetail();
            detail.Product = product;
            detail.ProductId = product.Id;
            detail.ProductName = product.Name;      // o anki adın kopyası
            detail.BasePrice = product.BasePrice;   // o anki fiyatın kopyası
            detail.UnitPrice = unitPrice;
            detail.Quantity = item.Quantity;
            detail.LineTotal = unitPrice * item.Quantity;

            foreach (ProductOption option in chosenOptions)
            {
                OrderDetailOption detailOption = new OrderDetailOption();
                detailOption.ProductOptionId = option.Id;
                detailOption.GroupKey = option.GroupKey;
                detailOption.Value = option.Value;
                detailOption.PriceDelta = option.PriceDelta;
                detail.Options.Add(detailOption);
            }

            order.Details.Add(detail);
        }

        // 3) Ara toplam
        order.Subtotal = order.Details.Sum(d => d.LineTotal);

        // 4) STRATEGY: sipariş türüne göre ücret kuralı seçilir.
        order.DeliveryFee = order.Details.Count == 0 ? 0 : GetFeeStrategy(order.Channel).Calculate(order.Subtotal);
        order.Total = order.Subtotal + order.DeliveryFee;
    }

    /// <summary>
    /// DECORATOR: taban fiyatın üstüne her seçenek bir kat ekler.
    /// Buradaki new doğrudur: decorator'lar servis değil, çalışma anındaki değerleri (fiyat, seçenek) saran nesnelerdir.
    /// </summary>
    private static decimal CalculateUnitPrice(Product product, List<ProductOption> options)
    {
        IPriceComponent price = new BaseProductPrice(product.BasePrice);
        foreach (ProductOption option in options)
        {
            price = new OptionPriceDecorator(price, option.PriceDelta);
        }
        return price.GetPrice();
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
