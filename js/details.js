//_first_load_details_page
window.onload = async function() {
  var params = new URLSearchParams(window.location.search);
  var id = params.get("id");
  var content = document.getElementById("detailsContent");

  if (!id) {
    content.innerHTML = "<div class='not-found'>No property ID specified.</div>";
    return;
  }

  // Show loading state
  content.innerHTML = "<div class='not-found' style='text-align:center; padding:60px 0;'><i class='fa-solid fa-spinner fa-pulse' style='font-size:32px;'></i><p style='margin-top:12px; color:var(--text-light);'>Loading listing...</p></div>";

  var property = null;
  var loadError = null;

  try {
    if (window.UniNestApi) {
      property = await window.UniNestApi.getListing(id);
    } else {
      loadError = "API not available";
    }
  } catch (err) {
    if (err.status === 404) {
      loadError = "not_found";
    } else {
      loadError = "error";
    }
  }

  if (loadError === "not_found") {
    content.innerHTML = "<div class='not-found'>😕 Property not found.</div>";
    return;
  }

  if (loadError) {
    content.innerHTML = "<div class='not-found'>⚠️ Unable to load property details. Please try again later.</div>";
    return;
  }

  if (!property) {
    content.innerHTML = "<div class='not-found'>😕 Property not found.</div>";
    return;
  }

  // Build amenities HTML
  var amenitiesHTML = "";
  var amenities = property.amenities || [];
  for (var i = 0; i < amenities.length; i++) {
    amenitiesHTML += "<span class='amenity-tag'><i class='fa-solid fa-check-circle'></i> " + amenities[i] + "</span>";
  }

  var lang = localStorage.getItem("lang") || "en";
  var tTitle = lang === "ar" && property.title_ar ? property.title_ar : property.title;
  var tLoc = lang === "ar" && property.location_ar ? property.location_ar : property.location;
  var tDesc = lang === "ar" && property.desc_ar ? property.desc_ar : property.description;
  var tType = property.type;
  if (property.type === "apartment") tType = window.t("optApt");
  if (property.type === "single") tType = window.t("optSingle");
  if (property.type === "shared") tType = window.t("optShared");

  // Generate fake reviews (unchanged from original — uses deterministic seed-based generation)
  var htmlReviews = "";
  var femaleNames = ["Salma Y.", "Nour S.", "Aya M.", "Fatma A.", "Hadeer K.", "Mai E.", "Hanan F.", "Dina S.", "Sara A.", "Noha A.", "Rana T.", "Mariam G.", "Heba W.", "Yasmine H.", "Esraa B.", "Maha N."];
  var maleNames = ["Ahmed Y.", "Omar M.", "Ziad A.", "Kareem T.", "Hassan M.", "Mohamed R.", "Nader S.", "Mahmoud E.", "Youssef I.", "Sayed F.", "Tamer H.", "Khaled Y.", "Ali F.", "Ramy S.", "Amr D.", "Mostafa K."];

  var getNames = function(propGender) {
    if (propGender === "female") return femaleNames;
    if (propGender === "male") return maleNames;
    return maleNames.concat(femaleNames);
  };

  var seed = window.UniNestApi ? window.UniNestApi.numericSeed(property.id) : Number(property.id) || 0;
  var availableNames = getNames(property.gender);
  var name1 = availableNames[(seed * 3) % availableNames.length];
  var name2 = availableNames[((seed * 3) + 1) % availableNames.length];
  var name3 = availableNames[((seed * 3) + 2) % availableNames.length];

  var texts = [
    { r: "Great location and very quiet. The internet is super fast.", ar: "مكان ممتاز وهادئ جداً، والنت سريع.", s: 5 },
    { r: "The owner is very respectful and helpful.", ar: "المالك محترم جداً ومتعاون.", s: 5 },
    { r: "Value for money is amazing, close to my faculty.", ar: "قيمة مقابل سعر ممتازة وقريب لجامعتي.", s: 4 },
    { r: "Clean room and everything was exactly like the pictures.", ar: "غرفة نظيفة وكل حاجة كانت زي الصور بالظبط.", s: 5 },
    { r: "Neighbors are friendly, highly recommend.", ar: "سكن ممتاز ومريح، أنصح بيه بشدة.", s: 4 },
    { r: "Security is good here, I feel safe.", ar: "الأمان عالي هنا وبحس براحة.", s: 5 },
    { r: "Perfect for students, lots of restaurants nearby.", ar: "مثالي للطلاب، وفي مطاعم كتير قريبة.", s: 4 },
    { r: "It's decent for the price, but could be cleaner at first.", ar: "معقول بالنسبة لسعره، بس كان محتاج تنضيف أول ما استلمت.", s: 3 },
    { r: "Good overall, the view is nice and sunny.", ar: "بشكل عام كويس جداً، الإطلالة حلوة وبتدخلها شمس.", s: 4 }
  ];

  var count = (seed % 2 === 0) ? 3 : 2;
  for (var k = 0; k < count; k++) {
    var nLabel = [name1, name2, name3][k];
    var tLabel = (k + 1) + " months ago";
    var txtObj = texts[(seed + k) % texts.length];
    var txt = lang === "ar" ? txtObj.ar : txtObj.r;

    var starsHtml = "";
    for (var s = 1; s <= 5; s++) {
      if (s <= txtObj.s) starsHtml += "<span style='color:#f39c12; font-size:14px; margin-right:2px;'>★</span>";
      else starsHtml += "<span style='color:#ccc; font-size:14px; margin-right:2px;'>★</span>";
    }

    htmlReviews += "<div style='background:var(--bg); padding:16px; border-radius:8px; margin-bottom:12px;'>" +
      "<div style='display:flex; justify-content:space-between; margin-bottom:8px;'>" +
      "<strong style='font-size:14px;'>" + nLabel + "</strong>" +
      "<span style='font-size:12px; color:var(--text-light);'>" + tLabel + "</span>" +
      "</div>" +
      "<div style='margin-bottom:8px;'>" + starsHtml + "</div>" +
      "<p style='font-size:14px; margin:0;'>" + txt + "</p>" +
      "</div>";
  }

  var usdPrice = Math.round(property.price / 50);
  var eurPrice = Math.round(property.price / 54);
  var sarPrice = Math.round(property.price / 13.3);

  // Check auth state and gender mismatch
  var user = JSON.parse(localStorage.getItem("currentUser") || "null");
  var userGender = user ? user.gender : null;

  var isGenderMismatch = false;
  if (property.gender && userGender && property.gender !== userGender) {
    isGenderMismatch = true;
  }

  // Build contact/action buttons — reverted to original hardcoded strings for missing translation keys
  var actionButtonsHTML = "";
  if (isGenderMismatch) {
    var genderWord = property.gender === "female" ? "إناث" : "ذكور";
    var genderWordEn = property.gender === "female" ? "Females" : "Males";
    var errorTxt = lang === "ar" ? "❌ هذا السكن مخصص للـ (" + genderWord + ") فقط"
      : "❌ This property is for " + genderWordEn + " only";
    actionButtonsHTML = "<button class='contact-btn' style='background:#f8d7da; color:#721c24; cursor:not-allowed; border:none; width:100%; margin-top:20px;' disabled><b>" + errorTxt + "</b></button>";
  } else {
    actionButtonsHTML =
      "<a href='tel:" + property.phone + "' class='contact-btn btn-call'><i class='fa-solid fa-phone'></i> Call</a>" +
      "<button class='contact-btn' style='background:#27ae60; color:white;' onclick='showInquiryForm()'><i class='fa-solid fa-paper-plane'></i> Request Booking</button>" +
      "<div id='inquiryFormContainer' style='display:none; margin-bottom:12px; background:var(--bg); padding:12px; border-radius:8px; text-align:left;'>" +
      "<textarea id='inquiryMsgInput' placeholder='Send a message to owner...' style='width:100%; height:75px; border-radius:6px; border:1px solid var(--border); padding:8px; font-size:13px; font-family:inherit;'></textarea>" +
      "<button class='contact-btn btn-call' style='margin-top:8px; margin-bottom:0;' onclick='sendInquiryMessage(\"" + String(property.id) + "\")'>Submit Request</button>" +
      "</div>" +
      "<button class='contact-btn btn-favorite' onclick='addToFavorites(\"" + String(property.id) + "\")'><i class='fa-solid fa-heart'></i> " + window.t("favs") + "</button>" +
      "<div class='discount-banner' style='margin-top:15px; padding:15px; background:rgba(33,150,243,0.1); border-radius:8px; border:1px dashed #2196f3; text-align:center;'>" +
      "<div style='font-size:14px; font-weight:600; color:#1976d2; margin-bottom:8px;'>" + window.t("eeluPromo") + "</div>" +
      "<button class='contact-btn' style='background:#1976d2; color:white; font-size:13px; margin:0;' onclick='showDiscountForm()'><i class='fa-solid fa-tag'></i> " + window.t("applyDiscountBtn") + "</button>" +
      "</div>" +
      "<div id='discountForm' style='display:none; margin-top:15px; background:var(--bg); padding:15px; border-radius:8px; font-size:13px;'>" +
      "<p style='margin-bottom:15px; color:var(--text-light);'>" + window.t("discountModalDesc") + "</p>" +
      "<label style='display:block; margin-bottom:6px; font-weight:600;'>" + window.t("uniIdLbl") + "</label>" +
      "<input type='file' accept='image/*' style='margin-bottom:15px; width:100%; border:1px solid var(--border); padding:8px;'>" +
      "<label style='display:block; margin-bottom:6px; font-weight:600;'>" + window.t("nidLbl") + "</label>" +
      "<input type='file' accept='image/*' style='margin-bottom:15px; width:100%; border:1px solid var(--border); padding:8px;'>" +
      "<button class='contact-btn btn-call' onclick='submitDiscountReq()'>" + window.t("btnSendReq") + "</button>" +
      "</div>";
  }

  var fallbackImg = "https://images.unsplash.com/photo-1522708323590-d24dbb6b0267?w=800&auto=format&fit=crop&q=80";
  var mainImg = property.image || fallbackImg;

  content.innerHTML =
    "<div class='details-layout'>" +
    "<div>" +
    "<img src='" + mainImg + "' alt='" + tTitle + "' class='details-img' onerror=\"this.onerror=null;this.src='" + fallbackImg + "';\"/>" +
    "<h1 class='details-title'>" + tTitle + "</h1>" +
    "<div class='details-price' style='display:flex; align-items:center; gap:12px; flex-wrap:wrap; margin-bottom:16px;'>" +
    "<span>" + property.price + " " + window.t("egpMonth") + "</span>" +
    "<div style='font-size:14px; font-weight:600; color:var(--text-light); display:flex; gap:10px; align-items:center; background:var(--bg); padding:6px 14px; border-radius:20px; border:1px solid var(--border);'>" +
    "<span title='US Dollar' style='display:flex; align-items:center; gap:4px;'>🇺🇸 ~$" + usdPrice + "</span> <span style='color:var(--border)'>|</span> " +
    "<span title='Euro' style='display:flex; align-items:center; gap:4px;'>🇪🇺 ~€" + eurPrice + "</span> <span style='color:var(--border)'>|</span> " +
    "<span title='Saudi Riyal' style='display:flex; align-items:center; gap:4px;'>🇸🇦 ~" + sarPrice + " SAR</span>" +
    "</div>" +
    "</div>" +
    "<div class='details-location'><i class='fa-solid fa-location-dot'></i> " + tLoc + "</div>" +
    "<div class='details-section'>" +
    "<h3><i class='fa-solid fa-map-location-dot'></i> Location Map</h3>" +
    "<div id='propertyMap' style='height:240px; border-radius:var(--radius); border:1px solid var(--border); margin-top:8px;'></div>" +
    "</div>" +
    "<div class='details-section'>" +
    "<h3>" + window.t("propDesc") + "</h3>" +
    "<p>" + tDesc + "</p>" +
    "</div>" +
    "<div class='details-section'>" +
    "<h3>" + window.t("propAmenities") + "</h3>" +
    "<div class='amenities-list'>" + amenitiesHTML + "</div>" +
    "</div>" +
    "<div class='details-section'>" +
    "<h3>Property Details</h3>" +
    "<p>" + window.t("propRooms") + ": " + property.rooms + "</p>" +
    "<p>" + window.t("propType") + ": " + tType + "</p>" +
    "</div>" +
    "<div class='details-section'>" +
    "<h3><i class='fa-solid fa-star' style='color:#f39c12'></i> " + window.t("studentReviews") + "</h3>" +
    htmlReviews +
    "</div>" +
    "</div>" +
    "<div>" +
    "<div class='contact-card'>" +
    "<h3>Contact</h3>" +
    "<div class='owner-info'>" +
    "<div class='owner-avatar'>" + property.owner.charAt(0) + "</div>" +
    "<div>" +
    "<div class='owner-name'>" + property.owner + "</div>" +
    "<div class='owner-label'>" + window.t("optOwner") + "</div>" +
    "</div>" +
    "</div>" +
    actionButtonsHTML +
    "</div>" +
    "</div>" +
    "</div>";

  // Initialize map after DOM update
  setTimeout(function() {
    if (window.L && document.getElementById("propertyMap")) {
      var lat = 30.0444 + ((seed % 50) - 25) * 0.005;
      var lng = 31.2357 + ((seed % 40) - 20) * 0.005;
      var map = L.map("propertyMap").setView([lat, lng], 14);
      L.tileLayer("https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png", {
        maxZoom: 18,
        attribution: "&copy; OpenStreetMap contributors"
      }).addTo(map);
      L.marker([lat, lng]).addTo(map)
        .bindPopup("<b>" + tTitle + "</b><br>" + tLoc)
        .openPopup();
    }
  }, 200);
};

// Favorites management (real API integration)
async function addToFavorites(id) {
  var user = JSON.parse(localStorage.getItem("currentUser") || "null");
  if (!user) {
    window.location.href = "login.html?returnUrl=" + encodeURIComponent(window.location.href);
    return;
  }

  try {
    if (window.UniNestApi) {
      await window.UniNestApi.addFavorite(id);
      alert("Added to favorites! ❤️");
    }
  } catch (err) {
    alert("Failed to add to favorites.");
  }
}

// Discount form toggle (unchanged)
function showDiscountForm() {
  var form = document.getElementById("discountForm");
  if (form) {
    form.style.display = form.style.display === "none" ? "block" : "none";
  }
}

// Discount submission (unchanged — localStorage mock)
function submitDiscountReq() {
  alert(window.t("discountSuccess"));
  document.getElementById("discountForm").style.display = "none";
}

// Inquiry form toggle (unchanged)
function showInquiryForm() {
  var form = document.getElementById("inquiryFormContainer");
  if (form) {
    form.style.display = form.style.display === "none" ? "block" : "none";
  }
}

// Inquiry message submission (real API integration)
async function sendInquiryMessage(propertyId) {
  var user = JSON.parse(localStorage.getItem("currentUser") || "null");
  if (!user) {
    window.location.href = "login.html?returnUrl=" + encodeURIComponent(window.location.href);
    return;
  }

  var msgInput = document.getElementById("inquiryMsgInput");
  var text = msgInput ? msgInput.value.trim() : "";
  var container = document.getElementById("inquiryFormContainer");

  var feedbackDiv = document.getElementById("inquiryFeedback");
  if (!feedbackDiv && container) {
    feedbackDiv = document.createElement("div");
    feedbackDiv.id = "inquiryFeedback";
    container.appendChild(feedbackDiv);
  }

  if (!text) {
    if (feedbackDiv) {
      feedbackDiv.style.cssText = "margin-top:8px; padding:8px 12px; border-radius:6px; font-size:13px; background:#f8d7da; color:#721c24;";
      feedbackDiv.textContent = "Please enter a message for your inquiry.";
    }
    return;
  }

  var btn = container ? container.querySelector("button") : null;
  if (btn) {
    btn.disabled = true;
    btn.innerHTML = "<i class='fa-solid fa-spinner fa-pulse'></i> Sending...";
  }

  try {
    if (window.UniNestApi) {
      await window.UniNestApi.inquire(propertyId, text);
      if (feedbackDiv) {
        feedbackDiv.style.cssText = "margin-top:8px; padding:10px 12px; border-radius:6px; font-size:13px; background:#d4edda; color:#155724;";
        feedbackDiv.innerHTML = "✅ Booking inquiry sent to owner successfully! 📬";
      }
      if (msgInput) msgInput.value = "";
      if (btn) btn.style.display = "none";
    } else {
      throw new Error("API unavailable");
    }
  } catch (err) {
    if (btn) {
      btn.disabled = false;
      btn.innerHTML = "Submit Request";
    }
    var msg = err.message || "Failed to send inquiry.";
    if (err.status === 409) {
      msg = "⚠️ You already have an active inquiry for this property.";
    } else if (err.status === 400 && msg.indexOf("own listing") !== -1) {
      msg = "⚠️ You cannot inquire on your own listing.";
    }
    if (feedbackDiv) {
      feedbackDiv.style.cssText = "margin-top:8px; padding:10px 12px; border-radius:6px; font-size:13px; background:#fff3cd; color:#856404;";
      feedbackDiv.textContent = msg;
    }
  }
}
