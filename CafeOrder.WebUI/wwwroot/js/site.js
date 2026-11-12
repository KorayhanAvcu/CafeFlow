/* CafeFlow — müşteri sayfalarının ortak küçük görsel davranışları.
   Bu dosya hiçbir işlem yapmaz: veri getirmez, form göndermez, fiyat hesaplamaz.
   Bütün işlemler formlarla sunucuda (controller → servis) yapılır. */
(function () {
  "use strict";

  // Sayfa kaydırılınca navbar'a gölge ver.
  var nav = document.getElementById("mainNav");
  if (nav) {
    function onScroll() {
      if (window.scrollY > 30) {
        nav.classList.add("scrolled");
      } else {
        nav.classList.remove("scrolled");
      }
    }
    onScroll();
    window.addEventListener("scroll", onScroll);
  }

  // Ürün resmi yüklenemezse yerine ikonlu bir kutu koy.
  var images = document.querySelectorAll("img[data-fallback]");
  for (var i = 0; i < images.length; i++) {
    images[i].addEventListener("error", function () {
      var box = document.createElement("div");
      box.className = "cf-img-fallback";
      box.innerHTML = '<i class="bi bi-cup-hot"></i>';
      this.replaceWith(box);
    });
  }

  // Sunucunun gösterdiği "sepete eklendi" mesajını birkaç saniye sonra gizle.
  var toast = document.getElementById("cfToast");
  if (toast && toast.classList.contains("show")) {
    setTimeout(function () {
      toast.classList.remove("show");
    }, 2600);
  }

  // Sepete ekledikten sonra sağdaki sepet panelini bir kez aç (sunucu body'ye data-open-cart yazar).
  var drawer = document.getElementById("cartDrawer");
  if (document.body.dataset.openCart && drawer && window.bootstrap) {
    bootstrap.Offcanvas.getOrCreateInstance(drawer).show();
  }
})();
