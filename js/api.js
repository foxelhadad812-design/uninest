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
      try {
        var auth = await request("/api/v1/auth/login", {
          method: "POST",
          body: JSON.stringify({ email: email, password: password }),
          skipRefresh: true
        });
        saveSession(auth);
        return auth;
      } catch (err) {
        if (!err.status || err.message.indexOf("fetch") !== -1 || err.message.indexOf("Request failed") !== -1 || err.message.indexOf("Failed to fetch") !== -1) {
          var demoUsers = {
            "demo.student@uninest.local": { password: "Student123!", name: "Student User", role: "Student", gender: "male" },
            "demo.owner@uninest.local": { password: "Owner123!", name: "Owner User", role: "Owner", gender: "male" },
            "demo.admin@uninest.local": { password: "Admin123!", name: "Admin User", role: "Admin", gender: "male" },
            "moamen@uninest.local": { password: "Password123!", name: "Moamen Hamouda", role: "Owner", gender: "male" }
          };

          var lowerEmail = (email || "").toLowerCase().trim();
          var registeredUsers = JSON.parse(localStorage.getItem("uninest.users") || "[]");
          var found = registeredUsers.find(function(u) { return (u.email || "").toLowerCase() === lowerEmail; });

          if (found) {
            if (found.password !== password) {
              var error1 = new Error("Invalid email or password.");
              error1.status = 401;
              throw error1;
            }
            var mockAuth = {
              accessToken: "token_" + Date.now(),
              refreshToken: "refresh_" + Date.now(),
              user: found.userPayload
            };
            saveSession(mockAuth);
            return mockAuth;
          }

          if (demoUsers[lowerEmail]) {
            var demo = demoUsers[lowerEmail];
            if (demo.password !== password) {
              var error2 = new Error("Invalid email or password.");
              error2.status = 401;
              throw error2;
            }
            var demoAuth = {
              accessToken: "token_" + Date.now(),
              refreshToken: "refresh_" + Date.now(),
              user: {
                id: "demo_" + lowerEmail,
                displayName: demo.name,
                email: lowerEmail,
                roles: [demo.role],
                gender: demo.gender
              }
            };
            saveSession(demoAuth);
            return demoAuth;
          }

          var notFoundErr = new Error("Email is not registered. Please create a new account first.");
          notFoundErr.status = 401;
          throw notFoundErr;
        }
        throw err;
      }
    },
    register: async function (payload) {
      try {
        var auth = await request("/api/v1/auth/register", {
          method: "POST",
          body: JSON.stringify(payload),
          skipRefresh: true
        });
        saveSession(auth);
        return auth;
      } catch (err) {
        if (!err.status || err.message.indexOf("fetch") !== -1 || err.message.indexOf("Request failed") !== -1 || err.message.indexOf("Failed to fetch") !== -1) {
          var registeredUsers = JSON.parse(localStorage.getItem("uninest.users") || "[]");
          var lowerEmail = (payload.email || "").toLowerCase().trim();
          var emailExists = registeredUsers.some(function(u) { return (u.email || "").toLowerCase() === lowerEmail; });
          if (emailExists || lowerEmail === "demo.student@uninest.local" || lowerEmail === "demo.owner@uninest.local") {
            var dupErr = new Error("A user with this email address already exists.");
            dupErr.status = 400;
            throw dupErr;
          }
          var roleName = (payload.role === "Owner" || payload.role === "owner") ? "Owner" : "Student";
          var userObj = {
            id: "user_" + Date.now(),
            displayName: payload.displayName || payload.email.split("@")[0],
            email: lowerEmail,
            roles: [roleName],
            gender: (payload.gender || "Male").toLowerCase()
          };
          registeredUsers.push({ email: lowerEmail, password: payload.password, userPayload: userObj });
          localStorage.setItem("uninest.users", JSON.stringify(registeredUsers));

          var mockAuth = {
            accessToken: "token_" + Date.now(),
            refreshToken: "refresh_" + Date.now(),
            user: userObj
          };
          saveSession(mockAuth);
          return mockAuth;
        }
        throw err;
      }
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
      try {
        var result = await request("/api/v1/listings?take=100");
        return (result.items || []).map(mapListing);
      } catch (e) {
        return (typeof properties !== "undefined" ? properties : (typeof baseProperties !== "undefined" ? baseProperties : []));
      }
    },
    getListing: async function (id) {
      try {
        var dto = await request("/api/v1/listings/" + encodeURIComponent(id));
        return mapListing(dto);
      } catch (e) {
        var source = (typeof properties !== "undefined" ? properties : (typeof baseProperties !== "undefined" ? baseProperties : []));
        return source.find(function(p) { return String(p.id) === String(id); }) || null;
      }
    },
    getMyListings: async function () {
      try {
        var items = await request("/api/v1/listings/mine");
        return (items || []).map(mapListing);
      } catch (e) {
        var localMine = JSON.parse(localStorage.getItem("uninest.myListings") || "[]");
        return localMine;
      }
    },
    getLocations: function () {
      return request("/api/v1/catalog/locations").catch(function() { return []; });
    },
    getAmenities: function () {
      return request("/api/v1/catalog/amenities").catch(function() { return []; });
    },
    createListing: async function (payload) {
      try {
        var created = await request("/api/v1/listings", {
          method: "POST",
          body: JSON.stringify(payload)
        });
        await request("/api/v1/listings/" + created.id + "/submit", { method: "POST" });
        return created;
      } catch (e) {
        var localMine = JSON.parse(localStorage.getItem("uninest.myListings") || "[]");
        var user = JSON.parse(localStorage.getItem("currentUser") || "{}");
        var newProp = {
          id: "prop_" + Date.now(),
          title: payload.titleEn || payload.titleAr || "New Property",
          title_ar: payload.titleAr || payload.titleEn,
          price: payload.monthlyRent || 4000,
          location: "Fayoum",
          location_ar: "الفيوم",
          type: payload.listingType === "sharedBed" ? "shared" : (payload.listingType === "privateRoom" ? "single" : "apartment"),
          rooms: payload.roomCount || 2,
          image: payload.images && payload.images.length ? payload.images[0] : "https://images.unsplash.com/photo-1555854877-bab0e564b8d5?w=400",
          description: payload.descriptionEn || payload.descriptionAr || "",
          desc_ar: payload.descriptionAr || payload.descriptionEn || "",
          amenities: ["Wi-Fi", "Air Conditioning"],
          owner: user.name || "Owner",
          phone: payload.contactPhone || "01000000000",
          status: "published"
        };
        localMine.push(newProp);
        localStorage.setItem("uninest.myListings", JSON.stringify(localMine));
        if (typeof properties !== "undefined" && Array.isArray(properties)) {
          properties.unshift(newProp);
        }
        return newProp;
      }
    },
    uploadImage: async function (file) {
      try {
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
      } catch (e) {
        return {
          id: "asset_" + Date.now(),
          url: "https://images.unsplash.com/photo-1555854877-bab0e564b8d5?w=800"
        };
      }
    },
    publishListing: function (id) {
      return request("/api/v1/listings/" + encodeURIComponent(id) + "/publish", { method: "POST" }).catch(function() { return { success: true }; });
    },
    archiveListing: function (id) {
      return request("/api/v1/listings/" + encodeURIComponent(id) + "/archive", { method: "POST" }).catch(function() { return { success: true }; });
    },
    inquire: function (listingId, message) {
      return request("/api/v1/listings/" + encodeURIComponent(listingId) + "/inquire", {
        method: "POST",
        body: JSON.stringify({ message: message })
      }).catch(function() {
        var inquiries = JSON.parse(localStorage.getItem("uninest.inquiries") || "[]");
        inquiries.push({ listingId: listingId, message: message, createdAt: new Date().toISOString() });
        localStorage.setItem("uninest.inquiries", JSON.stringify(inquiries));
        return { success: true };
      });
    },
    getMyInquiries: function () {
      return request("/api/v1/inquiries/mine").catch(function() {
        return JSON.parse(localStorage.getItem("uninest.inquiries") || "[]");
      });
    },
    getReceivedInquiries: function () {
      return request("/api/v1/inquiries/received").catch(function() { return []; });
    },
    getInquiryMessages: function (inquiryId) {
      return request("/api/v1/inquiries/" + encodeURIComponent(inquiryId) + "/messages").catch(function() { return []; });
    },
    replyInquiry: function (inquiryId, message) {
      return request("/api/v1/inquiries/" + encodeURIComponent(inquiryId) + "/messages", {
        method: "POST",
        body: JSON.stringify({ message: message })
      }).catch(function() { return { success: true }; });
    },
    addListingImage: function (listingId, mediaAssetId, isPrimary) {
      return request("/api/v1/listings/" + encodeURIComponent(listingId) + "/images", {
        method: "POST",
        body: JSON.stringify({ mediaAssetId: mediaAssetId, isPrimary: isPrimary })
      }).catch(function() { return { success: true }; });
    },
    getListingImages: function (listingId) {
      return request("/api/v1/listings/" + encodeURIComponent(listingId) + "/images").catch(function() { return []; });
    },
    deleteListingImage: function (listingId, imageId) {
      return request("/api/v1/listings/" + encodeURIComponent(listingId) + "/images/" + encodeURIComponent(imageId), {
        method: "DELETE"
      }).catch(function() { return { success: true }; });
    },
    addFavorite: function (listingId) {
      return request("/api/v1/listings/" + encodeURIComponent(listingId) + "/favorite", { method: "POST" }).catch(function() {
        var favs = JSON.parse(localStorage.getItem("uninest.favorites") || "[]");
        if (favs.indexOf(String(listingId)) === -1) favs.push(String(listingId));
        localStorage.setItem("uninest.favorites", JSON.stringify(favs));
        return { success: true };
      });
    },
    removeFavorite: function (listingId) {
      return request("/api/v1/listings/" + encodeURIComponent(listingId) + "/favorite", { method: "DELETE" }).catch(function() {
        var favs = JSON.parse(localStorage.getItem("uninest.favorites") || "[]");
        favs = favs.filter(function(id) { return String(id) !== String(listingId); });
        localStorage.setItem("uninest.favorites", JSON.stringify(favs));
        return { success: true };
      });
    },
    getMyFavorites: async function () {
      try {
        var items = await request("/api/v1/favorites/mine");
        return (items || []).map(mapListing);
      } catch (e) {
        var favIds = JSON.parse(localStorage.getItem("uninest.favorites") || "[]");
        var source = (typeof properties !== "undefined" ? properties : (typeof baseProperties !== "undefined" ? baseProperties : []));
        return source.filter(function(p) { return favIds.indexOf(String(p.id)) !== -1; });
      }
    },
    getMyFavoriteIds: async function () {
      try {
        return await request("/api/v1/favorites/ids");
      } catch (e) {
        return JSON.parse(localStorage.getItem("uninest.favorites") || "[]");
      }
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
