// UniNest Global Toast Notification Component
(function(global) {
  function injectToastStyles() {
    if (document.getElementById("uninest-toast-styles")) return;
    var style = document.createElement("style");
    style.id = "uninest-toast-styles";
    style.innerHTML = `
      #uninest-toast-container {
        position: fixed;
        bottom: 24px;
        right: 24px;
        z-index: 99999;
        display: flex;
        flex-direction: column;
        gap: 10px;
        max-width: 360px;
        pointer-events: none;
      }
      .uninest-toast {
        background: #1e293b;
        color: #ffffff;
        padding: 14px 20px;
        border-radius: 12px;
        box-shadow: 0 10px 25px -5px rgba(0,0,0,0.3);
        font-size: 14px;
        font-weight: 500;
        display: flex;
        align-items: center;
        gap: 12px;
        pointer-events: auto;
        opacity: 0;
        transform: translateY(20px) scale(0.95);
        transition: all 0.3s cubic-bezier(0.16, 1, 0.3, 1);
        border-left: 4px solid #2196f3;
      }
      .uninest-toast.show {
        opacity: 1;
        transform: translateY(0) scale(1);
      }
      .uninest-toast.success { border-left-color: #10b981; }
      .uninest-toast.error { border-left-color: #ef4444; }
      .uninest-toast.warning { border-left-color: #f59e0b; }
      .uninest-toast.info { border-left-color: #2196f3; }
    `;
    document.head.appendChild(style);
  }

  function showToast(message, type, duration) {
    injectToastStyles();
    type = type || "info";
    duration = duration || 3500;

    var container = document.getElementById("uninest-toast-container");
    if (!container) {
      container = document.createElement("div");
      container.id = "uninest-toast-container";
      document.body.appendChild(container);
    }

    var iconMap = {
      success: "fa-check-circle",
      error: "fa-exclamation-circle",
      warning: "fa-exclamation-triangle",
      info: "fa-info-circle"
    };
    var iconClass = iconMap[type] || "fa-info-circle";

    var toast = document.createElement("div");
    toast.className = "uninest-toast " + type;
    toast.innerHTML = "<i class='fa-solid " + iconClass + "'></i> <span>" + message + "</span>";

    container.appendChild(toast);

    requestAnimationFrame(function() {
      toast.classList.add("show");
    });

    setTimeout(function() {
      toast.classList.remove("show");
      setTimeout(function() {
        if (toast.parentNode) {
          toast.parentNode.removeChild(toast);
        }
      }, 300);
    }, duration);
  }

  global.showToast = showToast;
})(window);
