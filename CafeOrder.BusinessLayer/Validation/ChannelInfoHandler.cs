using CafeOrder.BusinessLayer.Exceptions;
using CafeOrder.EntityLayer.Entities;
using CafeOrder.EntityLayer.Enums;

namespace CafeOrder.BusinessLayer.Validation;

/// <summary>Halka 3: sipariş türüne göre gereken bilgiler girilmiş mi?</summary>
public class ChannelInfoHandler : OrderValidationHandler
{
    protected override void Check(Order order)
    {
        // Masadan sipariş: sadece geçerli bir QR (masa) gerekir.
        if (order.Channel == OrderChannel.Table)
        {
            if (order.TableId == null)
            {
                throw new BusinessException("Please scan the QR code on your table.");
            }
            return;
        }

        // Teslimat ve gel-al: ad ve telefon gerekir.
        if (order.Customer == null || string.IsNullOrWhiteSpace(order.Customer.FullName) || string.IsNullOrWhiteSpace(order.Customer.Phone))
        {
            throw new BusinessException("Please enter your name and phone number.");
        }

        // Teslimat: adres gerekir.
        if (order.Channel == OrderChannel.Delivery && string.IsNullOrWhiteSpace(order.Address))
        {
            throw new BusinessException("Please enter your delivery address.");
        }

        // Gel-al: gelecekte bir saat gerekir.
        if (order.Channel == OrderChannel.Pickup)
        {
            if (order.PickupTime == null)
            {
                throw new BusinessException("Please choose a pickup time.");
            }
            if (order.PickupTime < DateTime.Now)
            {
                throw new BusinessException("Pickup time cannot be in the past.");
            }
        }
    }
}
