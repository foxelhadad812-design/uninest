(function () {
  function getChatUser() {
    return JSON.parse(localStorage.getItem("currentUser") || "null");
  }

  function getChats() {
    return JSON.parse(localStorage.getItem("uninest.chats") || "[]");
  }

  function saveChats(chats) {
    localStorage.setItem("uninest.chats", JSON.stringify(chats));
  }

  function initChatWidget() {
    var user = getChatUser();
    if (!user) return; // Only show chat widget for logged in users

    if (document.getElementById("uniChatWidget")) return;

    var lang = localStorage.getItem("lang") || "en";
    var titleText = lang === "ar" ? "💬 المحادثات المباشرة" : "💬 UniNest Live Support & Chat";
    var placeholderText = lang === "ar" ? "اكتب رسالتك هنا..." : "Type your message...";
    var sendBtnText = lang === "ar" ? "إرسال" : "Send";

    var widgetHTML =
      "<div id='uniChatWidget' style='position:fixed; bottom:20px; right:20px; z-index:99999; font-family:inherit;'>" +
        "<button id='chatToggleBtn' onclick='toggleChatDrawer()' style='background:var(--primary, #2196f3); color:white; border:none; border-radius:50px; padding:12px 20px; font-weight:700; font-size:14px; cursor:pointer; box-shadow:0 4px 14px rgba(33,150,243,0.4); display:flex; align-items:center; gap:8px; transition:transform 0.2s;'>" +
          "<i class='fa-solid fa-comments' style='font-size:18px;'></i> <span>Live Chat</span> <span id='chatBadge' style='background:#e74c3c; color:white; border-radius:10px; padding:2px 6px; font-size:10px; display:none;'>1</span>" +
        "</button>" +
        "<div id='chatDrawer' style='display:none; width:340px; height:450px; background:var(--white, #fff); border-radius:12px; box-shadow:0 10px 30px rgba(0,0,0,0.2); border:1px solid var(--border, #e0e0e0); position:absolute; bottom:60px; right:0; flex-direction:column; overflow:hidden;'>" +
          "<div style='background:var(--primary, #2196f3); color:white; padding:14px 16px; font-weight:700; font-size:14px; display:flex; justify-content:space-between; align-items:center;'>" +
            "<div>" + titleText + "</div>" +
            "<button onclick='toggleChatDrawer()' style='background:none; border:none; color:white; font-size:18px; cursor:pointer;'>✕</button>" +
          "</div>" +
          "<div id='chatBody' style='flex:1; padding:12px; overflow-y:auto; display:flex; flex-direction:column; gap:8px; background:var(--bg, #f8f9fa);'></div>" +
          "<div style='padding:10px; background:var(--white, #fff); border-top:1px solid var(--border, #e0e0e0); display:flex; gap:8px;'>" +
            "<input type='text' id='chatInput' placeholder='" + placeholderText + "' style='flex:1; padding:8px 12px; border:1px solid var(--border, #ccc); border-radius:6px; font-size:13px; outline:none;' onkeypress='if(event.key===\"Enter\") sendChatMessage()'/>" +
            "<button onclick='sendChatMessage()' style='background:var(--primary, #2196f3); color:white; border:none; padding:8px 14px; border-radius:6px; font-weight:bold; cursor:pointer; font-size:12px;'>" + sendBtnText + "</button>" +
          "</div>" +
        "</div>" +
      "</div>";

    var div = document.createElement("div");
    div.innerHTML = widgetHTML;
    document.body.appendChild(div.firstElementChild);

    renderChatMessages();
  }

  window.toggleChatDrawer = function () {
    var drawer = document.getElementById("chatDrawer");
    if (!drawer) return;
    var isOpen = drawer.style.display === "flex";
    drawer.style.display = isOpen ? "none" : "flex";
    if (!isOpen) {
      document.getElementById("chatBadge").style.display = "none";
      renderChatMessages();
    }
  };

  window.renderChatMessages = function () {
    var chatBody = document.getElementById("chatBody");
    if (!chatBody) return;

    var user = getChatUser();
    if (!user) return;

    var chats = getChats();
    if (chats.length === 0) {
      // Welcome message
      chats.push({
        sender: "UniNest Assistant",
        text: "Hello " + user.name + "! 👋 How can we help you find your student housing today?",
        time: new Date().toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' }),
        isBot: true
      });
      saveChats(chats);
    }

    var html = "";
    for (var i = 0; i < chats.length; i++) {
      var msg = chats[i];
      var isMe = msg.sender === user.name;
      var align = isMe ? "align-self:flex-end; background:#2196f3; color:white;" : "align-self:flex-start; background:#e9ecef; color:#333;";

      html +=
        "<div style='max-width:80%; padding:8px 12px; border-radius:12px; font-size:13px; " + align + "'>" +
          "<div style='font-size:10px; opacity:0.8; margin-bottom:2px; font-weight:bold;'>" + msg.sender + "</div>" +
          "<div>" + msg.text + "</div>" +
          "<div style='font-size:9px; opacity:0.7; text-align:right; margin-top:2px;'>" + (msg.time || "") + "</div>" +
        "</div>";
    }
    chatBody.innerHTML = html;
    chatBody.scrollTop = chatBody.scrollHeight;
  };

  window.sendChatMessage = function () {
    var input = document.getElementById("chatInput");
    if (!input) return;
    var text = input.value.trim();
    if (!text) return;

    var user = getChatUser();
    var chats = getChats();
    var timeStr = new Date().toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' });

    chats.push({
      sender: user ? user.name : "Guest",
      text: text,
      time: timeStr,
      isMe: true
    });

    input.value = "";
    saveChats(chats);
    renderChatMessages();

    // Auto bot response simulation after 1 sec
    setTimeout(function () {
      var botReply = "Thank you for reaching out! A UniNest housing specialist will get back to you shortly.";
      var lang = localStorage.getItem("lang") || "en";
      if (lang === "ar") botReply = "شكراً لإنشائك المحادثة! سيقوم ممثل UniNest بالرد عليك في أقرب وقت.";

      chats.push({
        sender: "UniNest Support",
        text: botReply,
        time: new Date().toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' }),
        isBot: true
      });
      saveChats(chats);
      renderChatMessages();
    }, 1000);
  };

  if (document.readyState === "loading") {
    document.addEventListener("DOMContentLoaded", initChatWidget);
  } else {
    initChatWidget();
  }
})();
