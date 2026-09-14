// Real API integration for Home Page Featured Listings
async function displayFeaturedListings() {
  var container = document.getElementById("featuredCards");
  if (!container) return;

  container.innerHTML = "<div class='loader'></div>";

  var items = [];
  try {
    if (window.UniNestApi) {
      var result = await window.UniNestApi.request("/api/v1/listings?take=4");
      if (result && result.items && result.items.length) {
        items = result.items.map(window.UniNestApi.mapListing);
      }
    }
  } catch (err) {
    console.warn("API unavailable, using local dataset fallback:", err);
  }

  if (!items || items.length === 0) {
    try {
      var source = (typeof properties !== "undefined" && Array.isArray(properties) && properties.length) ? properties :
                   (typeof baseProperties !== "undefined" && Array.isArray(baseProperties) ? baseProperties : []);
      items = source.slice(0, 4);
    } catch (e) {
      items = [];
    }
  }

  var fallbackImg = "https://images.unsplash.com/photo-1522708323590-d24dbb6b0267?w=800&auto=format&fit=crop&q=80";
  var lang = (typeof localStorage !== "undefined" && localStorage.getItem("lang")) || "en";

  var html = "";
  try {
    for (var i = 0; i < items.length; i++) {
      var p = items[i] || {};
      var tTitle = (lang === "ar" && p.title_ar) ? p.title_ar : (p.title || "UniNest Property");
      var tLoc = (lang === "ar" && p.location_ar) ? p.location_ar : (p.location || "Egypt");
      var tType = "Entire Apartment";
      if (p.type === "single") tType = (window.t ? window.t("optSingle") : null) || "Single Room";
      else if (p.type === "shared") tType = (window.t ? window.t("optShared") : null) || "Shared Room";
      else tType = (window.t ? window.t("optApt") : null) || "Entire Apartment";

      var pId = p.id || (i + 1);
      var seed = window.UniNestApi ? window.UniNestApi.numericSeed(pId) : Number(pId) || (i * 17);
      var ratingScore = (4 + (seed % 10) / 10).toFixed(1);
      var ratingCount = 10 + (seed % 40);

      var priceVal = Number(p.price) || 0;
      var priceDisplay = priceVal ? priceVal.toLocaleString() : "3,500";

      var imgUrl = p.image || fallbackImg;
      var revText = (window.t ? window.t("reviewsLbl") : null) || "reviews";
      var egpText = (window.t ? window.t("egpMonth") : null) || "EGP/mo";
      var rmsText = (window.t ? window.t("roomsWord") : null) || "rooms";
      var viewText = (window.t ? window.t("btnView") : null) || "View Details";

      html += "<div class='card' style='position:relative;'>";
      html += "<img src='" + imgUrl + "' alt='" + tTitle + "' referrerpolicy='no-referrer' crossorigin='anonymous' onerror=\"this.onerror=null;this.src='" + fallbackImg + "';\" style='width:100%; height:200px; object-fit:cover; border-radius:12px 12px 0 0;' />";
      html += "<div class='card-body'>";
      html += "<h3 style='font-size:16px; margin-bottom:4px; font-weight:700;'>" + tTitle + "</h3>";
      html += "<div style='font-size:13px; color:#f39c12; margin-bottom:10px;'><i class='fa-solid fa-star'></i> " + ratingScore + " <span style='color:var(--text-light); font-size:12px;'>(" + ratingCount + " " + revText + ")</span></div>";
      html += "<div class='card-price' style='font-weight:700; color:var(--primary); font-size:18px; margin-bottom:6px;'>" + priceDisplay + " " + egpText + "</div>";
      html += "<div class='card-location' style='font-size:13px; color:var(--text-light); margin-bottom:8px;'><i class='fa-solid fa-location-dot'></i> " + tLoc + "</div>";
      html += "<p style='font-size:13px; margin-bottom:12px; color:var(--text);'><i class='fa-solid fa-door-open'></i> " + (p.rooms || 2) + " " + rmsText + " · <i class='fa-solid fa-layer-group'></i> " + tType + "</p>";
      html += "<a href='details.html?id=" + pId + "' class='btn btn-primary' style='width:100%; text-align:center; padding:10px; border-radius:8px; display:inline-block; text-decoration:none;'>" + viewText + "</a>";
      html += "</div></div>";
    }
  } catch (renderErr) {
    console.error("Error generating HTML:", renderErr);
  }

  if (html) {
    container.innerHTML = html;
  }
}

// Search functionality
function searchListings() {
  var input = document.getElementById("searchInput");
  if (!input) return;
  var query = input.value.trim().toLowerCase();
  if (query === "") {
    window.location.href = "listings.html";
  } else {
    window.location.href = "listings.html?search=" + encodeURIComponent(query);
  }
}

function initIndex() {
  var searchInput = document.getElementById("searchInput");
  if (searchInput) {
    searchInput.addEventListener("keypress", function (e) {
      if (e.key === "Enter") searchListings();
    });
  }
  displayFeaturedListings();
}

if (document.readyState === "loading") {
  document.addEventListener("DOMContentLoaded", initIndex);
} else {
  initIndex();
}