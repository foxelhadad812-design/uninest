// أول ما صفحة التفاصيل تفتح
window.onload = function() {
  // بنجيب رقم الـ ID بتاع العقار من اللينك اللي فوق
  var params = new URLSearchParams(window.location.search);
  var id = parseInt(params.get("id"));
  var property = null;

  // بنلف على العقارات لحد ما تلاقي التطابق في البيانات عن طريق الـ ID
  for (var i = 0; i < properties.length; i++) {
    if (properties[i].id === id) {
      property = properties[i];
      break;
    }
  }

  var content = document.getElementById("detailsContent");

  // لو ملحقناش أي ID ولا أي عقار صح، هنطبع رسالة ان المكان ده مش موجود (error fallback)
  if (!property) {
    content.innerHTML = "<div class='not-found'>😕 Property not found.</div>";
    return;
  }

  var amenitiesHTML = "";
  for (var i = 0; i < property.amenities.length; i++) {
    amenitiesHTML += "<span class='amenity-tag'><i class='fa-solid fa-check-circle'></i> " + property.amenities[i] + "</span>";
  }

  var lang = localStorage.getItem("lang") || "en";
  var tTitle = lang === "ar" && property.title_ar ? property.title_ar : property.title;
  var tLoc = lang === "ar" && property.location_ar ? property.location_ar : property.location;
  var tDesc = lang === "ar" && property.desc_ar ? property.desc_ar : property.description;
  var tType = property.type;
  if (property.type === 'apartment') tType = window.t('optApt');
  if (property.type === 'single') tType = window.t('optSingle');
  if (property.type === 'shared') tType = window.t('optShared');

  var htmlReviews = "";
  var femaleNames = ["Salma Y.", "Nour S.", "Aya M.", "Fatma A.", "Hadeer K.", "Mai E.", "Hanan F.", "Dina S.", "Sara A.", "Noha A.", "Rana T.", "Mariam G.", "Heba W.", "Yasmine H.", "Esraa B.", "Maha N."];
  var maleNames = ["Ahmed Y.", "Omar M.", "Ziad A.", "Kareem T.", "Hassan M.", "Mohamed R.", "Nader S.", "Mahmoud E.", "Youssef I.", "Sayed F.", "Tamer H.", "Khaled Y.", "Ali F.", "Ramy S.", "Amr D.", "Mostafa K."];
  
  var getNames = function(propGender) {
    if (propGender === 'female') return femaleNames;
    if (propGender === 'male') return maleNames;
    return maleNames.concat(femaleNames);
  };
  
  var availableNames = getNames(property.gender);
  var name1 = availableNames[(property.id * 3) % availableNames.length];
  var name2 = availableNames[((property.id * 3) + 1) % availableNames.length];
  var name3 = availableNames[((property.id * 3) + 2) % availableNames.length];

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

  var count = (property.id % 2 === 0) ? 3 : 2;
  for (var k=0; k<count; k++) {
    var nLabel = [name1, name2, name3][k];
    var tLabel = (k+1) + " months ago";
    var txtObj = texts[(property.id + k) % texts.length];
    var txt = lang === "ar" ? txtObj.ar : txtObj.r;

    var starsHtml = "";
    for(var i=1; i<=5; i++) {
      if(i <= txtObj.s) starsHtml += "<span style='color:#f39c12; font-size:14px; margin-right:2px;'>★</span>";
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

  // بنتأكد هل المستخدم مسجل دخوله وبنسحب الداتا بتاعته من LocalStorage
  var user = JSON.parse(localStorage.getItem("currentUser") || "null");
  var userGender = user ? user.gender : null;
  
  // بنعمل Validation مهم جداً: هل نوع العقار (مخصص ولاد ولا بنات) يخالف أكونت اليوزر اللي فاتح؟
  var isGenderMismatch = false;
  if (property.gender && userGender && property.gender !== userGender) {
    isGenderMismatch = true;
  }

  // بناء أزرار التواصل والحجز.. لو فيه تعارض في النوع، هنقفل الاتصال خالص ونظهر رسالة ممنوع!
  var actionButtonsHTML = "";
  if (isGenderMismatch) {
    var genderWord = property.gender === 'female' ? 'إناث' : 'ذكور';
    var genderWordEn = property.gender === 'female' ? 'Females' : 'Males';
    var errorTxt = lang === "ar" ? "❌ هذا السكن مخصص للـ (" + genderWord + ") فقط" 
                                 : "❌ This property is for " + genderWordEn + " only";
    actionButtonsHTML = "<button class='contact-btn' style='background:#f8d7da; color:#721c24; cursor:not-allowed; border:none; width:100%; margin-top:20px;' disabled><b>" + errorTxt + "</b></button>";
  } else {
    actionButtonsHTML = 
      "<a href='tel:" + property.phone + "' class='contact-btn btn-call'><i class='fa-solid fa-phone'></i> Call</a>" +
      "<button class='contact-btn btn-favorite' onclick='addToFavorites(" + property.id + ")'><i class='fa-solid fa-heart'></i> " + window.t('favs') + "</button>" +
      "<div class='discount-banner' style='margin-top: 15px; padding: 15px; background: rgba(33, 150, 243, 0.1); border-radius: 8px; border: 1px dashed #2196f3; text-align: center;'>" +
        "<div style='font-size:14px; font-weight: 600; color:#1976d2; margin-bottom: 8px;'>" + window.t('eeluPromo') + "</div>" +
        "<button class='contact-btn' style='background:#1976d2; color:white; font-size:13px; margin:0;' onclick='showDiscountForm()'><i class='fa-solid fa-tag'></i> " + window.t('applyDiscountBtn') + "</button>" +
      "</div>" +
      "<div id='discountForm' style='display:none; margin-top: 15px; background: var(--bg); padding: 15px; border-radius: 8px; font-size: 13px;'>" +
        "<p style='margin-bottom: 15px; color: var(--text-light);'>" + window.t('discountModalDesc') + "</p>" +
        "<label style='display:block; margin-bottom: 6px; font-weight:600;'>" + window.t('uniIdLbl') + "</label>" +
        "<input type='file' accept='image/*' style='margin-bottom: 15px; width: 100%; border: 1px solid var(--border); padding: 8px;'>" +
        "<label style='display:block; margin-bottom: 6px; font-weight:600;'>" + window.t('nidLbl') + "</label>" +
        "<input type='file' accept='image/*' style='margin-bottom: 15px; width: 100%; border: 1px solid var(--border); padding: 8px;'>" +
        "<button class='contact-btn btn-call' onclick='submitDiscountReq()'>" + window.t('btnSendReq') + "</button>" +
      "</div>";
  }

  content.innerHTML =
    "<div class='details-layout'>" +
      "<div>" +
        "<img src='" + property.image + "' alt='" + property.title + "' class='details-img'/>" +
        "<h1 class='details-title'>" + tTitle + "</h1>" +
        "<div class='details-price' style='display:flex; align-items:center; gap:12px; flex-wrap:wrap; margin-bottom:16px;'>" + 
          "<span>" + property.price + " " + window.t('egpMonth') + "</span>" +
          "<div style='font-size:14px; font-weight:600; color:var(--text-light); display:flex; gap:10px; align-items:center; background:var(--bg); padding:6px 14px; border-radius:20px; border:1px solid var(--border);'>" +
            "<span title='US Dollar' style='display:flex; align-items:center; gap:4px;'>🇺🇸 ~$" + usdPrice + "</span> <span style='color:var(--border)'>|</span> " +
            "<span title='Euro' style='display:flex; align-items:center; gap:4px;'>🇪🇺 ~€" + eurPrice + "</span> <span style='color:var(--border)'>|</span> " +
            "<span title='Saudi Riyal' style='display:flex; align-items:center; gap:4px;'>🇸🇦 ~" + sarPrice + " SAR</span>" +
          "</div>" +
        "</div>" +
        "<div class='details-location'><i class='fa-solid fa-location-dot'></i> " + tLoc + "</div>" +
        "<div class='details-section'>" +
          "<h3>" + window.t('propDesc') + "</h3>" +
          "<p>" + tDesc + "</p>" +
        "</div>" +
        "<div class='details-section'>" +
          "<h3>" + window.t('propAmenities') + "</h3>" +
          "<div class='amenities-list'>" + amenitiesHTML + "</div>" +
        "</div>" +
        "<div class='details-section'>" +
          "<h3>" + window.t('btnView') + "</h3>" +
          "<p>" + window.t('propRooms') + ": " + property.rooms + "</p>" +
          "<p>" + window.t('propType') + ": " + tType + "</p>" +
        "</div>" +
        "<div class='details-section'>" +
          "<h3><i class='fa-solid fa-star' style='color:#f39c12'></i> " + window.t('studentReviews') + "</h3>" +
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
              "<div class='owner-label'>" + window.t('optOwner') + "</div>" +
            "</div>" +
          "</div>" +
          actionButtonsHTML +
        "</div>" +
      "</div>" +
    "</div>";
};

function addToFavorites(id) {
  var favorites = JSON.parse(localStorage.getItem("favorites") || "[]");
  if (favorites.indexOf(id) === -1) {
    favorites.push(id);
    localStorage.setItem("favorites", JSON.stringify(favorites));
    alert("Added to favorites! ❤️");
  } else {
    alert("Already in favorites!");
  }
}

function showDiscountForm() {
  var form = document.getElementById('discountForm');
  if (form) {
    form.style.display = form.style.display === 'none' ? 'block' : 'none';
  }
}

function submitDiscountReq() {
  alert(window.t('discountSuccess'));
  document.getElementById('discountForm').style.display = 'none';
}