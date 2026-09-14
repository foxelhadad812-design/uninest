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
    }
  } catch (err) {
    console.warn("Failed to load listing from API, falling back to local dataset:", err);
  }

  if (!property) {
    var source = (typeof properties !== "undefined" ? properties : (typeof baseProperties !== "undefined" ? baseProperties : []));
    for (var j = 0; j < source.length; j++) {
      if (String(source[j].id) === String(id)) {
        property = source[j];
        break;
      }
    }
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
  // Render user submitted reviews
  var localReviews = JSON.parse(localStorage.getItem("uninest.userReviews." + property.id) || "[]");
  var userReviewsHTML = "";
  for (var ur = 0; ur < localReviews.length; ur++) {
    var rev = localReviews[ur];
    var rStars = "";
    for (var st = 1; st <= 5; st++) {
      rStars += st <= rev.rating ? "<span style='color:#f39c12; font-size:14px; margin-right:2px;'>★</span>" : "<span style='color:#ccc; font-size:14px; margin-right:2px;'>★</span>";
    }
    userReviewsHTML +=
      "<div style='background:var(--white); padding:14px; border-radius:8px; margin-bottom:10px; border:1px solid var(--border);'>" +
      "<div style='display:flex; justify-content:space-between; margin-bottom:4px;'>" +
      "<strong>" + rev.userName + " <span style='background:#27ae60; color:white; padding:1px 6px; border-radius:10px; font-size:10px;'>Verified Student</span></strong>" +
      "<span style='font-size:11px; color:var(--text-light);'>" + rev.date + "</span>" +
      "</div>" +
      "<div style='margin-bottom:6px;'>" + rStars + "</div>" +
      "<p style='font-size:13px; margin:0;'>" + rev.comment + "</p>" +
      "</div>";
  }

  var addReviewFormHTML =
    "<div style='margin-top:16px; background:var(--bg); padding:14px; border-radius:8px; border:1px solid var(--border);'>" +
    "<h4 style='font-size:14px; margin-bottom:8px;'><i class='fa-solid fa-pen'></i> Write a Student Review</h4>" +
    "<div style='display:flex; gap:10px; margin-bottom:8px; align-items:center;'>" +
    "<span style='font-size:13px;'>Rating:</span>" +
    "<select id='newReviewRating' style='padding:4px 8px; border-radius:6px; border:1px solid var(--border); font-size:13px;'>" +
    "<option value='5'>⭐⭐⭐⭐⭐ (5/5 Exceptional)</option>" +
    "<option value='4'>⭐⭐⭐⭐ (4/5 Very Good)</option>" +
    "<option value='3'>⭐⭐⭐ (3/5 Good)</option>" +
    "<option value='2'>⭐⭐ (2/5 Fair)</option>" +
    "<option value='1'>⭐ (1/5 Poor)</option>" +
    "</select>" +
    "</div>" +
    "<textarea id='newReviewComment' placeholder='Share your experience about location, landlord, internet...' style='width:100%; height:60px; border-radius:6px; border:1px solid var(--border); padding:8px; font-size:13px; font-family:inherit;'></textarea>" +
    "<button onclick='submitStudentReview(\"" + property.id + "\")' class='contact-btn' style='margin-top:8px; width:auto; padding:6px 16px; font-size:12px; background:var(--primary); color:white;'>Submit Review</button>" +
    "</div>";

  actionButtonsHTML +=
    "<button class='contact-btn' style='background:#f39c12; color:white; font-weight:700; margin-top:10px;' onclick='showPaymentModal(\"" + property.id + "\", \"" + tTitle.replace(/'/g, "") + "\", " + property.price + ")'><i class='fa-solid fa-shield-halved'></i> Reserve Now with Deposit</button>";

  var fallbackImg = "https://images.unsplash.com/photo-1522708323590-d24dbb6b0267?w=800&auto=format&fit=crop&q=80";
  var mainImg = property.image || fallbackImg;

  content.innerHTML =
    "<div class='details-layout'>" +
    "<div>" +
    "<img src='" + mainImg + "' alt='" + tTitle + "' class='details-img' referrerpolicy='no-referrer' crossorigin='anonymous' onerror=\"this.onerror=null;this.src='" + fallbackImg + "';\"/>" +
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
    userReviewsHTML +
    htmlReviews +
    addReviewFormHTML +
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

// Student review submission handler
window.submitStudentReview = function(propId) {
  var user = JSON.parse(localStorage.getItem("currentUser") || "null");
  if (!user) {
    alert("Please log in to submit a review!");
    window.location.href = "login.html";
    return;
  }
  var rating = parseInt(document.getElementById("newReviewRating").value, 10);
  var comment = document.getElementById("newReviewComment").value.trim();
  if (!comment) {
    alert("Please enter a review comment.");
    return;
  }

  var localReviews = JSON.parse(localStorage.getItem("uninest.userReviews." + propId) || "[]");
  localReviews.unshift({
    userName: user.name,
    rating: rating,
    comment: comment,
    date: "Just now"
  });
  localStorage.setItem("uninest.userReviews." + propId, JSON.stringify(localReviews));
  alert("Thank you! Your review has been published ⭐");
  window.location.reload();
};

// Payment simulation modal
window.showPaymentModal = function(propId, title, price) {
  var user = JSON.parse(localStorage.getItem("currentUser") || "null");
  if (!user) {
    alert("Please log in first to reserve this property!");
    window.location.href = "login.html";
    return;
  }

  var depositAmount = Math.round(price * 0.2); // 20% deposit

  var modalHTML =
    "<div id='paymentModalOverlay' style='position:fixed; top:0; left:0; width:100%; height:100%; background:rgba(0,0,0,0.6); z-index:999999; display:flex; align-items:center; justify-content:center; padding:16px;'>" +
    "<div style='background:white; border-radius:12px; width:100%; max-width:440px; padding:24px; box-shadow:0 10px 30px rgba(0,0,0,0.3); font-family:inherit; position:relative;'>" +
    "<button onclick='closePaymentModal()' style='position:absolute; top:16px; right:16px; background:none; border:none; font-size:20px; cursor:pointer;'>✕</button>" +
    "<h3 style='margin-bottom:6px; color:#1976d2;'><i class='fa-solid fa-lock'></i> Secure Booking Deposit</h3>" +
    "<p style='font-size:13px; color:#666; margin-bottom:16px;'>" + title + "</p>" +
    "<div style='background:#f8f9fa; padding:12px; border-radius:8px; margin-bottom:16px; font-size:13px; border:1px solid #e0e0e0;'>" +
    "<div style='display:flex; justify-content:space-between; margin-bottom:4px;'><span>Monthly Rent:</span><strong>" + Number(price).toLocaleString() + " EGP</strong></div>" +
    "<div style='display:flex; justify-content:space-between; color:#27ae60; font-size:14px; font-weight:bold;'><span>Reservation Deposit (20%):</span><span>" + Number(depositAmount).toLocaleString() + " EGP</span></div>" +
    "</div>" +
    "<label style='display:block; font-size:13px; font-weight:600; margin-bottom:6px;'>Select Payment Method:</label>" +
    "<select id='payMethodSelect' style='width:100%; padding:10px; border-radius:6px; border:1px solid #ccc; font-size:13px; margin-bottom:14px;'>" +
    "<option value='Vodafone Cash'>📱 Vodafone Cash / Orange Cash</option>" +
    "<option value='InstaPay'>⚡ InstaPay Egypt</option>" +
    "<option value='Visa/MasterCard'>💳 Credit / Debit Card (Visa / MasterCard)</option>" +
    "<option value='Meeza'>🇪🇬 Meeza National Card</option>" +
    "</select>" +
    "<label style='display:block; font-size:13px; font-weight:600; margin-bottom:6px;'>Mobile / Card Number:</label>" +
    "<input type='text' id='payAccountNum' placeholder='e.g. 01012345678 or 4111...' style='width:100%; padding:10px; border-radius:6px; border:1px solid #ccc; font-size:13px; margin-bottom:16px;' value='01099887766'/>" +
    "<button onclick='processPayment(\"" + propId + "\", \"" + title + "\", " + depositAmount + ")' style='width:100%; background:#27ae60; color:white; padding:12px; border:none; border-radius:8px; font-weight:bold; font-size:14px; cursor:pointer;'>Pay Deposit & Reserve</button>" +
    "</div></div>";

  var div = document.createElement("div");
  div.id = "paymentModalContainer";
  div.innerHTML = modalHTML;
  document.body.appendChild(div);
};

window.closePaymentModal = function() {
  var c = document.getElementById("paymentModalContainer");
  if (c) c.remove();
};

window.processPayment = function(propId, title, amount) {
  var user = JSON.parse(localStorage.getItem("currentUser") || "{}");
  var method = document.getElementById("payMethodSelect").value;
  var refNum = "UN-" + Math.floor(100000 + Math.random() * 900000);

  var overlay = document.getElementById("paymentModalOverlay");
  if (overlay) {
    overlay.innerHTML =
      "<div style='background:white; border-radius:12px; width:100%; max-width:440px; padding:28px; text-align:center; box-shadow:0 10px 30px rgba(0,0,0,0.3); font-family:inherit;'>" +
      "<div style='font-size:48px; color:#27ae60; margin-bottom:12px;'>🎉</div>" +
      "<h2 style='color:#27ae60; font-size:20px; margin-bottom:8px;'>Booking Confirmed!</h2>" +
      "<p style='font-size:13px; color:#666; margin-bottom:16px;'>Your deposit of <strong>" + Number(amount).toLocaleString() + " EGP</strong> was received via " + method + ".</p>" +
      "<div style='background:#f8f9fa; padding:14px; border-radius:8px; font-size:13px; text-align:left; border:1px solid #e0e0e0; margin-bottom:16px;'>" +
      "<div><strong>Receipt No:</strong> " + refNum + "</div>" +
      "<div><strong>Student:</strong> " + user.name + " (" + user.email + ")</div>" +
      "<div><strong>Property:</strong> " + title + "</div>" +
      "<div><strong>Date:</strong> " + new Date().toLocaleDateString() + "</div>" +
      "</div>" +
      "<button onclick='closePaymentModal()' style='background:#2196f3; color:white; border:none; padding:10px 24px; border-radius:6px; font-weight:bold; cursor:pointer;'>Done & Return</button>" +
      "</div>";
  }
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
