// add-property.js — Add Property page integration with real backend API
// Requires: user must be logged in AND have Owner role
// Image upload is deferred — see inline note

(function () {
  "use strict";

  // ── State ──────────────────────────────────────────────────────────
  var isPageReady = false;
  var locationsMap = null;   // nameEn → guid
  var amenitiesMap = null;   // nameEn → guid
  var isSubmitting = false;

  // ── DOM refs (set after render) ────────────────────────────────────
  var formContent = null;

  // ── Helpers ─────────────────────────────────────────────────────────
  function el(id) { return document.getElementById(id); }

  function showError(msg) {
    var e = el("propError");
    if (e) { e.textContent = msg; e.style.display = "block"; }
  }

  function hideMessages() {
    var e = el("propError");  if (e) e.style.display = "none";
    var s = el("propSuccess"); if (s) s.style.display = "none";
  }

  function getVal(id) { var i = el(id); return i ? i.value.trim() : ""; }

  // ── Auth guard ──────────────────────────────────────────────────────
  function checkAuth() {
    var user = JSON.parse(localStorage.getItem("currentUser") || "null");
    if (!user) {
      renderNotLogged();
      return null;
    }
    if (user.role !== "owner") {
      renderNotOwner();
      return null;
    }
    return user;
  }

  function renderNotLogged() {
    formContent.innerHTML =
      '<div class="not-logged">' +
        '<div style="font-size:60px;margin-bottom:16px;">🔒</div>' +
        '<h2>' + window.t('notLogHead') + '</h2>' +
        '<p style="color:var(--text-light);margin-bottom:24px;">' + window.t('notLogSub') + '</p>' +
        '<a href="login.html" class="btn btn-primary">' + window.t('btnLoginNow') + '</a>' +
      '</div>';
  }

  function renderNotOwner() {
    formContent.innerHTML =
      '<div class="not-logged">' +
        '<div style="font-size:60px;margin-bottom:16px;">🚫</div>' +
        '<h2>' + window.t('accessDenied') + '</h2>' +
        '<p style="color:var(--text-light);margin-bottom:24px;">' + window.t('onlyOwner') + '</p>' +
        '<a href="profile.html" class="btn btn-primary">' + window.t('backToProfile') + '</a>' +
      '</div>';
  }

  // ── Catalog data loading (shared with listings.js pattern) ─────────
  function fetchLocationsMap() {
    if (locationsMap) return Promise.resolve(locationsMap);
    return window.UniNestApi.request("/api/v1/catalog/locations").then(function (result) {
      var items = result.items || result || [];
      locationsMap = {};
      for (var i = 0; i < items.length; i++) {
        var loc = items[i];
        var name = loc.nameEn || loc.name || "";
        var id = loc.id || "";
        if (name && id) locationsMap[name] = id;
      }
      return locationsMap;
    }).catch(function (e) {
      console.warn("Could not load locations map:", e);
      locationsMap = {};
      return locationsMap;
    });
  }

  function fetchAmenitiesMap() {
    if (amenitiesMap) return Promise.resolve(amenitiesMap);
    return window.UniNestApi.request("/api/v1/catalog/amenities").then(function (result) {
      var items = result.items || result || [];
      amenitiesMap = {};
      for (var i = 0; i < items.length; i++) {
        var am = items[i];
        var name = am.nameEn || am.name || "";
        var id = am.id || "";
        if (name && id) amenitiesMap[name] = id;
      }
      return amenitiesMap;
    }).catch(function (e) {
      console.warn("Could not load amenities map:", e);
      amenitiesMap = {};
      return amenitiesMap;
    });
  }

  // ── Build form HTML ─────────────────────────────────────────────────
  function buildForm() {
    var locOptions = '<option value="">— Select Location —</option>';
    // We'll fill locations dynamically after fetch, but render a placeholder select
    var amenitiesHtml = '<div style="color:var(--text-light);font-size:14px;">Loading amenities...</div>';

    return (
      '<div class="add-card">' +
        '<h2>' + window.t('addPropHead') + '</h2>' +
        '<p>' + window.t('addPropSub') + '</p>' +

        // Basic info section
        '<div class="section-label">' + window.t('basicInfo') + '</div>' +

        // Title
        '<div class="form-group">' +
          '<label>' + window.t('propTitle') + '</label>' +
          '<input type="text" id="propTitle" placeholder="..." maxlength="200" />' +
        '</div>' +

        // Price + Rooms row
        '<div class="form-row">' +
          '<div class="form-group">' +
            '<label>' + window.t('propPrice') + '</label>' +
            '<input type="number" id="propPrice" placeholder="..." min="1" max="1000000" />' +
          '</div>' +
          '<div class="form-group">' +
            '<label>' + window.t('propRooms') + '</label>' +
            '<input type="number" id="propRooms" placeholder="..." min="1" max="100" />' +
          '</div>' +
        '</div>' +

        // Location (dynamically populated)
        '<div class="form-group">' +
          '<label>' + window.t('propLocation') + '</label>' +
          '<select id="propLocation">' + locOptions + '</select>' +
        '</div>' +

        // Listing type
        '<div class="form-group">' +
          '<label>' + window.t('propType') + '</label>' +
          '<select id="propType">' +
            '<option value="entireApartment">' + window.t('optApt') + '</option>' +
            '<option value="privateRoom">' + window.t('optSingle') + '</option>' +
            '<option value="sharedBed">' + window.t('optShared') + '</option>' +
          '</select>' +
        '</div>' +

        // Gender policy (NEW — backend-required field)
        '<div class="form-group">' +
          '<label>' + window.t('genderPolicyLbl') + '</label>' +
          '<select id="propGenderPolicy">' +
            '<option value="Any">' + window.t('genderPolicyAny') + '</option>' +
            '<option value="MaleOnly">' + window.t('optMale') + '</option>' +
            '<option value="FemaleOnly">' + window.t('optFemale') + '</option>' +
          '</select>' +
        '</div>' +

        // Description
        '<div class="form-group">' +
          '<label>' + window.t('propDesc') + '</label>' +
          '<textarea id="propDesc" placeholder="..." maxlength="4000"></textarea>' +
        '</div>' +

        // Phone
        '<div class="form-group">' +
          '<label>' + window.t('propPhone') + '</label>' +
          '<input type="text" id="propPhone" placeholder="e.g. 01012345678" maxlength="30" />' +
        '</div>' +

        // Image upload — DEFERRED (not yet supported by backend)
        '<div class="form-group disabled-field">' +
          '<label><i class="fa-solid fa-image"></i> ' + window.t('propImage') + '</label>' +
          '<input type="file" id="propImageFile" accept="image/*" disabled />' +
          '<div class="image-note">' + window.t('imageNotAvailable') + '</div>' +
        '</div>' +

        // Amenities section
        '<hr class="section-divider" />' +
        '<div class="section-label">' + window.t('propAmenities') + '</div>' +
        '<div class="form-group">' +
          '<div class="amenities-check" id="amenitiesContainer">' + amenitiesHtml + '</div>' +
        '</div>' +

        // Submit + messages
        '<button class="form-submit" id="submitBtn" onclick="window.submitProperty()">' + window.t('btnSubmitProp') + '</button>' +
        '<div class="error-msg" id="propError"></div>' +
        '<div class="success-msg" id="propSuccess"></div>' +
      '</div>'
    );
  }

  // ── Populate locations dropdown ─────────────────────────────────────
  function populateLocations(locations) {
    var select = el("propLocation");
    if (!select) return;
    select.innerHTML = '<option value="">— Select Location —</option>';
    var names = Object.keys(locations);
    // Sort alphabetically for consistent UI
    names.sort();
    for (var i = 0; i < names.length; i++) {
      var name = names[i];
      var id = locations[name];
      var opt = document.createElement("option");
      opt.value = id;
      opt.textContent = name;
      select.appendChild(opt);
    }
  }

  // ── Populate amenities checkboxes ───────────────────────────────────
  function populateAmenities(amenities) {
    var container = el("amenitiesContainer");
    if (!container) return;
    container.innerHTML = "";
    var entries = Object.keys(amenities);
    if (entries.length === 0) {
      container.innerHTML = '<div style="color:var(--text-light);font-size:14px;">No amenities available.</div>';
      return;
    }
    // Sort by name for consistent UI
    entries.sort();
    for (var i = 0; i < entries.length; i++) {
      var name = entries[i];
      var id = amenities[name];
      var label = document.createElement("label");
      label.className = "amenity-item";
      var cb = document.createElement("input");
      cb.type = "checkbox";
      cb.value = id;  // Store the GUID as the value — resolved at submit time
      cb.dataset.amenityName = name;
      var span = document.createElement("span");
      span.textContent = name;
      label.appendChild(cb);
      label.appendChild(span);
      container.appendChild(label);
    }
  }

  // ── Submit handler ──────────────────────────────────────────────────
  window.submitProperty = function () {
    if (isSubmitting) return;
    hideMessages();

    var title = getVal("propTitle");
    var priceStr = getVal("propPrice");
    var roomsStr = getVal("propRooms");
    var locationId = el("propLocation").value;
    var listingType = el("propType").value;
    var genderPolicy = el("propGenderPolicy").value;
    var desc = getVal("propDesc");
    var phone = getVal("propPhone");

    var imageFileInput = el("propImageFile");
    var imageFile = (imageFileInput && imageFileInput.files && imageFileInput.files.length > 0) ? imageFileInput.files[0] : null;

    // Basic required-field validation (before API call)
    if (!title || !priceStr || !roomsStr || !locationId || !desc) {
      showError(window.t('errProp'));
      return;
    }

    var price = parseInt(priceStr, 10);
    var rooms = parseInt(roomsStr, 10);
    if (isNaN(price) || price < 1 || price > 1000000) {
      showError(window.t('errProp'));
      return;
    }
    if (isNaN(rooms) || rooms < 1 || rooms > 100) {
      showError(window.t('errProp'));
      return;
    }

    var btn = el("submitBtn");
    btn.disabled = true;
    btn.textContent = window.t('submitting');
    isSubmitting = true;

    // Resolve amenities → GUIDs
    var amenityIds = [];
    var checkedBoxes = document.querySelectorAll("#amenitiesContainer input:checked");
    for (var i = 0; i < checkedBoxes.length; i++) {
      var guid = checkedBoxes[i].value;
      if (guid) amenityIds.push(guid);
    }

    // Build the CreateListingRequest payload — exact match to backend DTO
    var payload = {
      titleEn: title,
      titleAr: null,
      descriptionEn: desc,
      descriptionAr: null,
      listingType: listingType,       // "entireApartment" | "privateRoom" | "sharedBed"
      genderPolicy: genderPolicy,      // "Any" | "MaleOnly" | "FemaleOnly"
      monthlyRent: price,
      roomCount: rooms,
      totalBeds: rooms,               // Auto-set = roomCount
      availableBeds: rooms,           // Auto-set = roomCount
      locationId: locationId,          // GUID from locations catalog
      universityId: null,
      contactPhone: phone || null,
      amenityIds: amenityIds.length > 0 ? amenityIds : null  // GUIDs from amenities catalog
    };

    // Step 1: Upload image file if provided
    var uploadPromise = imageFile ? window.UniNestApi.uploadImage(imageFile) : Promise.resolve(null);

    uploadPromise.then(function (mediaResult) {
      var mediaAssetId = mediaResult ? mediaResult.id : null;

      // Step 2: Create listing
      return window.UniNestApi.createListing(payload).then(function (created) {
        var listingId = created ? created.id : null;

        if (mediaAssetId && listingId) {
          // Step 3: Associate image with listing
          return window.UniNestApi.addListingImage(listingId, mediaAssetId, true).then(function () {
            showSuccessAndRedirect();
          }).catch(function (imgErr) {
            console.warn("Listing created, but image association failed:", imgErr);
            // User Clarification #1: Show clear warning message if image association fails
            var s = el("propSuccess");
            if (s) {
              s.style.display = "block";
              s.innerHTML = "✅ Listing created successfully! <br/><span style='font-size:13px; color:#856404; background:#fff3cd; padding:4px 8px; border-radius:4px; display:inline-block; margin-top:6px;'>⚠️ The image couldn't be attached — you can add it later.</span>";
            }
            setTimeout(function () { window.location.href = "profile.html"; }, 2500);
          });
        } else {
          showSuccessAndRedirect();
        }
      });
    }).catch(function (err) {
      // Handle backend validation errors
      var msg = window.t('errProp');
      if (err.payload && err.payload.errors) {
        // ModelState errors from backend
        var keys = Object.keys(err.payload.errors);
        if (keys.length > 0) {
          var firstErrors = err.payload.errors[keys[0]];
          if (firstErrors && firstErrors.length > 0) {
            msg = firstErrors[0];
          }
        }
      } else if (err.payload && err.payload.detail) {
        msg = err.payload.detail;
      } else if (err.status === 401) {
        msg = window.t('sessionExpired');
      }
      showError(msg);
      console.error("Create listing failed:", err);
    }).finally(function () {
      btn.disabled = false;
      btn.textContent = window.t('btnSubmitProp');
      isSubmitting = false;
    });
  };

  // ── Page init ───────────────────────────────────────────────────────
  window.onload = function () {
    var user = JSON.parse(localStorage.getItem("currentUser") || "null");
    var navBtn = el("navLoginBtn");
    if (user && navBtn) {
      navBtn.textContent = user.name;
      navBtn.href = "profile.html";
    }

    var authUser = checkAuth();
    if (!authUser) return; // renders not-logged or not-owner message

    formContent = el("pageContent");
    formContent.innerHTML = buildForm();

    // Fetch catalog data and populate dropdowns/checkboxes
    Promise.all([
      fetchLocationsMap(),
      fetchAmenitiesMap()
    ]).then(function (results) {
      populateLocations(results[0]);
      populateAmenities(results[1]);
      isPageReady = true;
    }).catch(function (e) {
      console.error("Failed to initialize add-property page:", e);
      showError(window.t('loadError'));
    });
  };
})();
