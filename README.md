# 🏠 UniNest - Student Housing Platform

**UniNest** is a frontend web application designed by students from EELU (Egyptian E-Learning University) to help university students find affordable and suitable housing easily.

![UniNest Preview](https://images.unsplash.com/photo-1523217582562-09d0def993a6?w=800)

## 📌 Project Overview
This project was developed as a university assignment to demonstrate fundamental web development skills using core frontend technologies. It simulates a fully functional property finding system tailored for students, featuring role-based authentication, property listings, dynamic filtering, and a translation system.

### 🚀 Key Features
- **Role-Based System:** Users can register as "Students" (to find housing) or "Owners" (to list housing).
- **No Backend Required:** The entire application leverages `localStorage` as a mock database, making it 100% frontend-based while ensuring data persistence across sessions.
- **Dynamic Search & Filters:** Filter properties by price, location, and type (Single/Shared/Apartment).
- **Multi-language Support (i18n):** Real-time switching between Arabic (عربي) and English using a custom dictionary-based translation engine.
- **Dark Mode:** A seamless toggle for dark mode that adjusts UI colors via CSS variables.
- **Gender-Based Restriction System:** Prevents students from interacting with or contacting properties designated for the opposite gender.
- **Security Validation:** Comprehensive registration form validation including strict password rules (Uppercase, Lowercase, Numbers, Symbols).
- **Responsive Design:** Completely mobile-friendly layouts utilizing CSS Grid and Flexbox.

## 🛠️ Technologies Built With
- **HTML5:** Semantic architecture.
- **CSS3:** Vanilla CSS featuring Custom Properties (Variables), Grid, Flexbox, media queries, and keyframe animations.
- **JavaScript (ES5/ES6 Docs):** Procedural scripting for DOM manipulation, `localStorage` state management, and algorithmic filtering.
- **FontAwesome:** For vector icons.

## 📂 File Structure
```text
📦 UniNest
 ┣ 📂 css
 ┃ ┗ 📜 style.css           # Contains all styling, responsive rules, and dark mode.
 ┣ 📂 js
 ┃ ┣ 📜 data.js             # The mock database (array of objects).
 ┃ ┣ 📜 details.js          # Logic for the individual property details page.
 ┃ ┣ 📜 index.js            # Home page logic.
 ┃ ┣ 📜 lang.js             # Our custom translation engine and UI chatbot.
 ┃ ┗ 📜 listings.js         # Search, filters, and rendering logic.
 ┣ 📜 index.html            # Landing page.
 ┣ 📜 login.html            # Registration and Authentication.
 ┣ 📜 listings.html         # Main discover page for properties.
 ┣ 📜 details.html          # Dynamic view for selected properties.
 ┣ 📜 add-property.html     # Dashboard for owners to add listings.
 ┗ 📜 profile.html          # User profile to manage favorites or listings.
```

## ⚙️ How to Run
1. Clone or download this repository to your local machine.
2. Open the project folder in **VS Code**.
3. Install the **Live Server** extension.
4. Right-click on `index.html` and select **"Open with Live Server"**.
*(Note: It is highly recommended to run this via a local server to ensure `localStorage` policies perform flawlessly across all browsers).*

## 👨‍💻 Developed By
- **EELU Students** (2nd Year CS)

> *Developed with ❤️ for our university project.*
