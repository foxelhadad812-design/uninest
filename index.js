// دالة عرض العقارات المميزة في الصفحة الرئيسية
function displayFeaturedListings() {
  const container = document.getElementById("featuredCards");
  const featured = properties.slice(0, 4);

  container.innerHTML = featured.map(p => `
    <div class="card">
      <img src="${p.image}" alt="${p.title}" />
      <div class="card-body">
        <div class="card-price">${p.price} ${window.t('egpMonth')}</div>
        <div class="card-location">📍 ${p.location}</div>
        <p style="font-size:14px; margin-bottom:12px;">${p.rooms} ${window.t('propRooms')} · ${p.type}</p>
        <a href="details.html?id=${p.id}" class="btn btn-primary">${window.t('btnView')}</a>
      </div>
    </div>
  `).join("");
}

// دالة البحث لما المستخدم يكتب اسم الجامعة أو المنطقة
function searchListings() {
  const query = document.getElementById("searchInput").value.trim().toLowerCase();
  if (query === "") {
    window.location.href = "listings.html";
  } else {
    window.location.href = `listings.html?search=${query}`;
  }
}

// تشغيل دالة البحث بمجرد الضغط على زر Enter من الكيبورد
document.getElementById("searchInput").addEventListener("keypress", function(e) {
  if (e.key === "Enter") searchListings();
});

// استدعاء الدالة أول ما الصفحة تحمل عشان تعرض العقارات
displayFeaturedListings();