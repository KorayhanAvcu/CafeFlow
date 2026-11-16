using CafeOrder.DtoLayer.Dtos.OrderDtos;
using Microsoft.AspNetCore.Mvc;


// Sepet ve onay sayfası parçaları. Hepsi CartService / OrderService'ten gelen DTO'yu parametre olarak alır.

namespace CafeOrder.WebUI.ViewComponents.Cart_Confirmation;

/// <summary>Onay sayfasındaki sipariş fişi.</summary>
public class _CartConfirmationReceiptViewComponent : ViewComponent
{
    /// <summary>Gelen ResultOrderDto nesnesini model olarak Default.cshtml'e verir.</summary>
    public IViewComponentResult Invoke(ResultOrderDto order)
    {
        return View(order);
    }
}
