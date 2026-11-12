/* CafeFlow — ana sayfa: bölümler ekrana girdikçe yumuşakça görünsün (sadece görsel). */
(function () {
  "use strict";

  var items = document.querySelectorAll(".reveal");

  // Eski tarayıcı: hepsini direkt göster.
  if (!("IntersectionObserver" in window)) {
    for (var i = 0; i < items.length; i++) {
      items[i].classList.add("visible");
    }
    return;
  }

  var observer = new IntersectionObserver(function (entries) {
    entries.forEach(function (entry) {
      if (entry.isIntersecting) {
        entry.target.classList.add("visible");
        observer.unobserve(entry.target);
      }
    });
  }, { threshold: 0.12 });

  for (var j = 0; j < items.length; j++) {
    observer.observe(items[j]);
  }
})();
