// دي بمثابة قاعدة البيانات (Database) للمشروع 
// عشان مفيش سيرفر حقيقي، بنعمل محاكاة (Mock) في شكل مصفوفة (Array) جواها العقارات كـ Objects
let baseProperties = [
  {
    id: 1,
    title: "Apartment near Fayoum University", title_ar: "شقة بالقرب من جامعة الفيوم",
    price: 3500,
    location: "Fayoum", location_ar: "الفيوم",
    type: "apartment",
    rooms: 2,
    image: "https://images.unsplash.com/photo-1555854877-bab0e564b8d5?w=400",
    description: "Spacious apartment near Fayoum University, fully furnished, quiet neighborhood.", desc_ar: "شقة واسعة بالقرب من جامعة الفيوم، مفروشة بالكامل، في حي هادئ.",
    amenities: ["Wi-Fi", "Air Conditioning", "Kitchen"],
    owner: "Moamen hamouda", phone: "01007272508"
  },
  {
    id: 2,
    title: "Single Room near EELU Fayoum", title_ar: "غرفة مفردة بالقرب من الجامعة الأهلية بالفيوم",
    price: 3800,
    gender: "male",
    location: "Fayoum", location_ar: "الفيوم",
    type: "single",
    rooms: 1,
    image: "https://images.unsplash.com/photo-1502672260266-1c1ef2d93688?w=400",
    description: "Cozy single room, 10 minutes from EELU Fayoum branch, all utilities included.", desc_ar: "غرفة مفردة مريحة، على بعد 10 دقائق من فرع الجامعة الأهلية بالفيوم، شاملة كل المرافق.",
    amenities: ["Wi-Fi", "Kitchen"],
    owner: "Ziad Ahmed", phone: "01012557656"
  },
  {
    id: 3,
    title: "Shared Room near Fayoum City Center", title_ar: "غرفة مشتركة بالقرب من وسط الفيوم",
    price: 3100,
    gender: "male",
    location: "Fayoum", location_ar: "الفيوم",
    type: "shared",
    rooms: 1,
    image: "https://images.unsplash.com/photo-1560448204-e02f11c3d0e2?w=400",
    description: "Affordable shared room for students, close to transportation and markets.", desc_ar: "غرفة مشتركة بأسعار اقتصادية للطلاب، قريبة من المواصلات والأسواق.",
    amenities: ["Wi-Fi", "Washing Machine"],
    owner: "Mohamed Hassan", phone: "01009229692"
  },
  {
    id: 4,
    title: "Studio Apartment near Cairo University", title_ar: "استوديو سكن بالقرب من جامعة القاهرة",
    price: 6500,
    location: "Cairo", location_ar: "القاهرة",
    type: "apartment",
    rooms: 1,
    image: "https://images.unsplash.com/photo-1493809842364-78817add7ffb?w=400",
    description: "Modern studio apartment, fully furnished, 5 minutes from Cairo University.", desc_ar: "شقة استوديو حديثة مفروشة بالكامل، على بعد 5 دقائق من جامعة القاهرة.",
    amenities: ["Wi-Fi", "Air Conditioning", "Kitchen", "Parking"],
    owner: "Ammar Khaled", phone: "01066805363"
  },
  {
    id: 5,
    title: "Apartment near Ain Shams University", title_ar: "شقة بالقرب من جامعة عين شمس",
    price: 7000,
    location: "Cairo", location_ar: "القاهرة",
    type: "apartment",
    rooms: 3,
    image: "https://images.unsplash.com/photo-1484154218962-a197022b5858?w=400",
    description: "Large 3-bedroom apartment, perfect for students looking for roommates.", desc_ar: "شقة واسعة بـ 3 غرف نوم، مثالية لطلاب يبحثون عن سكن مشترك.",
    amenities: ["Wi-Fi", "Kitchen", "Air Conditioning"],
    owner: "Abdullah Ashraf", phone: "01016714122"
  },
  {
    id: 6,
    title: "Single Room near Alexandria University", title_ar: "غرفة مفردة بالقرب من جامعة الإسكندرية",
    price: 3600,
    location: "Alexandria", location_ar: "الإسكندرية",
    type: "single",
    rooms: 1,
    image: "https://images.unsplash.com/photo-1522771739844-6a9f6d5f14af?w=400",
    description: "Clean and quiet single room near Alexandria University campus.", desc_ar: "غرفة مفردة نظيفة وهادئة بالقرب من حرم جامعة الإسكندرية.",
    amenities: ["Wi-Fi", "Kitchen"],
    owner: "Mohamed Ragab", phone: "01032894477"
  },
  {
    id: 7,
    title: "Shared Room near Mansoura University", title_ar: "غرفة مشتركة بالقرب من جامعة المنصورة",
    price: 3400,
    location: "Mansoura", location_ar: "المنصورة",
    type: "shared",
    rooms: 1,
    image: "https://images.unsplash.com/photo-1484101403633-562f891dc89a?w=400",
    description: "Budget-friendly shared room, close to Mansoura University and public transport.", desc_ar: "غرفة مشتركة بأسعار اقتصادية، قريبة من جامعة المنصورة ووسائل النقل العام.",
    amenities: ["Wi-Fi", "Washing Machine", "Kitchen"],
    owner: "Nader Sayed", phone: "01061825930"
  },
  {
    id: 8,
    title: "Apartment near Zagazig University", title_ar: "شقة بالقرب من جامعة الزقازيق",
    price: 3800,
    location: "Zagazig", location_ar: "الزقازيق",
    type: "apartment",
    rooms: 2,
    image: "https://images.unsplash.com/photo-1536376072261-38c75010e6c9?w=400",
    description: "Comfortable 2-bedroom apartment near Zagazig University, fully furnished.", desc_ar: "شقة مريحة بـ 2 غرفة نوم بالقرب من جامعة الزقازيق، مفروشة بالكامل.",
    amenities: ["Wi-Fi", "Air Conditioning", "Kitchen"],
    owner: "Hossam Hassan", phone: "01124242930"
  },
  {
    id: 9,
    title: "Premium Studio in Fayoum City", title_ar: "استوديو فاخر في مدينة الفيوم",
    price: 5000,
    location: "Fayoum", location_ar: "الفيوم",
    type: "apartment",
    rooms: 1,
    image: "https://images.unsplash.com/photo-1499955085172-a104c9463ece?w=400",
    description: "A newly renovated premium studio with fast internet, ideal for dedicated students.", desc_ar: "استوديو مجدد بالكامل بإنترنت سريع، مثالي للطلاب.",
    amenities: ["Wi-Fi", "Air Conditioning", "Kitchen", "Smart TV"],
    owner: "Kareem Tarek", phone: "01234567890"
  },
  {
    id: 10,
    title: "Quiet Room near Fayoum Stadium", title_ar: "غرفة هادئة بالقرب من استاد الفيوم",
    price: 3600,
    location: "Fayoum", location_ar: "الفيوم",
    type: "single",
    rooms: 1,
    image: "https://images.unsplash.com/photo-1513694203232-719a280e022f?w=400",
    description: "Quiet and well-lit single room in a peaceful area near the stadium.", desc_ar: "غرفة مفردة هادئة ومضيئة في منطقة قريبة من الاستاد.",
    amenities: ["Wi-Fi", "Balcony"],
    owner: "Mahmoud Ezzat", phone: "01001122334"
  },
  {
    id: 11,
    title: "Shared Apartment by EELU Gates", title_ar: "شقة مشتركة بجوار بوابات الأهلية",
    price: 3000,
    location: "Fayoum", location_ar: "الفيوم",
    type: "shared",
    rooms: 2,
    image: "https://images.unsplash.com/photo-1505691938895-1758d7feb511?w=400",
    description: "Share this lovely apartment with fellow students just steps away from EELU.", desc_ar: "شارك هذه الشقة اللطيفة مع زملائك على بعد خطوات من الجامعة الأهلية.",
    amenities: ["Washing Machine", "Kitchen"],
    owner: "Youssef Ibrahim", phone: "01112233445"
  },
  {
    id: 12,
    title: "Furnished Flat near Fayoum Station", title_ar: "شقة مفروشة بالقرب من محطة الفيوم",
    price: 3200,
    location: "Fayoum", location_ar: "الفيوم",
    type: "apartment",
    rooms: 3,
    image: "https://images.unsplash.com/photo-1600585154340-be6161a56a0c?w=400",
    description: "Large furnished flat close to the train station for easy commuting.", desc_ar: "شقة مفروشة واسعة قريبة من محطة القطار لسهولة التنقل.",
    amenities: ["Air Conditioning", "Wi-Fi", "Elevator"],
    owner: "Sayed Fathy", phone: "01099887766"
  },
  {
    id: 13,
    title: "Shared Room for Girls only - Fayoum", title_ar: "غرفة مشتركة للبنات فقط - الفيوم",
    price: 3600,
    gender: "female",
    location: "Fayoum", location_ar: "الفيوم",
    type: "shared",
    rooms: 1,
    image: "https://images.unsplash.com/photo-1554995207-c18c203602cb?w=400",
    description: "Safe and secure room for female students, all bills included.", desc_ar: "غرفة آمنة ومريحة للطالبات، السعر شامل الفواتير.",
    amenities: ["Wi-Fi", "Kitchen", "Security"],
    owner: "Noha Adel", phone: "01200112233"
  },
  {
    id: 14,
    title: "Luxury Apartment in Maadi", title_ar: "شقة فاخرة في المعادي",
    price: 12000,
    location: "Cairo", location_ar: "القاهرة",
    type: "apartment",
    rooms: 2,
    image: "https://images.unsplash.com/photo-1600566753190-17f0baa2a6c3?w=400",
    description: "Luxury apartment with Nile view, suitable for international students.", desc_ar: "شقة فاخرة تطل على النيل، مناسبة للطلاب الدوليين.",
    amenities: ["Wi-Fi", "Pool", "Gym", "Air Conditioning"],
    owner: "Omar Tarek", phone: "01144556677"
  },
  {
    id: 15,
    title: "Shared Male Dorm - Giza", title_ar: "سكن طلاب مشترك - الجيزة",
    price: 3700,
    gender: "male",
    location: "Giza", location_ar: "الجيزة",
    type: "shared",
    rooms: 1,
    image: "https://images.unsplash.com/photo-1540518614846-7eded433c457?w=400",
    description: "Bed in a shared room, very close to Giza square and metro station.", desc_ar: "سرير في غرفة مشتركة، قريبة جداً من ميدان الجيزة ومحطة المترو.",
    amenities: ["Wi-Fi", "Washing Machine"],
    owner: "Adel Imam", phone: "01033445566"
  },
  {
    id: 16,
    title: "Cozy Studio near Smouha", title_ar: "استوديو مريح في سموحة",
    price: 5500,
    location: "Alexandria", location_ar: "الإسكندرية",
    type: "apartment",
    rooms: 1,
    image: "https://images.unsplash.com/photo-1522708323590-d24dbb6b0267?w=400",
    description: "Beautifully designed studio in Smouha, Alexandria. Perfect for living alone.", desc_ar: "استوديو بتصميم جميل في سموحة، الإسكندرية. مثالي للعيش المستقل.",
    amenities: ["Wi-Fi", "Air Conditioning", "Balcony"],
    owner: "Tamer Hosny", phone: "01255667788"
  },
  {
    id: 17,
    title: "Single Room in Miami - Alex", title_ar: "غرفة مفردة في ميامي - الإسكندرية",
    price: 4500,
    location: "Alexandria", location_ar: "الإسكندرية",
    type: "single",
    rooms: 1,
    image: "https://images.unsplash.com/photo-1519710164239-da123dc03ef4?w=400",
    description: "Sea view single room, close to Arab Academy.", desc_ar: "غرفة مفردة بإطلالة على البحر، قريبة من الأكاديمية العربية.",
    amenities: ["Wi-Fi", "Kitchen"],
    owner: "Sara Ahmed", phone: "01166778899"
  },
  {
    id: 18,
    title: "Large Apartment in New Cairo", title_ar: "شقة واسعة في القاهرة الجديدة",
    price: 16000,
    location: "Cairo", location_ar: "القاهرة",
    type: "apartment",
    rooms: 4,
    image: "https://images.unsplash.com/photo-1600607687939-ce8a6c25118c?w=400",
    description: "Spacious 4-bedroom apartment next to AUC campus. Perfect to share.", desc_ar: "شقة واسعة بـ 4 غرف نوم بجوار حرم الجامعة الأمريكية. مثالية للمشاركة.",
    amenities: ["Wi-Fi", "Air Conditioning", "Kitchen", "Gym", "Pool"],
    owner: "Khaled Youssef", phone: "01077889900"
  },
  {
    id: 19,
    title: "Shared Female Room - Mansoura", title_ar: "غرفة مشتركة للطالبات - المنصورة",
    price: 3300,
    gender: "female",
    location: "Mansoura", location_ar: "المنصورة",
    type: "shared",
    rooms: 1,
    image: "https://images.unsplash.com/photo-1524758631624-e2822e304c36?w=400",
    description: "Affordable bed in a double room for female students in Mansoura.", desc_ar: "سرير اقتصادي في غرفة مزدوجة للطالبات في المنصورة.",
    amenities: ["Wi-Fi", "Washing Machine"],
    owner: "Mai Ezzat", phone: "01288990011"
  },
  {
    id: 20,
    title: "2-Bedroom Apartment in Tanta", title_ar: "شقة غرفتين في طنطا",
    price: 3500,
    location: "Tanta", location_ar: "طنطا",
    type: "apartment",
    rooms: 2,
    image: "https://images.unsplash.com/photo-1564013799919-ab600027ffc6?w=400",
    description: "Close to Tanta University, newly painted and furnished.", desc_ar: "قريبة من جامعة طنطا، مجددة ومفروشة حديثاً.",
    amenities: ["Wi-Fi", "Kitchen"],
    owner: "Ali Fawzy", phone: "01099001122"
  },
  {
    id: 21,
    title: "Single Room in Assiut", title_ar: "غرفة مفردة في أسيوط",
    price: 3400,
    location: "Assiut", location_ar: "أسيوط",
    type: "single",
    rooms: 1,
    image: "https://images.unsplash.com/photo-1542889601-399c4f3a8402?w=400",
    description: "Quiet room located in central Assiut, ideal for studying.", desc_ar: "غرفة هادئة في وسط أسيوط، مثالية للمذاكرة.",
    amenities: ["Wi-Fi", "Balcony"],
    owner: "Kamal Hassan", phone: "01100112233"
  },
  {
    id: 22,
    title: "Premium Apartment in Minya", title_ar: "شقة مميزة في المنيا",
    price: 4500,
    location: "Minya", location_ar: "المنيا",
    type: "apartment",
    rooms: 3,
    image: "https://images.unsplash.com/photo-1512917774080-9991f1c4c750?w=400",
    description: "Gorgeous apartment overlooking the Nile in Minya city.", desc_ar: "شقة رائعة تطل على النيل في مدينة المنيا.",
    amenities: ["Wi-Fi", "Air Conditioning", "Kitchen"],
    owner: "Hanan Fathy", phone: "01211223344"
  },
  {
    id: 23,
    title: "Shared Bed near Suez Canal Uni", title_ar: "سرير مشترك بالقرب من جامعة قناة السويس",
    price: 3200,
    location: "Ismailia", location_ar: "الإسماعيلية",
    type: "shared",
    rooms: 1,
    image: "https://images.unsplash.com/photo-1595526114035-0d45ed16cfbf?w=400",
    description: "Great location in Ismailia, very close to the university campus.", desc_ar: "موقع استثنائي في الإسماعيلية، قريب جداً من الحرم الجامعي.",
    amenities: ["Washing Machine", "Kitchen"],
    owner: "Mostafa Kamel", phone: "01022334455"
  },
  {
    id: 24,
    title: "Modern Studio in Banha", title_ar: "استوديو حديث في بنها",
    price: 4800,
    location: "Banha", location_ar: "بنها",
    type: "apartment",
    rooms: 1,
    image: "https://images.unsplash.com/photo-1493809842364-78817add7ffb?w=400",
    description: "Well equipped modern studio near Banha University gates.", desc_ar: "استوديو حديث مجهز بالكامل بالقرب من بوابات جامعة بنها.",
    amenities: ["Wi-Fi", "Air Conditioning"],
    owner: "Dina Sayed", phone: "01133445566"
  },
  {
    id: 25,
    title: "Single Room in Menoufia", title_ar: "غرفة مفردة في المنوفية",
    price: 3400,
    location: "Shibin El Kom", location_ar: "شبين الكوم",
    type: "single",
    rooms: 1,
    image: "https://images.unsplash.com/photo-1522771739844-6a9f6d5f14af?w=400",
    description: "Close to Menoufia University complex, very peaceful street.", desc_ar: "بالقرب من مجمع كليات جامعة المنوفية، شارع هادئ جداً.",
    amenities: ["Wi-Fi", "Kitchen"],
    owner: "Ramy Sabry", phone: "01244556677"
  },
  {
    id: 26,
    title: "Shared Room in Sohag", title_ar: "غرفة مشتركة في سوهاج",
    price: 3100,
    location: "Sohag", location_ar: "سوهاج",
    type: "shared",
    rooms: 1,
    image: "https://images.unsplash.com/photo-1536376072261-38c75010e6c9?w=400",
    description: "A comfortable shared room in the new Sohag city, near the University.", desc_ar: "غرفة مشتركة مريحة في مدينة سوهاج الجديدة بجوار الجامعة.",
    amenities: ["Washing Machine"],
    owner: "Hassan Shaker", phone: "01055667788"
  },
  {
    id: 27,
    title: "Cozy Shared Room in Fayoum", title_ar: "غرفة مشتركة مريحة للبنات",
    price: 3900,
    gender: "female",
    location: "Fayoum", location_ar: "الفيوم",
    type: "shared",
    rooms: 1,
    image: "https://images.unsplash.com/photo-1502672260266-1c1ef2d93688?w=400",
    description: "Recently painted cozy single room with private bathroom access.", desc_ar: "غرفة مفردة مريحة مطلية حديثاً مع حمام خاص.",
    amenities: ["Wi-Fi", "Air Conditioning", "Private Bathroom"],
    owner: "Salma Yasser", phone: "01166778899"
  },
  {
    id: 28,
    title: "Spacious Flat near Fayoum UNI", title_ar: "شقة فسيحة بالقرب من جامعة الفيوم",
    price: 4500,
    location: "Fayoum", location_ar: "الفيوم",
    type: "apartment",
    rooms: 3,
    image: "https://images.unsplash.com/photo-1555854877-bab0e564b8d5?w=400",
    description: "Perfect for a group of students, 3 bedrooms in a lively neighborhood.", desc_ar: "مثالية لمجموعة من الطلاب، 3 غرف في حي حيوي.",
    amenities: ["Wi-Fi", "Kitchen", "Air Conditioning", "Balcony"],
    owner: "Amr Diab", phone: "01277889900"
  }
];

let properties = baseProperties;

// add custom properties to the main list
var savedListings = JSON.parse(localStorage.getItem("myListings") || "[]");
properties = properties.concat(savedListings);

// update nav bar if user is logged in
document.addEventListener("DOMContentLoaded", function() {
  var user = JSON.parse(localStorage.getItem("currentUser") || "null");
  if (user) {
    var navLoginBtn = document.querySelector(".btn-login");
    if (navLoginBtn) {
      navLoginBtn.textContent = user.name;
      navLoginBtn.href = "profile.html";
    }
  }
});

// dark mode functions
function toggleDarkMode() {
  var isDark = document.body.classList.toggle("dark-mode");
  localStorage.setItem("theme", isDark ? "dark" : "light");
  updateDarkModeIcon(isDark);
}

function updateDarkModeIcon(isDark) {
  var btn = document.getElementById("darkModeBtn");
  if (btn) {
    btn.textContent = isDark ? "☀️" : "🌙";
  }
}

// apply dark mode if saved
if (localStorage.getItem("theme") === "dark") {
  document.documentElement.classList.add("dark-mode");
  document.body.classList.add("dark-mode");
}

document.addEventListener("DOMContentLoaded", function() {
  if (localStorage.getItem("theme") === "dark") {
    document.body.classList.add("dark-mode");
  }
  updateDarkModeIcon(document.body.classList.contains("dark-mode"));
});