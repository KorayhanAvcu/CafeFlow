using CafeOrder.BusinessLayer.Exceptions;
using CafeOrder.EntityLayer.Entities;
using CafeOrder.EntityLayer.Enums;

namespace CafeOrder.BusinessLayer.Validation;

/// <summary>Halka 3: sipariş türüne göre gereken bilgiler girilmiş mi?</summary>
public class ChannelInfoHandler : OrderValidationHandler
{
    private readonly TimeProvider _time;

    public ChannelInfoHandler(TimeProvider time)
    {
        _time = time;
    }

    /// <summary>Halka 3: kanala göre gereken bilgileri kontrol eder. Masa: geçerli QR. Teslimat ve gel-al: ad ve telefon. Teslimat: adres. Gel-al: ileri bir teslim alma saati.</summary>
    protected override void Check(Order order)
    {
        // Masadan sipariş: sadece geçerli bir QR (masa) gerekir.
        if (order.Channel == OrderChannel.Table)
        {
            if (order.TableId == null)
            {
                throw new BusinessException("Lütfen masanızdaki QR kodu okutun.");
            }
            return;
        }

        // Teslimat ve gel-al: ad ve telefon gerekir.
        if (order.Customer == null || string.IsNullOrWhiteSpace(order.Customer.FullName) || string.IsNullOrWhiteSpace(order.Customer.Phone))
        {
            throw new BusinessException("Lütfen adınızı ve telefon numaranızı girin.");
        }

        // Teslimat: adres gerekir.
        if (order.Channel == OrderChannel.Delivery && string.IsNullOrWhiteSpace(order.Address))
        {
            throw new BusinessException("Lütfen teslimat adresinizi girin.");
        }

        // Gel-al: gelecekte bir saat gerekir.
        if (order.Channel == OrderChannel.Pickup)
        {
            if (order.PickupTime == null)
            {
                throw new BusinessException("Lütfen teslim alma saatini seçin.");
            }
            if (order.PickupTime < _time.GetLocalNow().DateTime)
            {
                throw new BusinessException("Teslim alma saati geçmişte olamaz.");
            }
        }
    }
}
