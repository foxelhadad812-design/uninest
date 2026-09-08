// دالة إضافة أو إزالة الإعلان من العقارات المفضلة (بيحفظها في المتصفح)
window.toggleFavorite = function(id) {
  // بنسحب من الـ LocalStorage الليستة بتاعت المفضلة، ولو فاضية بنعتبرها مصفوفة حرة زي كدا []
  var favs = JSON.parse(localStorage.getItem("favorites") || "[]");
  var idx = favs.indexOf(id);
  if (idx === -1) {
    favs.push(id);
  } else {
    favs.splice(idx, 1);
  }
  localStorage.setItem("favorites", JSON.stringify(favs));
  applyFilters();
}

// الدالة دي هي اللي بتبني الواجهة: بتمسك كل العقارات (data) وتطبعها قدامك في الصفحة على شكل كروت
function displayListings(data) {
  var grid = document.getElementById("listingsGrid");
  var count = document.getElementById("resultsCount");
  
  // بنظهر لودينج (تحميل) في البداية عشان شكل الموقع يبقى احترافي
  grid.innerHTML = "<div class='loader'></div>";
  count.textContent = "...";
  
  setTimeout(function() {
    count.textContent = data.length + " properties found";
    if (data.length === 0) {
      grid.innerHTML = "<div class='no-results'>No properties found.</div>";
      return;
    }
    
    var favs = JSON.parse(localStorage.getItem("favorites") || "[]");
    
    var html = "";
    for (var i = 0; i < data.length; i++) {
      var p = data[i];
      var lang = localStorage.getItem("lang") || "en";
      var tTitle = lang === "ar" && p.title_ar ? p.title_ar : p.title;
      var tLoc = lang === "ar" && p.location_ar ? p.location_ar : p.location;
      var tType = p.type;
      if (p.type === 'apartment') tType = window.t('optApt');
      if (p.type === 'single') tType = window.t('optSingle');
      if (p.type === 'shared') tType = window.t('optShared');

      var isFav = favs.indexOf(p.id) !== -1;
      var heartIcon = isFav ? "<i class='fa-solid fa-heart' style='color:#e74c3c; font-size:16px;'></i>" : "<i class='fa-regular fa-heart' style='color:#999; font-size:16px;'></i>";
      
      var ratingScore = (4 + (p.id % 10) / 10).toFixed(1);
      var ratingCount = 10 + (p.id * 3);

      html += "<div class='card' style='position:relative;'>";
      html += "<div class='card-badges' style='position:absolute; top:12px; right:12px; display:flex; gap:8px; z-index:10;'>";
      if (p.gender === 'female') html += "<span style='background:#e91e63; color:#fff; padding:4px 10px; border-radius:6px; font-size:11px; font-weight:bold;'>" + window.t('badgeFemale') + "</span>";
      if (p.gender === 'male') html += "<span style='background:#2196f3; color:#fff; padding:4px 10px; border-radius:6px; font-size:11px; font-weight:bold;'>" + window.t('badgeMale') + "</span>";
      html += "</div>";
      html += "<button onclick='toggleFavorite(" + p.id + ")' title='Add to Favorites' style='position:absolute; top:12px; left:12px; background:white; border:none; border-radius:50%; width:32px; height:32px; display:flex; align-items:center; justify-content:center; cursor:pointer; box-shadow:0 2px 4px rgba(0,0,0,0.2); z-index:10; font-size:16px; transition: transform 0.2s;' onmouseover='this.style.transform=\"scale(1.1)\"' onmouseout='this.style.transform=\"scale(1)\"'>" + heartIcon + "</button>";
      html += "<img src='" + p.image + "' alt='" + p.title + "'/>";
      html += "<div class='card-body'>";
      html += "<h3 style='font-size:16px;margin-bottom:4px;'>" + tTitle + "</h3>";
      html += "<div style='font-size:13px; color:#f39c12; margin-bottom:10px;'><i class='fa-solid fa-star'></i> " + ratingScore + " <span style='color:var(--text-light); font-size:12px;'>(" + ratingCount + " " + window.t('reviewsLbl') + ")</span></div>";
      html += "<div class='card-price'>" + p.price + " " + window.t('egpMonth') + "</div>";
      html += "<div class='card-location'><i class='fa-solid fa-location-dot'></i> " + tLoc + "</div>";
      html += "<p style='font-size:13px;margin-bottom:12px;'><i class='fa-solid fa-layer-group'></i> " + p.rooms + " " + window.t('roomsWord') + " · " + tType + "</p>";
      html += "<a href='details.html?id=" + p.id + "' class='btn btn-primary'>" + window.t('btnView') + "</a>";
      html += "</div></div>";
    }
    grid.innerHTML = html;
  }, 400); // 400ms loader delay
}

// الدالة المسئولة عن تشغيل الفلتر (لما تيجي تدور على شقة بفلوس معينة أو مدينة معينة)
function applyFilters() {
  // بنمسك القيمة اللي اتكتبت ونحولها لحروف صغيرة عشان البحث يشتغل صح بغض النظر عن الكابيتال والسمول
  var search = document.getElementById("searchInput").value.toLowerCase();
  var maxPrice = parseInt(document.getElementById("priceRange").value);
  var location = document.getElementById("locationFilter").value;
  var checkboxes = document.querySelectorAll(".checkbox-group input:checked");
  var checkedTypes = [];
  for (var i = 0; i < checkboxes.length; i++) {
    checkedTypes.push(checkboxes[i].value);
  }
  var filtered = [];
  for (var i = 0; i < properties.length; i++) {
    var p = properties[i];
    var matchSearch = p.title.toLowerCase().indexOf(search) !== -1 ||
                      p.location.toLowerCase().indexOf(search) !== -1 ||
                      (p.title_ar && p.title_ar.indexOf(search) !== -1) ||
                      (p.location_ar && p.location_ar.indexOf(search) !== -1);
    var matchPrice = p.price <= maxPrice;
    var matchLocation = location === "" || p.location === location;
    var matchType = checkedTypes.length === 0 || checkedTypes.indexOf(p.type) !== -1;
    if (matchSearch && matchPrice && matchLocation && matchType) {
      filtered.push(p);
    }
  }
  displayListings(filtered);
}

// لما بنحرك (مؤشر السعر)، الدالة دي بتحدث الكلمة اللي بتوريني السعر وصل إيه وتحدث الفلتر
function updatePrice(val) {
  document.getElementById("priceDisplay").textContent = "Up to " + val + " EGP";
  applyFilters();
}

function resetFilters() {
  document.getElementById("searchInput").value = "";
  document.getElementById("priceRange").value = 20000;
  document.getElementById("priceDisplay").textContent = "Up to 20000 EGP";
  document.getElementById("locationFilter").value = "";
  var checkboxes = document.querySelectorAll(".checkbox-group input");
  for (var i = 0; i < checkboxes.length; i++) {
    checkboxes[i].checked = false;
  }
  displayListings(properties);
}

// لما الصفحة تفتح، بنسحب من اللينك (url) عشان لو كنت باحث عن حاجة من الصفحة الرئيسية أطبقها هنا
window.onload = function() {
  var params = new URLSearchParams(window.location.search);
  var search = params.get("search");
  if (search) {
    document.getElementById("searchInput").value = search;
    applyFilters();
  } else {
    displayListings(properties);
  }
};