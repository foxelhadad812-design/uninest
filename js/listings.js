// Real API integration for favorites
window.toggleFavorite = async function(id) {
  var user = JSON.parse(localStorage.getItem("currentUser") || "null");
  if (!user) {
    window.location.href = "login.html?returnUrl=" + encodeURIComponent(window.location.href);
    return;
  }

  try {
    if (window.UniNestApi) {
      var favs = window._userFavoriteIds || [];
      var isFav = favs.indexOf(id) !== -1 || favs.indexOf(String(id)) !== -1;
      if (isFav) {
        await window.UniNestApi.removeFavorite(id);
      } else {
        await window.UniNestApi.addFavorite(id);
      }
      try {
        window._userFavoriteIds = await window.UniNestApi.getMyFavoriteIds();
      } catch (e) {
        window._userFavoriteIds = [];
      }
      applyFilters();
    }
  } catch (err) {
    console.error("Failed to toggle favorite:", err);
  }
};

// خريطة أسماء المدن إلى الـ GUID اللي الباكند بيبغاه
var locationNameToId = null;

// خريطة أنواع العقارات من واجهة المستخدم إلى القيم اللي الباكند بيتوقعها
var typeToBackend = {
  apartment: "entireApartment",
  single: "privateRoom",
  shared: "sharedBed"
};

// الحالة العامة للصفحة
var listingsData = null;
var isLoading = false;

// جلب المدن واحد مرة من الباكند عشان نبني خريطة الأسماء إلى المعرفات
async function fetchLocationsMap() {
  if (locationNameToId) return locationNameToId;
  try {
    var result = await window.UniNestApi.request("/api/v1/catalog/locations");
    locationNameToId = {};
    var items = result.items || result || [];
    for (var i = 0; i < items.length; i++) {
      var loc = items[i];
      var nameKey = loc.nameEn || loc.name || loc.titleEn || "";
      var idVal = loc.id || loc.locationId || "";
      if (nameKey && idVal) {
        locationNameToId[nameKey] = idVal;
      }
    }
    return locationNameToId;
  } catch (e) {
    console.warn("Could not load locations map:", e);
    locationNameToId = {};
    return locationNameToId;
  }
}

// بناء سلسلة الاستعلام من حالة الفلاتر الحالية
function buildQueryString() {
  var params = [];
  var search = document.getElementById("searchInput").value.trim();
  if (search) params.push("q=" + encodeURIComponent(search));

  var maxPrice = parseInt(document.getElementById("priceRange").value, 10);
  if (!isNaN(maxPrice) && maxPrice < 20000) {
    params.push("maxPrice=" + maxPrice);
  }

  var locationName = document.getElementById("locationFilter").value;
  if (locationName) {
    if (!locationNameToId) {
      throw new Error("Locations map not loaded yet");
    }
    var locId = locationNameToId[locationName];
    if (locId) {
      params.push("locationId=" + locId);
    }
  }

  var checkedTypes = [];
  var checkboxes = document.querySelectorAll(".checkbox-group input:checked");
  for (var i = 0; i < checkboxes.length; i++) {
    checkedTypes.push(checkboxes[i].value);
  }
  if (checkedTypes.length > 0) {
    var backendTypes = [];
    for (var j = 0; j < checkedTypes.length; j++) {
      var bt = typeToBackend[checkedTypes[j]];
      if (bt) backendTypes.push(bt);
    }
    if (backendTypes.length > 0) {
      params.push("listingType=" + encodeURIComponent(backendTypes.join(",")));
    }
  }

  params.push("skip=0");
  params.push("take=50");

  return params.join("&");
}

// عرض حالة التحميل أو النتائج الفارغة أو الخطأ
function setGridLoading() {
  var grid = document.getElementById("listingsGrid");
  var count = document.getElementById("resultsCount");
  grid.innerHTML = "<div class='loader'></div>";
  count.textContent = "...";
}

function setGridEmpty() {
  var grid = document.getElementById("listingsGrid");
  var count = document.getElementById("resultsCount");
  grid.innerHTML = "<div class='no-results'>No properties found.</div>";
  count.textContent = "0 properties found";
}

function setGridError() {
  var grid = document.getElementById("listingsGrid");
  var count = document.getElementById("resultsCount");
  grid.innerHTML = "<div class='no-results'>Unable to load listings. Please try again later.</div>";
  count.textContent = "Error loading data";
}

// عرض Cascading للعقارات (نفس التصميم القديم، لكن البيانات من الباكند)
function renderListings(data) {
  var grid = document.getElementById("listingsGrid");
  var count = document.getElementById("resultsCount");
  count.textContent = data.length + " properties found";

  if (data.length === 0) {
    grid.innerHTML = "<div class='no-results'>No properties found.</div>";
    return;
  }

  var favs = window._userFavoriteIds || [];

  var html = "";
  for (var i = 0; i < data.length; i++) {
    var p = data[i];
    var lang = localStorage.getItem("lang") || "en";
    var tTitle = lang === "ar" && p.title_ar ? p.title_ar : p.title;
    var tLoc = lang === "ar" && p.location_ar ? p.location_ar : p.location;
    var tType = p.type;
    if (p.type === "apartment") tType = window.t("optApt");
    if (p.type === "single") tType = window.t("optSingle");
    if (p.type === "shared") tType = window.t("optShared");

    var isFav = favs.indexOf(p.id) !== -1 || favs.indexOf(String(p.id)) !== -1;
    var heartIcon = isFav
      ? "<i class='fa-solid fa-heart' style='color:#e74c3c; font-size:16px;'></i>"
      : "<i class='fa-regular fa-heart' style='color:#999; font-size:16px;'></i>";

    var seed = window.UniNestApi ? window.UniNestApi.numericSeed(p.id) : Number(p.id) || 0;
    var ratingScore = (4 + (seed % 10) / 10).toFixed(1);
    var ratingCount = 10 + (seed % 40);

    html += "<div class='card' style='position:relative;'>";
    html += "<div class='card-badges' style='position:absolute; top:12px; right:12px; display:flex; gap:8px; z-index:10;'>";
    if (p.gender === "female")
      html += "<span style='background:#e91e63; color:#fff; padding:4px 10px; border-radius:6px; font-size:11px; font-weight:bold;'>" + window.t("badgeFemale") + "</span>";
    if (p.gender === "male")
      html += "<span style='background:#2196f3; color:#fff; padding:4px 10px; border-radius:6px; font-size:11px; font-weight:bold;'>" + window.t("badgeMale") + "</span>";
    html += "</div>";
    html += "<button onclick='toggleFavorite(" + JSON.stringify(String(p.id)) + ")' title='Add to Favorites' style='position:absolute; top:12px; left:12px; background:white; border:none; border-radius:50%; width:32px; height:32px; display:flex; align-items:center; justify-content:center; cursor:pointer; box-shadow:0 2px 4px rgba(0,0,0,0.2); z-index:10; font-size:16px; transition: transform 0.2s;' onmouseover='this.style.transform=\"scale(1.1)\"' onmouseout='this.style.transform=\"scale(1)\"'>" + heartIcon + "</button>";
    var fallbackImg = "https://images.unsplash.com/photo-1522708323590-d24dbb6b0267?w=800&auto=format&fit=crop&q=80";
    html += "<img src='" + (p.image || fallbackImg) + "' alt='" + tTitle + "' onerror=\"this.onerror=null;this.src='" + fallbackImg + "';\" style='width:100%; height:200px; object-fit:cover; border-radius:12px 12px 0 0;'/>";
    html += "<div class='card-body'>";
    html += "<h3 style='font-size:16px;margin-bottom:4px; font-weight:700;'>" + tTitle + "</h3>";
    html += "<div style='font-size:13px; color:#f39c12; margin-bottom:10px;'><i class='fa-solid fa-star'></i> " + ratingScore + " <span style='color:var(--text-light); font-size:12px;'>(" + ratingCount + " " + window.t("reviewsLbl") + ")</span></div>";
    html += "<div class='card-price' style='font-weight:700; color:var(--primary); font-size:18px; margin-bottom:6px;'>" + Number(p.price).toLocaleString() + " " + window.t("egpMonth") + "</div>";
    html += "<div class='card-location' style='font-size:13px; color:var(--text-light); margin-bottom:8px;'><i class='fa-solid fa-location-dot'></i> " + tLoc + "</div>";
    html += "<p style='font-size:13px;margin-bottom:12px; color:var(--text);'><i class='fa-solid fa-door-open'></i> " + p.rooms + " " + window.t("roomsWord") + " · <i class='fa-solid fa-layer-group'></i> " + tType + "</p>";
    html += "<a href='details.html?id=" + p.id + "' class='btn btn-primary' style='width:100%; text-align:center; padding:10px; border-radius:8px; display:inline-block; text-decoration:none;'>" + window.t("btnView") + "</a>";
    html += "</div></div>";
  }
  grid.innerHTML = html;
}

// جلب العقارات من الباكند وبناء الواجهة
async function fetchAndRenderListings() {
  if (isLoading) return;
  isLoading = true;

  setGridLoading();

  try {
    // تأكد أن خريطة المدن جاهزة
    if (!locationNameToId) {
      await fetchLocationsMap();
    }

    if (localStorage.getItem("currentUser")) {
      try { window._userFavoriteIds = await window.UniNestApi.getMyFavoriteIds(); } catch (e) { window._userFavoriteIds = []; }
    }

    var queryString = buildQueryString();
    var url = "/api/v1/listings" + (queryString ? "?" + queryString : "");
    var result = await window.UniNestApi.request(url);

    listingsData = (result.items || []).map(window.UniNestApi.mapListing);
    renderListings(listingsData);
  } catch (err) {
    console.error("Failed to load listings:", err);
    setGridError();
  } finally {
    isLoading = false;
  }
}

// تحديث الفلاتر من الباكند بدل التصفية المحلية
function applyFilters() {
  fetchAndRenderListings();
}

// تحديث عرض السعر وتشغيل الفلتر
function updatePrice(val) {
  document.getElementById("priceDisplay").textContent = "Up to " + val + " EGP";
  applyFilters();
}

// إعادة ضبط الفلاتر واستعادة كل العقارات من الباكند
function resetFilters() {
  document.getElementById("searchInput").value = "";
  document.getElementById("priceRange").value = 20000;
  document.getElementById("priceDisplay").textContent = "Up to 20000 EGP";
  document.getElementById("locationFilter").value = "";
  var checkboxes = document.querySelectorAll(".checkbox-group input");
  for (var i = 0; i < checkboxes.length; i++) {
    checkboxes[i].checked = false;
  }
  fetchAndRenderListings();
}

// لما الصفحة تفتح، نحصل على المدن واحدة ثم نبدأ الجلب
window.onload = async function() {
  await fetchLocationsMap();
  var params = new URLSearchParams(window.location.search);
  var search = params.get("search");
  if (search) {
    document.getElementById("searchInput").value = search;
  }
  fetchAndRenderListings();
};
