// Real API integration for Home Page Featured Listings
async function displayFeaturedListings() {
  var container = document.getElementById("featuredCards");
  if (!container) return;

  container.innerHTML = "<div class='loader'></div>";

  try {
    var result = await window.UniNestApi.request("/api/v1/listings?take=4");
    var items = (result.items || []).map(window.UniNestApi.mapListing);

    if (items.length === 0) {
      container.innerHTML = "<div class='no-results'>No featured listings available right now.</div>";
      return;
    }

    var fallbackImg = "https://images.unsplash.com/photo-1522708323590-d24dbb6b0267?w=800&auto=format&fit=crop&q=80";
    var lang = localStorage.getItem("lang") || "en";

    var html = "";
    for (var i = 0; i < items.length; i++) {
      var p = items[i];
      var tTitle = lang === "ar" && p.title_ar ? p.title_ar : p.title;
      var tLoc = lang === "ar" && p.location_ar ? p.location_ar : p.location;
      var tType = p.type;
      if (p.type === "apartment") tType = window.t ? window.t("optApt") : "Entire Apartment";
      if (p.type === "single") tType = window.t ? window.t("optSingle") : "Single Room";
      if (p.type === "shared") tType = window.t ? window.t("optShared") : "Shared Room";

      var seed = window.UniNestApi ? window.UniNestApi.numericSeed(p.id) : Number(p.id) || 0;
      var ratingScore = (4 + (seed % 10) / 10).toFixed(1);
      var ratingCount = 10 + (seed % 40);

      var imgUrl = p.image || fallbackImg;

      html += "<div class='card' style='position:relative;'>";
      html += "<img src='" + imgUrl + "' alt='" + tTitle + "' onerror=\"this.onerror=null;this.src='" + fallbackImg + "';\" style='width:100%; height:200px; object-fit:cover; border-radius:12px 12px 0 0;' />";
      html += "<div class='card-body'>";
      html += "<h3 style='font-size:16px; margin-bottom:4px; font-weight:700;'>" + tTitle + "</h3>";
      html += "<div style='font-size:13px; color:#f39c12; margin-bottom:10px;'><i class='fa-solid fa-star'></i> " + ratingScore + " <span style='color:var(--text-light); font-size:12px;'>(" + ratingCount + " " + (window.t ? window.t("reviewsLbl") : "reviews") + ")</span></div>";
      html += "<div class='card-price' style='font-weight:700; color:var(--primary); font-size:18px; margin-bottom:6px;'>" + Number(p.price).toLocaleString() + " " + (window.t ? window.t("egpMonth") : "EGP/mo") + "</div>";
      html += "<div class='card-location' style='font-size:13px; color:var(--text-light); margin-bottom:8px;'><i class='fa-solid fa-location-dot'></i> " + tLoc + "</div>";
      html += "<p style='font-size:13px; margin-bottom:12px; color:var(--text);'><i class='fa-solid fa-door-open'></i> " + p.rooms + " " + (window.t ? window.t("roomsWord") : "rooms") + " · <i class='fa-solid fa-layer-group'></i> " + tType + "</p>";
      html += "<a href='details.html?id=" + p.id + "' class='btn btn-primary' style='width:100%; text-align:center; padding:10px; border-radius:8px; display:inline-block; text-decoration:none;'>" + (window.t ? window.t("btnView") : "View Details") + "</a>";
      html += "</div></div>";
    }
    container.innerHTML = html;
  } catch (err) {
    console.error("Failed to load featured listings:", err);
    container.innerHTML = "<div class='no-results'>Unable to load featured listings.</div>";
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

document.addEventListener("DOMContentLoaded", function () {
  var searchInput = document.getElementById("searchInput");
  if (searchInput) {
    searchInput.addEventListener("keypress", function (e) {
      if (e.key === "Enter") searchListings();
    });
  }
  displayFeaturedListings();
});