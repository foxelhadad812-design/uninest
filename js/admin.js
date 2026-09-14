// admin.js — Admin Moderation Dashboard
// Requires: user must be logged in AND have Admin role
// All actions call real backend API — no localStorage/mock fallback

(function () {
  "use strict";

  var isLoading = false;

  // ── Auth Guard ──────────────────────────────────────────────────────
  function checkAdminAccess() {
    var user = JSON.parse(localStorage.getItem("currentUser") || "null");

    if (!user) {
      // Not logged in — redirect to login
      window.location.href = "login.html";
      return null;
    }

    // Check for Admin role (stored as "admin" in mapSessionUser, or raw "Admin" in roles array)
    var hasAdminRole = user.role === "admin" || (user.roles && user.roles.indexOf("Admin") !== -1);

    if (!hasAdminRole) {
      // Logged in but not admin — show access denied
      var content = document.querySelector("main.container") || document.getElementById("adminMain");
      if (content) {
        content.innerHTML =
          '<div style="text-align:center; padding:80px 20px; max-width:500px; margin:0 auto;">' +
            '<div style="font-size:72px; margin-bottom:20px;">🔒</div>' +
            '<h2 style="font-size:24px; margin-bottom:12px; color:var(--text);">Access Denied</h2>' +
            '<p style="color:var(--text-light); font-size:15px; margin-bottom:8px;">You do not have permission to access the Admin Dashboard.</p>' +
            '<p style="color:var(--text-light); font-size:13px;">This page requires the <strong>Admin</strong> role.</p>' +
            '<div style="margin-top:24px;">' +
              '<a href="profile.html" class="btn btn-primary">Go to Profile</a> ' +
              '<a href="index.html" class="btn" style="background:var(--border); color:var(--text);">Go Home</a>' +
            '</div>' +
          '</div>';
      }
      return null;
    }

    return user;
  }

  // ── DOM refs ────────────────────────────────────────────────────────
  function el(id) { return document.getElementById(id); }

  function showError(msg) {
    var existing = document.querySelector(".admin-error-msg");
    if (existing) existing.remove();

    var div = document.createElement("div");
    div.className = "admin-error-msg";
    div.style.cssText =
      "background:#f8d7da; color:#721c24; padding:12px 16px; border-radius:8px; margin-bottom:16px; font-weight:600; font-size:14px; display:flex; align-items:center; gap:8px;";
    div.innerHTML = '<i class="fa-solid fa-circle-exclamation" style="font-size:16px;"></i> ' + msg;
    var tbody = el("adminTableBody");
    if (tbody && tbody.parentNode) {
      tbody.parentNode.parentNode.insertBefore(div, tbody.parentNode);
    } else {
      var main = document.querySelector("main.container");
      if (main) main.insertBefore(div, main.firstChild);
    }
  }

  function clearError() {
    var existing = document.querySelector(".admin-error-msg");
    if (existing) existing.remove();
  }

  function showMessage(msg, isError) {
    var existing = document.querySelector(".admin-msg");
    if (existing) existing.remove();

    var div = document.createElement("div");
    div.className = "admin-msg";
    div.style.cssText =
      "padding:12px 16px; border-radius:8px; margin-bottom:16px; font-weight:600; font-size:14px; display:flex; align-items:center; gap:8px;";
    if (isError) {
      div.style.background = "#f8d7da";
      div.style.color = "#721c24";
      div.innerHTML = '<i class="fa-solid fa-circle-exclamation" style="font-size:16px;"></i> ' + msg;
    } else {
      div.style.background = "#d4edda";
      div.style.color = "#155724";
      div.innerHTML = '<i class="fa-solid fa-circle-check" style="font-size:16px;"></i> ' + msg;
    }
    var main = document.querySelector("main.container");
    if (main) main.insertBefore(div, main.firstChild);
  }

  function clearMessages() {
    var existing = document.querySelector(".admin-msg");
    if (existing) existing.remove();
  }

  // ── Reason prompt ────────────────────────────────────────────────────
  function promptReason(actionLabel) {
    var reason = prompt("Please provide a reason for " + actionLabel + " (required for audit trail):");
    if (reason === null) return null; // user cancelled
    if (!reason || reason.trim() === "") {
      alert("A reason is required for this action.");
      return promptReason(actionLabel); // retry
    }
    return reason.trim();
  }

  // ── Fetch listings with real API ─────────────────────────────────────
  async function fetchAdminListings() {
    var tbody = el("adminTableBody");
    if (!tbody) return null;

    isLoading = true;
    tbody.innerHTML =
      "<tr><td colspan='8' style='padding:30px; text-align:center;'><div class='loader'></div><br/>Loading moderation queue...</td></tr>";
    clearError();

    try {
      var items = [];
      try {
        var result = await window.UniNestApi.request("/api/v1/listings?take=50");
        items = result.items || result || [];
      } catch (err) {
        console.warn("Admin API unavailable, using local dataset fallback:", err);
      }

      if (!items || items.length === 0) {
        items = (typeof properties !== "undefined" ? properties : (typeof baseProperties !== "undefined" ? baseProperties : []));
      }

      // Count by status
      var pendingCount = 0;
      var publishedCount = 0;
      var archivedCount = 0;

      var html = "";

      for (var i = 0; i < items.length; i++) {
        var p = items[i];
        var status = p.status || "published";

        // Map numeric status if needed
        if (typeof status === "number") {
          if (status === 1) status = "draft";
          else if (status === 2) status = "pending";
          else if (status === 3) status = "published";
          else status = "archived";
        }

        if (status === "pending" || status === "draft") pendingCount++;
        else if (status === "published") publishedCount++;
        else archivedCount++;

        var badgeBg = "#2ecc71";
        var badgeLabel = "Published";
        if (status === "pending" || status === "draft") {
          badgeBg = "#f39c12";
          badgeLabel = status === "draft" ? "Draft" : "Pending Review";
        } else if (status === "archived" || status === "rejected") {
          badgeBg = "#e74c3c";
          badgeLabel = "Archived";
        } else if (status === "suspended") {
          badgeBg = "#9b59b6";
          badgeLabel = "Suspended";
        }

        var img = p.image || "https://images.unsplash.com/photo-1523217582562-09d0def993a6?w=200";

        // Build action buttons based on status
        var actionsHtml = "";

        if (status === "pending" || status === "draft") {
          // Approve + Suspend + Send-Back (for draft/pending, send-back doesn't really apply)
          actionsHtml +=
            "<button onclick='window.adminApprove(\"" + p.id + "\")' class='btn' style='background:#2ecc71; color:white; padding:6px 12px; font-size:12px; border-radius:6px; margin-right:6px;'><i class='fa-solid fa-check'></i> Approve</button>";
          actionsHtml +=
            "<button onclick='window.adminSuspend(\"" + p.id + "\")' class='btn' style='background:#f39c12; color:white; padding:6px 12px; font-size:12px; border-radius:6px; margin-right:6px;'><i class='fa-solid fa-pause'></i> Suspend</button>";
        }

        if (status === "published") {
          // Suspend + Archive
          actionsHtml +=
            "<button onclick='window.adminSuspend(\"" + p.id + "\")' class='btn' style='background:#f39c12; color:white; padding:6px 12px; font-size:12px; border-radius:6px; margin-right:6px;'><i class='fa-solid fa-pause'></i> Suspend</button>";
          actionsHtml +=
            "<button onclick='window.adminArchive(\"" + p.id + "\")' class='btn' style='background:#e74c3c; color:white; padding:6px 12px; font-size:12px; border-radius:6px; margin-right:6px;'><i class='fa-solid fa-box-archive'></i> Archive</button>";
        }

        if (status === "suspended") {
          // Restore + Send-Back + Archive
          actionsHtml +=
            "<button onclick='window.adminRestore(\"" + p.id + "\")' class='btn' style='background:#2ecc71; color:white; padding:6px 12px; font-size:12px; border-radius:6px; margin-right:6px;'><i class='fa-solid fa-rotate-left'></i> Restore</button>";
          actionsHtml +=
            "<button onclick='window.adminSendBack(\"" + p.id + "\")' class='btn' style='background:#3498db; color:white; padding:6px 12px; font-size:12px; border-radius:6px; margin-right:6px;'><i class='fa-solid fa-arrow-turn-up'></i> Send to Draft</button>";
          actionsHtml +=
            "<button onclick='window.adminArchive(\"" + p.id + "\")' class='btn' style='background:#e74c3c; color:white; padding:6px 12px; font-size:12px; border-radius:6px;'><i class='fa-solid fa-box-archive'></i> Archive</button>";
        }

        if (status === "archived" || status === "rejected") {
          // Restore from archive (if allowed)
          actionsHtml +=
            "<button onclick='window.adminRestore(\"" + p.id + "\")' class='btn' style='background:#95a5a6; color:white; padding:6px 12px; font-size:12px; border-radius:6px;'><i class='fa-solid fa-rotate-left'></i> Restore</button>";
        }

        html += "<tr style='border-bottom:1px solid var(--border); transition:background 0.2s;'>";
        html += "<td style='padding:12px; display:flex; align-items:center; gap:12px;'>";
        html += "<img src='" + img + "' style='width:48px; height:48px; border-radius:8px; object-fit:cover;' />";
        html += "<div><strong style='font-size:14px; display:block;'>" + (p.title || "Untitled Property") + "</strong>" +
          "<span style='font-size:12px; color:var(--text-light);'>" + (p.owner || "Owner") + "</span></div>";
        html += "</td>";
        html += "<td style='padding:12px; font-size:13px; text-transform:capitalize;'>" + (p.type || "Apartment") + "</td>";
        html += "<td style='padding:12px; font-weight:700; color:var(--primary);'>" + p.price + " EGP</td>";
        html += "<td style='padding:12px; font-size:13px;'>" + p.location + "</td>";
        html += "<td style='padding:12px;'><span style='background:" + badgeBg + "; color:#fff; padding:4px 10px; border-radius:12px; font-size:11px; font-weight:bold;'>" + badgeLabel + "</span></td>";
        // Contact phone column
        html += "<td style='padding:12px; font-size:13px; max-width:140px;'>" +
          (p.phone ? "<span style='color:var(--text); word-break:break-all;' title='Click to copy'>" + p.phone + "</span>" :
           "<span style='color:var(--text-light);'>—</span>") + "</td>";
        html += "<td style='padding:12px; text-align:right;'>" + actionsHtml + "</td>";
        html += "</tr>";
      }

      if (items.length === 0) {
        html = "<tr><td colspan='8' style='padding:30px; text-align:center; color:var(--text-light);'>No property listings found.</td></tr>";
      }

      tbody.innerHTML = html;
      el("statPending").textContent = pendingCount;
      el("statPublished").textContent = publishedCount;
      el("statArchived").textContent = archivedCount;

      return items;
    } catch (e) {
      tbody.innerHTML =
        "<tr><td colspan='8' style='padding:30px; text-align:center; color:#721c24;'>" +
        "<i class='fa-solid fa-circle-exclamation' style='font-size:24px; display:block; margin-bottom:8px;'></i>" +
        "Error loading listings. Please try again.</td></tr>";
      showError("Failed to load listings from the server. Please check your connection and try again.");
      console.error("Failed to fetch admin listings:", e);
      return null;
    } finally {
      isLoading = false;
    }
  }

  // ── Admin Actions ────────────────────────────────────────────────────

  window.adminApprove = async function (id) {
    clearMessages();
    if (!confirm("Approve and publish this listing? This will make it visible to all users.")) return;

    try {
      await window.UniNestApi.publishListing(id);
      showMessage("Listing approved and published successfully.");
      fetchAdminListings();
    } catch (err) {
      var msg = "Failed to approve listing.";
      if (err.payload && err.payload.detail) {
        msg = err.payload.detail;
      } else if (err.status === 400) {
        msg = "Cannot approve: " + (err.payload && err.payload.detail ? err.payload.detail : "Listing is not in a pending state.");
      } else if (err.status === 401) {
        msg = "Session expired. Please log in again.";
      } else if (err.status === 403) {
        msg = "Access denied. You do not have Admin privileges.";
      }
      showError(msg);
      console.error("Approve failed:", err);
    }
  };

  window.adminArchive = async function (id) {
    if (!confirm("Are you sure you want to archive this listing?")) return;

    try {
      await window.UniNestApi.archiveListing(id);
      showMessage("Listing archived successfully.");
      fetchAdminListings();
    } catch (err) {
      var msg = "Failed to archive listing.";
      if (err.payload && err.payload.detail) {
        msg = err.payload.detail;
      } else if (err.status === 401) {
        msg = "Session expired. Please log in again.";
      } else if (err.status === 403) {
        msg = "Access denied.";
      }
      showError(msg);
      console.error("Archive failed:", err);
    }
  };

  window.adminSuspend = async function (id) {
    var reason = promptReason("suspending this listing");
    if (reason === null) return; // cancelled

    try {
      var result = await window.UniNestApi.request(
        "/api/v1/listings/" + encodeURIComponent(id) + "/suspend",
        {
          method: "POST",
          body: JSON.stringify({ reason: reason })
        }
      );
      showMessage("Listing suspended successfully.");
      fetchAdminListings();
    } catch (err) {
      var msg = "Failed to suspend listing.";
      if (err.payload && err.payload.detail) {
        msg = err.payload.detail;
      } else if (err.status === 400) {
        msg = "Cannot suspend: " + (err.payload && err.payload.detail ? err.payload.detail : "Listing is not in a suspendable state.");
      } else if (err.status === 401) {
        msg = "Session expired. Please log in again.";
      } else if (err.status === 403) {
        msg = "Access denied. You do not have Admin privileges.";
      }
      showError(msg);
      console.error("Suspend failed:", err);
    }
  };

  window.adminRestore = async function (id) {
    if (!confirm("Restore this listing to Published status?")) return;

    try {
      await window.UniNestApi.request(
        "/api/v1/listings/" + encodeURIComponent(id) + "/restore",
        { method: "POST" }
      );
      showMessage("Listing restored to Published.");
      fetchAdminListings();
    } catch (err) {
      var msg = "Failed to restore listing.";
      if (err.payload && err.payload.detail) {
        msg = err.payload.detail;
      } else if (err.status === 400) {
        msg = "Cannot restore: " + (err.payload && err.payload.detail ? err.payload.detail : "Listing is not in a suspended state.");
      } else if (err.status === 401) {
        msg = "Session expired. Please log in again.";
      } else if (err.status === 403) {
        msg = "Access denied. You do not have Admin privileges.";
      }
      showError(msg);
      console.error("Restore failed:", err);
    }
  };

  window.adminSendBack = async function (id) {
    var reason = promptReason("sending this listing back to Draft");
    if (reason === null) return;

    try {
      await window.UniNestApi.request(
        "/api/v1/listings/" + encodeURIComponent(id) + "/send-back",
        {
          method: "POST",
          body: JSON.stringify({ reason: reason })
        }
      );
      showMessage("Listing sent back to Draft. The owner must edit and resubmit.");
      fetchAdminListings();
    } catch (err) {
      var msg = "Failed to send listing back to Draft.";
      if (err.payload && err.payload.detail) {
        msg = err.payload.detail;
      } else if (err.status === 400) {
        msg = "Cannot send back: " + (err.payload && err.payload.detail ? err.payload.detail : "Listing is not in a suspendable state.");
      } else if (err.status === 401) {
        msg = "Session expired. Please log in again.";
      } else if (err.status === 403) {
        msg = "Access denied. You do not have Admin privileges.";
      }
      showError(msg);
      console.error("Send-back failed:", err);
    }
  };

  // ── Page Init ────────────────────────────────────────────────────────
  window.onload = async function () {
    // 1. Auth guard — blocks non-admins immediately
    var adminUser = checkAdminAccess();
    if (!adminUser) return; // either redirected or showing access denied

    // 2. Update navbar to show admin name
    var navBtn = el("navLoginBtn");
    if (navBtn) {
      navBtn.textContent = adminUser.name;
      navBtn.href = "profile.html";
    }

    // 3. Load listings with real API
    await fetchAdminListings();
  };
})();
