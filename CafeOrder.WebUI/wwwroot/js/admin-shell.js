/* Admin paneli: sol menüyü daraltma / mobilde açma (sadece görsel).
   Bu dosya hiçbir işlem yapmaz; çıkış, silme, okundu işaretleme gibi her şey formlarla sunucuya gider. */
(function () {
  "use strict";

  var body = document.body;
  var toggleButton = document.getElementById("sidebarToggle");
  var backdrop = document.getElementById("admBackdrop");
  var isDesktop = window.matchMedia("(min-width: 992px)");

  if (!toggleButton) {
    return;
  }

  toggleButton.addEventListener("click", function () {
    if (isDesktop.matches) {
      body.classList.toggle("adm-collapsed");      // masaüstü: menüyü daralt
    } else {
      body.classList.toggle("adm-sidebar-open");   // mobil: menüyü aç/kapat
    }
  });

  if (backdrop) {
    backdrop.addEventListener("click", function () {
      body.classList.remove("adm-sidebar-open");
    });
  }
})();
