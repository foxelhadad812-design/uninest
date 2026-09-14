(function (global) {
  var ACCESS_KEY = "uninest.accessToken";
  var REFRESH_KEY = "uninest.refreshToken";
  var USER_KEY = "currentUser";
  var BASE_KEY = "uninest.apiBase";
  var PLACEHOLDER_IMAGE = "https://images.unsplash.com/photo-1555854877-bab0e564b8d5?w=400";

  function apiBase() {
    var custom = localStorage.getItem(BASE_KEY);
    if (custom) return custom;
    if (typeof window !== "undefined" && window.location && window.location.origin) {
      if (window.location.port === "5157") {
        return window.location.origin;
      }
      var host = window.location.hostname;
      if (host && host !== "localhost" && host !== "127.0.0.1" && host !== "" && !host.endsWith(".github.io")) {
        return window.location.origin;
      }
    }
    return "http://localhost:5157";
  }

  function getAccessToken() {
    return localStorage.getItem(ACCESS_KEY);
  }

  function mapSessionUser(user) {
    var roles = user.roles || [];
    var role = "student";
    if (roles.indexOf("Admin") !== -1) role = "admin";
    else if (roles.indexOf("Owner") !== -1) role = "owner";
    else if (roles.indexOf("Student") !== -1) role = "student";

    var gender = (user.gender || "").toLowerCase();
    if (gender === "prefernottosay") gender = "";

    return {
      id: user.id,
      name: user.displayName,
      email: user.email,
      role: role,
      gender: gender,
      roles: roles
    };
  }

  function saveSession(auth) {
    localStorage.setItem(ACCESS_KEY, auth.accessToken);
    if (auth.refreshToken) localStorage.setItem(REFRESH_KEY, auth.refreshToken);
    localStorage.setItem(USER_KEY, JSON.stringify(mapSessionUser(auth.user)));
  }

  function clearSession() {
    localStorage.removeItem(ACCESS_KEY);
    localStorage.removeItem(REFRESH_KEY);
    localStorage.removeItem(USER_KEY);
  }

  function mapListing(dto) {
    var type = "apartment";
    if (dto.listingType === "privateRoom") type = "single";
    if (dto.listingType === "sharedBed") type = "shared";

    var gender;
    if (dto.genderPolicy === "maleOnly") gender = "male";
    if (dto.genderPolicy === "femaleOnly") gender = "female";

    var amenities = (dto.amenities || []).map(function (item) {
      return typeof item === "string" ? item : item.nameEn;
    });

    var imgUrl = dto.primaryImageUrl || PLACEHOLDER_IMAGE;
    if (imgUrl && typeof imgUrl === "string") {
      imgUrl = imgUrl.trim();
      if (imgUrl.indexOf("/http://") === 0 || imgUrl.indexOf("/https://") === 0) {
        imgUrl = imgUrl.substring(1);
      } else if (imgUrl.indexOf("http") !== 0 && imgUrl.indexOf("/") === 0) {
        imgUrl = apiBase() + imgUrl;
      }
    }

    return {
      id: dto.id,
      title: dto.titleEn,
      title_ar: dto.titleAr,
      price: dto.monthlyRent,
      location: dto.locationNameEn,
      location_ar: dto.locationNameAr,
      locationId: dto.locationId,
      type: type,
      listingType: dto.listingType,
      rooms: dto.roomCount,
      image: imgUrl,
      images: dto.images || [],
      description: dto.descriptionEn,
      desc_ar: dto.descriptionAr,
      amenities: amenities,
      owner: dto.ownerDisplayName,
      phone: dto.contactPhone || "",
      gender: gender,
      status: dto.status
    };
  }

  function parseError(payload, fallback) {
    if (!payload) return fallback;
    if (typeof payload === "string") return payload;
    return payload.detail || payload.title || payload.message || fallback;
  }

  async function request(path, options) {
    options = options || {};
    var headers = Object.assign({ "Accept": "application/json" }, options.headers || {});
    if (options.body && !headers["Content-Type"]) headers["Content-Type"] = "application/json";

    var token = getAccessToken();
    if (token) headers.Authorization = "Bearer " + token;

    var response = await fetch(apiBase() + path, {
      method: options.method || "GET",
      headers: headers,
      body: options.body || undefined
    });

    if (response.status === 401 && !options.skipRefresh && localStorage.getItem(REFRESH_KEY)) {
      var refreshed = await refreshSession();
      if (refreshed) return request(path, Object.assign({}, options, { skipRefresh: true }));
    }

    var payload = null;
    var text = await response.text();
    if (text) {
      try { payload = JSON.parse(text); } catch (e) { payload = text; }
    }

    if (!response.ok) {
      var error = new Error(parseError(payload, "Request failed (" + response.status + ")"));
      error.status = response.status;
      error.payload = payload;
      throw error;
    }

    return payload;
  }

  async function refreshSession() {
    var refreshToken = localStorage.getItem(REFRESH_KEY);
    if (!refreshToken) return false;
    try {
      var auth = await request("/api/v1/auth/refresh", {
        method: "POST",
        body: JSON.stringify({ refreshToken: refreshToken }),
        skipRefresh: true
      });
      saveSession(auth);
      return true;
    } catch (e) {
      clearSession();
      return false;
    }
  }

  var api = {
    apiBase: apiBase,
    mapListing: mapListing,
    saveSession: saveSession,
    clearSession: clearSession,
    request: request,
    login: async function (email, password) {
      var auth = await request("/api/v1/auth/login", {
        method: "POST",
        body: JSON.stringify({ email: email, password: password }),
        skipRefresh: true
      });
      saveSession(auth);
      return auth;
    },
    register: async function (payload) {
      var auth = await request("/api/v1/auth/register", {
        method: "POST",
        body: JSON.stringify(payload),
        skipRefresh: true
      });
      saveSession(auth);
      return auth;
    },
    logout: async function () {
      var refreshToken = localStorage.getItem(REFRESH_KEY);
      try {
        await request("/api/v1/auth/logout", {
          method: "POST",
          body: JSON.stringify({ refreshToken: refreshToken }),
          skipRefresh: true
        });
      } catch (e) { /* still clear local session */ }
      clearSession();
    },
    getPublishedListings: async function () {
      var result = await request("/api/v1/listings?take=100");
      return (result.items || []).map(mapListing);
    },
    getListing: async function (id) {
      var dto = await request("/api/v1/listings/" + encodeURIComponent(id));
      return mapListing(dto);
    },
    getMyListings: async function () {
      var items = await request("/api/v1/listings/mine");
      return (items || []).map(mapListing);
    },
    getLocations: function () {
      return request("/api/v1/catalog/locations");
    },
    getAmenities: function () {
      return request("/api/v1/catalog/amenities");
    },
    createListing: async function (payload) {
      var created = await request("/api/v1/listings", {
        method: "POST",
        body: JSON.stringify(payload)
      });
      await request("/api/v1/listings/" + created.id + "/submit", { method: "POST" });
      return created;
    },
    uploadImage: async function (file) {
      var formData = new FormData();
      formData.append("file", file);
      var token = getAccessToken();
      var headers = {};
      if (token) headers.Authorization = "Bearer " + token;

      var response = await fetch(apiBase() + "/api/v1/media/upload", {
        method: "POST",
        headers: headers,
        body: formData
      });
      if (!response.ok) throw new Error("Image upload failed");
      return await response.json();
    },
    publishListing: function (id) {
      return request("/api/v1/listings/" + encodeURIComponent(id) + "/publish", { method: "POST" });
    },
    archiveListing: function (id) {
      return request("/api/v1/listings/" + encodeURIComponent(id) + "/archive", { method: "POST" });
    },
    inquire: function (listingId, message) {
      return request("/api/v1/listings/" + encodeURIComponent(listingId) + "/inquire", {
        method: "POST",
        body: JSON.stringify({ message: message })
      });
    },
    getMyInquiries: function () {
      return request("/api/v1/inquiries/mine");
    },
    getReceivedInquiries: function () {
      return request("/api/v1/inquiries/received");
    },
    getInquiryMessages: function (inquiryId) {
      return request("/api/v1/inquiries/" + encodeURIComponent(inquiryId) + "/messages");
    },
    replyInquiry: function (inquiryId, message) {
      return request("/api/v1/inquiries/" + encodeURIComponent(inquiryId) + "/messages", {
        method: "POST",
        body: JSON.stringify({ message: message })
      });
    },
    addListingImage: function (listingId, mediaAssetId, isPrimary) {
      return request("/api/v1/listings/" + encodeURIComponent(listingId) + "/images", {
        method: "POST",
        body: JSON.stringify({ mediaAssetId: mediaAssetId, isPrimary: isPrimary })
      });
    },
    getListingImages: function (listingId) {
      return request("/api/v1/listings/" + encodeURIComponent(listingId) + "/images");
    },
    deleteListingImage: function (listingId, imageId) {
      return request("/api/v1/listings/" + encodeURIComponent(listingId) + "/images/" + encodeURIComponent(imageId), {
        method: "DELETE"
      });
    },
    addFavorite: function (listingId) {
      return request("/api/v1/listings/" + encodeURIComponent(listingId) + "/favorite", { method: "POST" });
    },
    removeFavorite: function (listingId) {
      return request("/api/v1/listings/" + encodeURIComponent(listingId) + "/favorite", { method: "DELETE" });
    },
    getMyFavorites: async function () {
      var items = await request("/api/v1/favorites/mine");
      return (items || []).map(mapListing);
    },
    getMyFavoriteIds: function () {
      return request("/api/v1/favorites/ids");
    },
    numericSeed: function (id) {
      var text = String(id);
      var seed = 0;
      for (var i = 0; i < text.length; i++) seed = (seed + text.charCodeAt(i) * (i + 1)) % 100000;
      return seed;
    }
  };

  global.UniNestApi = api;
})(window);
