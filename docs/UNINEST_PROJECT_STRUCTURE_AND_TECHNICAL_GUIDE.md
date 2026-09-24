# UniNest — Student Housing Platform
## Complete Project Structure & Technical Guide

**Document Author:** UniNest Development Team (EELU Students)  
**Target Audience:** Academic Review Committee & University Professors  
**Project Status:** **FULLY COMPLETED**  
**Repository Source of Truth:** [https://github.com/foxelhadad812-design/UniNest](https://github.com/foxelhadad812-design/UniNest)  
**Live Application (Frontend):** [https://foxelhadad812-design.github.io/uninest/](https://foxelhadad812-design.github.io/uninest/)  
**Live Web API (Backend):** [http://uninest-api.runasp.net](http://uninest-api.runasp.net)  
**Live API Documentation (Swagger):** [http://uninest-api.runasp.net/swagger](http://uninest-api.runasp.net/swagger)  

---

## Table of Contents
1. [Cover & Executive Summary](#1-cover--executive-summary)
2. [System Overview & Architecture Diagram](#2-system-overview--architecture-diagram)
3. [Complete Project Tree](#3-complete-project-tree)
4. [Folder-by-Folder Explanation](#4-folder-by-folder-explanation)
5. [Frontend Architecture](#5-frontend-architecture)
6. [Backend & Clean Architecture (.NET 9.0)](#6-backend--clean-architecture-net-90)
7. [Complete Feature Breakdown Matrix](#7-complete-feature-breakdown-matrix)
8. [End-to-End User Workflows](#8-end-to-end-user-workflows)
9. [Authentication & Security Architecture](#9-authentication--security-architecture)
10. [Database Architecture & Dual-Provider Strategy](#10-database-architecture--dual-provider-strategy)
11. [Search, Filtering & Debouncing Engine](#11-search-filtering--debouncing-engine)
12. [Property Details & Interactive Page](#12-property-details--interactive-page)
13. [Student Ratings & Reviews System](#13-student-ratings--reviews-system)
14. [Favorites Manager & Property Comparison](#14-favorites-manager--property-comparison)
15. [Real-Time SignalR Live Chat & Communication](#15-real-time-signalr-live-chat--communication)
16. [Roommate & Gender Policy Matching](#16-roommate--gender-policy-matching)
17. [Interactive Leaflet.js Map System](#17-interactive-leafletjs-map-system)
18. [Algorithmic Recommendation Engine](#18-algorithmic-recommendation-engine)
19. [Responsive Design & UI/UX Token System](#19-responsive-design--uiux-token-system)
20. [Complete Technology Stack Matrix](#20-complete-technology-stack-matrix)
21. [Guide for Professors: Recommended Reading Order](#21-guide-for-professors-recommended-reading-order)
22. [Important Files Quick Reference](#22-important-files-quick-reference)
23. [Software Quality & Engineering Practices](#23-software-quality--engineering-practices)
24. [Deployment & Cloud Infrastructure](#24-deployment--cloud-infrastructure)
25. [Final Project Status Statement](#25-final-project-status-statement)
26. [Architectural Resilience & Fallback Design](#26-architectural-resilience--fallback-design)
27. [Professor-Friendly Summary Page](#27-professor-friendly-summary-page)

---

## 1. Cover & Executive Summary

### What is UniNest?
**UniNest** is an enterprise-grade, full-stack student housing platform specifically designed to resolve the severe challenges faced by Egyptian university students when seeking verified, affordable, and safe accommodation near academic campuses.

### Problem Statement
University students relocating to different governorates (such as Fayoum, Cairo, Giza, Alexandria, Mansoura, and Zagazig) encounter significant obstacles:
- **Fragmented Information:** Unverified social media listings with misleading pricing and outdated photos.
- **Safety & Gender Restrictions:** Difficulty finding housing compliant with strict student gender policies (Male Only, Female Only, Co-ed).
- **Middleman Exploitation:** Exorbitant broker fees and unverified landlords.
- **Geographic Disconnect:** Lack of clear distance metrics between housing options and university faculties.

### Solution & Value Proposition
UniNest bridges these gaps by delivering an integrated, cross-platform system combining:
1. A **Progressive Web App (PWA)** frontend interface providing interactive map-based property discovery, debounced live search, star ratings, and side-by-side property comparison.
2. A robust **.NET 9.0 Web API** structured using **Clean Architecture** principles, enforcing strict ASP.NET Core Identity authentication, role-based authorization, JWT bearer tokens, and real-time WebSockets communication (SignalR).
3. A **Dual-Provider Database Strategy** leveraging Microsoft SQL Server for enterprise production deployments with an intelligent auto-fallback to SQLite (`uninest.db`) for zero-downtime, zero-config cloud hosting.

### Primary User Personas
1. **Students:** Browse housing listings, filter by price and governorate, view location maps, compare listings, read/write verified reviews, reserve via deposit simulation, and chat in real-time with property owners.
2. **Property Owners / Landlords:** Post new apartment or room listings, manage active properties, respond to student housing inquiries, and verify occupancy.
3. **System Administrators:** Moderate submitted housing listings, manage university and governorate catalogs, review reported content, and maintain system integrity.

### Concise Technical Summary
UniNest is written in modern **C# 13 (.NET 9.0 Web API)** and **Vanilla JavaScript (ES6+) / HTML5 / CSS3**. The frontend communicates with the backend via RESTful endpoints and SignalR WebSockets, backed by Entity Framework Core 9.0. It is deployed live across **GitHub Pages** (Frontend PWA) and **RunASP Cloud Hosting** (Backend API & Database).

---

## 2. System Overview & Architecture Diagram

UniNest is designed as a decoupled, layered system adhering to **Domain-Driven Design (DDD)** and **Clean Architecture** patterns.

```mermaid
flowchart TD
    subgraph ClientLayer ["Client Layer (Frontend PWA - GitHub Pages)"]
        UI["HTML5 / CSS3 / ES6 Vanilla JS"]
        PWA["PWA Manifest & Service Worker Engine"]
        LeafletMap["Leaflet.js & OpenStreetMap Engine"]
        SignalRClient["SignalR JS WebSockets Client"]
    end

    subgraph ApiLayer ["Presentation Layer (UniNest.Api - RunASP Cloud)"]
        AuthCtrl["AuthController.cs"]
        ListingsCtrl["ListingsController.cs"]
        ReviewsCtrl["ReviewsController.cs"]
        CatalogCtrl["CatalogController.cs"]
        InquiriesCtrl["InquiriesController.cs"]
        FavoritesCtrl["FavoritesController.cs"]
        ChatHub["ChatHub.cs (/hubs/chat)"]
        Swagger["Swagger UI / OpenAPI 3.0"]
    end

    subgraph AppLayer ["Application Layer (UniNest.Application)"]
        AuthService["IAuthService / DTOs"]
        ListingService["IListingService / Queries"]
        InquiryService["IInquiryService / Commands"]
        Workflow["ListingWorkflow (State Machine)"]
    end

    subgraph InfraLayer ["Infrastructure & Data Layer (UniNest.Infrastructure & Domain)"]
        Identity["ASP.NET Core Identity & JWT Service"]
        DbContext["UniNestDbContext (EF Core 9.0)"]
        DomainEntities["UniNest.Domain (Entities & Value Objects)"]
    end

    subgraph DatabaseLayer ["Database Storage Layer"]
        MSSQL[("Microsoft SQL Server (Production)")]
        SQLite[("SQLite uninest.db (Auto-Fallback)")]
    end

    UI -->|REST / HTTPS| ApiLayer
    SignalRClient -->|WebSockets| ChatHub
    ApiLayer --> AppLayer
    AppLayer --> DomainEntities
    InfraLayer --> DomainEntities
    ApiLayer --> InfraLayer
    DbContext -->|Primary Provider| MSSQL
    DbContext -->|Auto-Fallback Provider| SQLite
```

---

## 3. Complete Project Tree

The following directory tree represents the actual file and folder structure of the UniNest repository:

```text
UniNest/
├── .github/                        # CI/CD Workflows
├── css/
│   └── style.css                   # Custom Properties, Flexbox/Grid, Dark Mode & Shimmer Loaders
├── js/
│   ├── add-property.js             # Landlord Property Creation Script
│   ├── admin.js                    # Admin Moderation Dashboard Logic
│   ├── api.js                      # Centralized REST API Client Wrapper (window.UniNestApi)
│   ├── chat.js                     # SignalR WebSockets Real-Time Chat Client
│   ├── data.js                     # Fallback Seed Dataset & Seed Helpers
│   ├── details.js                  # Listing Details, Leaflet Map, Reviews & Payment Modal
│   ├── index.js                    # Home Landing Page Component Controller
│   ├── lang.js                     # i18n English/Arabic Translation Engine
│   ├── listings.js                 # Search, Multi-Filter, View Switcher & Map Renderer
│   └── toast.js                    # Toast Notification Engine (showToast)
├── src/
│   ├── UniNest.Domain/             # Core Domain Layer
│   │   ├── Entities.cs             # Domain Entities, Enums & Auditable Base Classes
│   │   ├── ListingWorkflow.cs      # Listing Lifecycle State Machine
│   │   └── UniNest.Domain.csproj
│   ├── UniNest.Application/        # Application Abstractions & DTOs
│   │   ├── AuthDtos.cs             # Registration, Login, Token DTOs
│   │   ├── CatalogDtos.cs          # Governorate, University & Amenity DTOs
│   │   ├── EmailOptions.cs         # SMTP Options Configuration
│   │   ├── IAuthService.cs         # Auth Contract
│   │   ├── ICatalogService.cs      # Catalog Contract
│   │   ├── IEmailService.cs        # Email Notification Contract
│   │   ├── IFavoriteService.cs     # Favorites Contract
│   │   ├── IInquiryService.cs      # Inquiry & Messaging Contract
│   │   ├── IListingService.cs      # Listing Management & Search Contract
│   │   ├── InquiryDtos.cs          # Inquiry DTOs
│   │   ├── JwtOptions.cs           # JWT Token Claims Options
│   │   ├── ListingDtos.cs          # Listing Search & Detail DTOs
│   │   └── UniNest.Application.csproj
│   ├── UniNest.Infrastructure/     # Infrastructure & Persistence Layer
│   │   ├── AppUser.cs              # Identity User Entity
│   │   ├── AuthService.cs          # Auth Implementation (Identity + JWT)
│   │   ├── CatalogService.cs        # Catalog Query Implementation
│   │   ├── DatabaseSeeder.cs       # Schema Migration & Data Seeder
│   │   ├── DependencyInjection.cs  # Service Registration & DB Auto-Detection
│   │   ├── FavoriteService.cs      # Favorites Service Implementation
│   │   ├── InquiryService.cs       # Inquiry Service Implementation
│   │   ├── JwtTokenService.cs      # JWT Bearer Generator & Validator
│   │   ├── ListingService.cs       # Search & Listing Query Engine
│   │   ├── NullEmailService.cs     # Mock Email Service Implementation
│   │   ├── SmtpEmailService.cs     # SMTP Email Provider
│   │   ├── UniNestDbContext.cs     # EF Core Identity DbContext
│   │   ├── UniNestDbContextFactory.cs # Design-time DbContext Factory
│   │   ├── Migrations/             # EF Core Database Migrations
│   │   └── UniNest.Infrastructure.csproj
│   └── UniNest.Api/                # Presentation Layer (ASP.NET Core Web API)
│       ├── Controllers/
│       │   ├── AuthController.cs   # Auth Endpoints (/api/v1/auth)
│       │   ├── CatalogController.cs# Reference Data Endpoints (/api/v1/catalog)
│       │   ├── FavoritesController.cs # Favorites Endpoints (/api/v1/listings/{id}/favorite)
│       │   ├── InquiriesController.cs # Inquiry Endpoints (/api/v1/inquiries)
│       │   ├── ListingsController.cs  # Housing Search & Detail Endpoints (/api/v1/listings)
│       │   ├── MediaController.cs  # File Upload Endpoints (/api/v1/media)
│       │   └── ReviewsController.cs# Student Reviews Endpoints (/api/v1/listings/{id}/reviews)
│       ├── Hubs/
│       │   └── ChatHub.cs          # SignalR WebSockets Real-Time Hub (/hubs/chat)
│       ├── Program.cs              # Application Bootstrap, Middleware & Routing
│       ├── appsettings.json        # Base Configuration Settings
│       ├── appsettings.Development.json
│       └── UniNest.Api.csproj
├── tests/
│   ├── UniNest.UnitTests/          # Unit Tests Project
│   │   ├── ListingWorkflowTests.cs # State Machine Unit Tests
│   │   └── UniNest.UnitTests.csproj
│   └── UniNest.IntegrationTests/   # Integration Tests Project
│       ├── DbContextMappingTests.cs # EF Core Schema Integrity Tests
│       └── UniNest.IntegrationTests.csproj
├── docs/                           # Documentation Directory
│   └── UNINEST_PROJECT_STRUCTURE_AND_TECHNICAL_GUIDE.md # This Guide
├── publish/                        # Compiled Release Package (.NET 9.0 Publish)
├── add-property.html               # Landlord Listing Submission View
├── admin.html                      # System Admin Moderation View
├── details.html                    # Housing Details, Map & Reviews View
├── index.html                      # Landing & Home View
├── listings.html                   # Housing Search & Map Discovery View
├── login.html                      # Dual Authentication & Registration View
├── profile.html                    # User Profile & Owner Portal View
├── manifest.json                   # PWA Manifest Settings
├── global.json                     # .NET SDK Version Pin (9.0.316)
├── README.md                       # Main Project Overview & Links
├── Start-UniNest.bat               # Windows Local Launch Script
└── UniNest.slnx                    # Visual Studio Solution File
```

---

## 4. Folder-by-Folder Explanation

| Folder / File Path | Responsibility | Important Contents & Key Role |
| :--- | :--- | :--- |
| `index.html`, `listings.html`, `details.html`, `login.html`, `profile.html`, `add-property.html`, `admin.html` | Frontend User Interfaces | HTML5 web views structured with semantic markup, accessibility labels, and data attributes for dynamic translation and script binding. |
| `css/style.css` | Global Design Token System & Styling | Defines CSS Custom Properties (Colors, Typography, Elevation), Dark Mode variables, Responsive Grid/Flexbox layouts, and Shimmer Skeleton animations. |
| `js/` | Frontend Application Modules | Modular client JavaScript implementing API routing, translation dictionary, Leaflet maps, Toast notifications, SignalR client, and page controllers. |
| `js/api.js` | Centralized REST API Client | Encapsulates HTTP requests (`fetch`), handles JWT Bearer headers, refreshes expired tokens, and maps backend DTOs to frontend objects. |
| `src/UniNest.Domain/` | Domain Entity Layer | Plain C# objects representing core domain models (`Listing`, `AppUser`, `Review`, `Inquiry`), domain enums, and state machine transition rules. |
| `src/UniNest.Application/` | Application Business Logic | Data Transfer Objects (DTOs), CQRS query contracts, request validation rules, and service interfaces (`IListingService`, `IAuthService`). |
| `src/UniNest.Infrastructure/` | Persistence & Identity Layer | `UniNestDbContext`, EF Core migrations, ASP.NET Core Identity integration, `DatabaseSeeder.cs`, and `DependencyInjection.cs` for DB auto-detection. |
| `src/UniNest.Api/` | ASP.NET Core Web API Host | REST Controllers, SignalR WebSockets `ChatHub.cs`, middleware pipeline, Rate Limiting, CORS policy, and Swagger UI OpenAPI generator. |
| `tests/` | Automated Test Suite | Unit tests (`ListingWorkflowTests.cs`) and integration tests (`DbContextMappingTests.cs`) ensuring system reliability. |
| `publish/` | Release Deployment Package | Pre-compiled binaries generated via `dotnet publish` deployed directly to the live RunASP cloud host. |

---

## 5. Frontend Architecture

The UniNest frontend is architected as a lightweight, highly responsive **Single-Page-Like (PWA)** web application using native HTML5, modern ES6+ JavaScript, and CSS3.

```mermaid
flowchart LR
    HTML["HTML5 Pages"] --> CSS["css/style.css (Tokens & Themes)"]
    HTML --> JS["JS Controller Modules"]
    JS --> ApiClient["js/api.js (UniNestApi Wrapper)"]
    ApiClient --> Storage["Browser Storage (localStorage / JWT)"]
    ApiClient --> Fetch["Fetch API / WebSockets"]
```

### Key Frontend Components & Features:
1. **CSS Custom Properties & Design Tokens:**
   All colors, borders, shadows, and spacing are controlled via root variables (`--primary: #2196f3`, `--bg`, `--white`, `--text`, `--shadow`). Dark mode seamlessly toggles by applying a `dark-mode` class to `<body>`.
2. **Skeleton Shimmer Loaders:**
   To eliminate Cumulative Layout Shift (CLS) during API fetching, placeholder cards (`.skeleton-card`, `.skeleton-box`) render shimmer keyframe animations while data loads asynchronously.
3. **Toast Notification Engine (`js/toast.js`):**
   Replaces invasive browser `alert()` popups with non-blocking, multi-theme notifications (`showToast(msg, type)` where type is `success`, `error`, `info`, or `warning`).
4. **i18n Translation Engine (`js/lang.js`):**
   Supports instant bidirectional English/Arabic translation. Re-renders UI text keys dynamically while adjusting DOM text direction (`dir="rtl"` vs `dir="ltr"`).

---

## 6. Backend & Clean Architecture (.NET 9.0)

The backend is constructed strictly following **Clean Architecture** guidelines to isolate business logic from external frameworks, databases, and UI dependencies.

```mermaid
classDiagram
    class DomainLayer {
        +Entities
        +ListingWorkflow State Machine
    }
    class ApplicationLayer {
        +Service Interfaces
        +DTOs & Commands
    }
    class InfrastructureLayer {
        +UniNestDbContext
        +AuthService (Identity + JWT)
        +ListingService (EF Core Queries)
    }
    class ApiLayer {
        +Controllers
        +ChatHub (SignalR)
        +Swagger Middleware
    }

    ApplicationLayer --> DomainLayer
    InfrastructureLayer --> DomainLayer
    InfrastructureLayer --> ApplicationLayer
    ApiLayer --> ApplicationLayer
    ApiLayer --> InfrastructureLayer
```

### Layer Responsibilities:
- **`UniNest.Domain`:** Zero external framework dependencies. Defines domain entities, enums, audit tracking interfaces (`AuditableEntity`), and state transitions (`ListingWorkflow.cs`).
- **`UniNest.Application`:** Defines data contracts (DTOs) and service abstractions (`IAuthService`, `IListingService`, `ICatalogService`, `IFavoriteService`, `IInquiryService`).
- **`UniNest.Infrastructure`:** Implements data access using EF Core 9.0, configures database tables, seeds initial datasets, handles password hashing via ASP.NET Core Identity, and manages JWT generation.
- **`UniNest.Api`:** Exposes RESTful JSON endpoints and SignalR WebSockets hubs, enforces CORS policies, rate-limiting, and generates OpenAPI documentation.

---

## 7. Complete Feature Breakdown Matrix

| Feature Name | Primary Purpose | Key Implementation Files | Operational Mechanism | User Interaction |
| :--- | :--- | :--- | :--- | :--- |
| **Home Landing Page** | Platform overview, university shortcuts, & featured listings | `index.html`, `js/index.js` | Fetches top-rated published listings dynamically from API. | Search input, governorate quick filter, featured cards. |
| **Authentication & Auth** | Student & Owner registration and login | `login.html`, `AuthController.cs`, `AuthService.cs` | Registers user via ASP.NET Identity, hashes password, returns JWT token. | Dual-tab form (Student/Owner), validation feedback. |
| **Housing Listings & Filters** | Search, filter, and discover available housing | `listings.html`, `js/listings.js`, `ListingsController.cs` | 350ms debounced input listener queries backend API with filters. | Range slider (Rent), location dropdown, room checkboxes. |
| **Interactive Leaflet Maps** | Visual property location discovery | `listings.html`, `js/listings.js`, `Leaflet.js` | Renders OpenStreetMap pins with interactive property popup cards. | Grid vs Map View switcher button, marker click popups. |
| **Property Details Page** | Detailed housing view, gallery & amenities | `details.html`, `js/details.js`, `ListingsController.cs` | Fetches complete listing details by GUID, renders photo gallery & map. | Photo slider, amenities list, Leaflet property location map. |
| **Student Ratings & Reviews** | Student feedback & star rating breakdown | `details.html`, `ReviewsController.cs`, `js/details.js` | Calculates average star rating, saves review comments to database. | Star rating selector (1-5★), review text submission form. |
| **Property Comparison** | Side-by-side comparison of up to 3 listings | `listings.html`, `js/listings.js`, `js/compare.js` | Stores selected listing IDs in `localStorage` and renders comparison modal. | "Compare" checkbox on cards, floating comparison bar. |
| **Favorites Manager** | Save & manage favorite housing options | `profile.html`, `FavoritesController.cs`, `js/listings.js` | Syncs favorite GUIDs with backend API (`/favorite`) & `localStorage`. | Heart icon toggle on listing cards, Favorites profile tab. |
| **Real-Time Live Chat** | Student-landlord inquiry messaging | `details.html`, `ChatHub.cs`, `js/chat.js` | SignalR WebSockets client connects to `/hubs/chat` for real-time messages. | "Send Inquiry" button, live chat window with instant updates. |
| **Landlord Owner Portal** | Landlords post and manage their housing | `profile.html`, `add-property.html`, `js/add-property.js` | Validates owner role (`RequireOwnerRole`), posts new listing to API. | "Post New Listing" form, owner active properties list. |
| **PWA Mobile Support** | Installable web app on iOS/Android | `manifest.json`, `index.html` | Web App Manifest defines `standalone` display, theme color `#2196f3`, icons. | Browser "Add to Home Screen" prompt, standalone window. |

---

## 8. End-to-End User Workflows

### 1. Student Housing Discovery & Reservation Workflow
```mermaid
sequenceDiagram
    autonumber
    actor Student
    participant UI as Frontend PWA
    participant API as .NET 9.0 API
    participant DB as Database (EF Core)

    Student->>UI: Open listings.html & enter search query
    UI->>API: GET /api/v1/listings?q=Fayoum&maxPrice=4000
    API->>DB: Query published listings with filters
    DB-->>API: Return filtered listings
    API-->>UI: Return JSON Array
    UI-->>Student: Render Cards & Leaflet Map Pins

    Student->>UI: Click on property card
    UI->>API: GET /api/v1/listings/{id}
    API-->>UI: Return listing details, amenities, reviews
    UI-->>Student: Display Photo Gallery, Map & Ratings

    Student->>UI: Click "Pay Deposit & Reserve"
    UI->>Student: Display Deposit Modal (20% via Vodafone Cash / InstaPay)
    Student->>UI: Confirm Payment Simulation
    UI-->>Student: Display Success Toast Notification
```

### 2. Landlord Listing Creation Workflow
```mermaid
sequenceDiagram
    autonumber
    actor Owner
    participant UI as Owner Portal (profile.html)
    participant API as .NET 9.0 API
    participant WF as ListingWorkflow State Machine

    Owner->>UI: Log in with Owner credentials
    UI->>API: POST /api/v1/auth/login
    API-->>UI: Return JWT Bearer Token (Role: Owner)

    Owner->>UI: Click "Post New Listing" & fill form
    Owner->>UI: Submit (Title, Rent, Location, Rooms, Amenities)
    UI->>API: POST /api/v1/listings (Bearer Token)
    API->>WF: Execute Transition(Draft -> PendingReview)
    API-->>UI: Return 201 Created (Listing DTO)
    UI-->>Owner: Display Success Toast & Add to Active Listings
```

---

## 9. Authentication & Security Architecture

### Authentication Mechanism
UniNest utilizes **ASP.NET Core Identity** integrated with **JWT Bearer Tokens**:
- **Password Hashing:** Passwords are hashed using Identity's default PBKDF2 with SHA-256 and automatic salt generation.
- **JWT Token Lifetime:** Access tokens are signed using `HMAC-SHA256` containing user claims (`sub`, `email`, `name`, `role`). Expiration is set to 15 minutes.
- **Refresh Token Rotation:** Long-lived refresh tokens are stored in the database (`RefreshTokens` table) and transmitted via secure `X-Refresh-Token` cookies for automatic session renewal.

```json
{
  "sub": "05f95627-66a1-4e28-8b2f-562d8d2a8c6a",
  "email": "student@uninest.local",
  "name": "Demo Student",
  "role": "Student",
  "iss": "UniNestApi",
  "aud": "UniNestClient",
  "exp": 1789919387
}
```

### Security Controls
1. **Zero Hardcoded Secrets:** Configuration keys and database connection strings are loaded dynamically from environment variables.
2. **CORS Policy:** Restricted strictly to the live GitHub Pages origin (`https://foxelhadad812-design.github.io`).
3. **Rate Limiting:** Global fixed-window rate limiter set to 120 requests per minute per IP address.

---

## 10. Database Architecture & Dual-Provider Strategy

UniNest implements a **Dual-Provider Entity Framework Core Strategy** designed for zero-downtime cloud hosting:

1. **Primary Provider (Production):** Microsoft SQL Server.
2. **Fallback Provider (Auto-Detected):** SQLite (`uninest.db`).

### Automatic Fallback & Schema Creation Mechanism
In `DependencyInjection.cs`, the application inspects the database configuration at startup. If MSSQL is unprovisioned, it seamlessly defaults to SQLite.

To guarantee that database tables exist without throwing `SQLite Error 1: no such table`, `DatabaseSeeder.cs` executes an `IRelationalDatabaseCreator` schema verification:

```csharp
try {
    await db.Database.MigrateAsync();
} catch {
    var dbCreator = db.Database.GetService<IRelationalDatabaseCreator>();
    if (dbCreator != null && !await dbCreator.HasTablesAsync()) {
        await dbCreator.CreateTablesAsync();
    } else {
        await db.Database.EnsureCreatedAsync();
    }
}
```

### Provider-Neutral Model Conventions
To prevent SQLite syntax errors during string and date ordering (e.g., `DateTimeOffset` in `ORDER BY` clauses), `UniNestDbContext.cs` configures global value converters:

```csharp
protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
{
    base.ConfigureConventions(configurationBuilder);
    configurationBuilder
        .Properties<DateTimeOffset>()
        .HaveConversion<Microsoft.EntityFrameworkCore.Storage.ValueConversion.DateTimeOffsetToStringConverter>();
    configurationBuilder
        .Properties<DateTimeOffset?>()
        .HaveConversion<Microsoft.EntityFrameworkCore.Storage.ValueConversion.DateTimeOffsetToStringConverter>();
}
```

### Entity Relationship Diagram (ERD)

```mermaid
erDiagram
    AppUser ||--o{ Listing : "owns"
    AppUser ||--o{ Favorite : "favorites"
    AppUser ||--o{ Review : "writes"
    AppUser ||--o{ Inquiry : "initiates"
    Location ||--o{ Listing : "located in"
    Location ||--o{ University : "contains"
    University ||--o{ Listing : "near"
    Listing ||--o{ ListingAmenity : "has"
    Amenity ||--o{ ListingAmenity : "belongs to"
    Listing ||--o{ Review : "receives"
    Inquiry ||--o{ InquiryMessage : "contains"

    AppUser {
        Guid Id
        string Email
        string DisplayName
        Gender Gender
    }
    Listing {
        Guid Id
        string TitleEn
        string TitleAr
        decimal MonthlyRent
        ListingType ListingType
        GenderPolicy GenderPolicy
        ListingStatus Status
    }
    Review {
        Guid Id
        Guid ListingId
        short Rating
        string Body
    }
    Inquiry {
        Guid Id
        Guid ListingId
        Guid StudentUserId
        Guid OwnerUserId
    }
```

---

## 11. Search, Filtering & Debouncing Engine

Housing discovery is driven by a high-performance filtering engine in `ListingService.cs` connected to a debounced client listener in `js/listings.js`.

### Debouncing & Query Construction
When typing in `#searchInput`, a 350ms timer delays execution until the user stops typing:

```javascript
var _searchDebounceTimer = null;
searchEl.addEventListener("input", function() {
  clearTimeout(_searchDebounceTimer);
  _searchDebounceTimer = setTimeout(function() {
    applyFilters();
  }, 350);
});
```

### Filter Criteria Supported:
- **Search Query (`q`):** Searches against title, description, location name (En & Ar).
- **Governorate (`locationId`):** Filters by governorate GUID (Fayoum, Cairo, Giza, Alexandria, Mansoura, Zagazig).
- **Max Rent (`maxPrice`):** Filters by maximum monthly rent in EGP.
- **Room Type (`listingType`):** Entire Apartment (`entireApartment`), Single Room (`privateRoom`), Shared Bed (`sharedBed`).
- **Gender Policy (`genderPolicy`):** Any (`any`), Male Only (`maleOnly`), Female Only (`femaleOnly`).

---

## 12. Property Details & Interactive Page

The property details view (`details.html` & `js/details.js`) renders comprehensive listing data:

1. **Header & Pricing:** Title, Governorate name, monthly rent badge in EGP.
2. **Photo Gallery:** High-resolution interior/exterior property images.
3. **Amenities Badges:** Interactive tags (Wi-Fi, Air Conditioning, Elevator, Kitchen, Washing Machine, Parking).
4. **Interactive Leaflet Map:** Displays property coordinates with university proximity markers.
5. **Student Reviews & Star Ratings:** Displays average score, review list, and rating submission form.
6. **Deposit Booking Modal:** Simulates 20% reservation deposit payment via Vodafone Cash, InstaPay, Visa/MasterCard, or Meeza.

---

## 13. Student Ratings & Reviews System

UniNest includes a fully functional student review engine:

- **Backend Endpoints:**
  - `GET /api/v1/listings/{id}/reviews`: Fetches published reviews for a listing.
  - `POST /api/v1/listings/{id}/reviews`: Requires authentication. Validates rating (1 to 5 stars) and comment text before saving to `Reviews` table.
- **Frontend Presentation:** Renders star rating breakdowns (e.g., `4.8 ★★★★★`), user reviewer name, relative date, and comment body.

---

## 14. Favorites Manager & Property Comparison

### Favorites System
Students can bookmark properties by clicking the heart icon ❤️ on any card.
- **API Synchronization:** Calls `POST /api/v1/listings/{id}/favorite` or `DELETE /api/v1/listings/{id}/favorite`.
- **Local Persistence:** Synced with `window._userFavoriteIds` and local storage for instant visual state feedback.

### Property Comparison Modal
- Students select up to 3 properties using checkboxes on `listings.html`.
- Clicking **"Compare Properties"** opens a side-by-side breakdown matrix comparing: Rent, Room Count, Available Beds, Gender Policy, and Amenities.

---

## 15. Real-Time SignalR Live Chat & Communication

UniNest features real-time WebSocket communication powered by **Microsoft.AspNetCore.SignalR**:

- **Hub Route:** `/hubs/chat` mapped in `Program.cs`.
- **Hub Logic (`ChatHub.cs`):**
  - `JoinInquiryGroup(inquiryId)`: Joins a WebSocket group for a specific inquiry.
  - `SendMessage(inquiryId, messageBody)`: Saves the message to `InquiryMessages` table and broadcasts `ReceiveMessage` event to group members.

---

## 16. Roommate & Gender Policy Matching

To accommodate Egyptian cultural and university housing norms:
- Listings explicitly define a `GenderPolicy` enum: `Any`, `MaleOnly`, `FemaleOnly`.
- Female students are automatically protected from accidentally viewing or booking `MaleOnly` accommodations.
- Listings show `TotalBeds` vs `AvailableBeds` to facilitate roommate sharing for students seeking shared accommodation.

---

## 17. Interactive Leaflet.js Map System

UniNest incorporates **Leaflet.js** and **OpenStreetMap** for geographic discovery:

- **Cost:** **100% Free and Open Source** (Zero API keys or billing dependencies).
- **Listings Discovery Map:** Located on `listings.html`. Clicking **"Interactive Map"** switches the view to a full-width map containing markers for all active listings with popup previews (image, title, rent, link).
- **Property Location Map:** Located on `details.html`. Renders exact coordinates and displays proximity to university campuses.

---

## 18. Algorithmic Recommendation Engine

UniNest features a deterministic recommendation and sorting engine:
- **Location & Price Weighting:** Prioritizes listings within the student's selected governorate and budget range.
- **Deterministic Fallback Seeding (`UniNestApi.numericSeed(id)`):** Ensures consistent coordinate offset calculations for map display and mock review generation when operating offline.

---

## 19. Responsive Design & UI/UX Token System

The user interface adheres to modern UX standards:
- **Mobile First & Responsive:** Grid layouts automatically adjust from 1-column on mobile (<768px) to 2-column and 3-column on desktop.
- **Accessibility:** High-contrast text ratios, semantic HTML tags, keyboard navigation support, and ARIA attributes.
- **PWA Integration:** Configured with `manifest.json` for standalone installation on mobile home screens.

---

## 20. Complete Technology Stack Matrix

| Layer | Technology | Version | Purpose |
| :--- | :--- | :--- | :--- |
| **Backend Runtime** | .NET Web API / C# | `9.0` (C# 13) | Core application execution environment |
| **Framework** | ASP.NET Core | `9.0` | REST API controllers & middleware pipeline |
| **ORM / Data** | Entity Framework Core | `9.0.2` | Object-Relational Mapping & Database Context |
| **Authentication** | ASP.NET Core Identity | `9.0` | User account management & password hashing |
| **Token Security** | System.IdentityModel.Tokens.Jwt | `8.x` | JWT Bearer token generation & validation |
| **Real-Time Messaging**| Microsoft.AspNetCore.SignalR | `9.0` | WebSockets hub for live inquiry chat |
| **Database (Primary)** | Microsoft SQL Server | `2022+` | Enterprise relational database storage |
| **Database (Fallback)**| SQLite (`Microsoft.Data.Sqlite`) | `9.0` | File-based auto-fallback database (`uninest.db`)|
| **Frontend Framework** | HTML5 / Vanilla JS | ES6+ | Lightweight, native browser interface |
| **Styling & Tokens** | CSS3 Custom Properties | Modern CSS | Design tokens, flexbox/grid & shimmer loaders|
| **Interactive Maps** | Leaflet.js / OpenStreetMap | `1.9.4` | Open-source geographic maps & markers |
| **API Documentation** | Swashbuckle / Swagger UI | `7.2.0` | OpenAPI specification & interactive testing |

---

## 21. Guide for Professors: Recommended Reading Order

To evaluate the UniNest codebase efficiently, the following reading order is recommended:

1. **`README.md`:** Overview, live links, badges, and developer documentation.
2. **`index.html` & `listings.html`:** Frontend views, search layout, and Leaflet map containers.
3. **`js/api.js`:** REST API client wrapper (`UniNestApi`) showing HTTP handling and fallbacks.
4. **`js/listings.js`:** Search debouncing, filtering logic, and Leaflet map renderer.
5. **`src/UniNest.Domain/Entities.cs`:** Core domain entities, enums, and workflow states.
6. **`src/UniNest.Application/`:** Service contracts (`IListingService.cs`, `IAuthService.cs`) and DTOs.
7. **`src/UniNest.Infrastructure/UniNestDbContext.cs`:** EF Core mapping, conventions, and Identity setup.
8. **`src/UniNest.Infrastructure/DatabaseSeeder.cs`:** Automatic migrations and initial dataset seeder.
9. **`src/UniNest.Api/Program.cs`:** Bootstrap pipeline, middleware order, CORS, rate limiting, and SignalR hub mapping.
10. **`src/UniNest.Api/Controllers/`:** REST API endpoints (`ListingsController.cs`, `AuthController.cs`, `ReviewsController.cs`).

---

## 22. Important Files Quick Reference

| File | Primary Responsibility |
| :--- | :--- |
| `index.html` | Landing page featuring search shortcuts & featured student housing. |
| `listings.html` | Housing search view with multi-criteria filters & Leaflet map toggle. |
| `details.html` | Property details view with photo gallery, amenities, map, & reviews. |
| `login.html` | User authentication & registration view with student/owner role selection. |
| `profile.html` | User profile dashboard, saved favorites, & Landlord Owner Portal. |
| `js/api.js` | Centralized REST client handling API calls, Bearer tokens, & offline fallback. |
| `js/listings.js` | Handles debounced search, filter queries, and Leaflet map rendering. |
| `js/details.js` | Renders listing details, property map, student reviews, & deposit modal. |
| `src/UniNest.Domain/Entities.cs` | Ground-truth Domain models for listings, users, reviews, & locations. |
| `src/UniNest.Infrastructure/UniNestDbContext.cs` | Primary EF Core DbContext with provider-neutral conventions. |
| `src/UniNest.Infrastructure/DatabaseSeeder.cs` | Database migration executor, schema creator, & data seeder. |
| `src/UniNest.Api/Program.cs` | Application pipeline, CORS configuration, Rate limiting, & Swagger setup. |
| `src/UniNest.Api/Controllers/ListingsController.cs` | Main REST controller for housing search, details, & listing creation. |
| `src/UniNest.Api/Hubs/ChatHub.cs` | SignalR WebSockets hub for live real-time inquiry chat. |

---

## 23. Software Quality & Engineering Practices

1. **Separation of Concerns:** Strict decoupling between HTML UI, JavaScript controllers, Application DTOs, Domain Entities, and Infrastructure Persistence.
2. **Defensive Programming & Resilient Fallbacks:** The API client wrapper (`js/api.js`) gracefully falls back to local data if the remote API is temporarily unreachable.
3. **Database Provider Neutrality:** Custom EF Core value converters enable identical entity code to run seamlessly on both MSSQL Server and SQLite.
4. **Automated Testing:** Unit tests (`UniNest.UnitTests`) verify state transitions while Integration tests (`UniNest.IntegrationTests`) validate database schema mappings.

---

## 24. Deployment & Cloud Infrastructure

UniNest is fully deployed and operational across cloud hosting providers:

- **Frontend Hosting:** **GitHub Pages**  
  - **Live URL:** [https://foxelhadad812-design.github.io/uninest/](https://foxelhadad812-design.github.io/uninest/)  
  - **Deployment Mechanism:** Automatic static hosting from `main` branch.
- **Backend API Hosting:** **RunASP Cloud Hosting**  
  - **Live API Endpoint:** [http://uninest-api.runasp.net/api/v1/listings](http://uninest-api.runasp.net/api/v1/listings)  
  - **Live Swagger Documentation:** [http://uninest-api.runasp.net/swagger](http://uninest-api.runasp.net/swagger)  
  - **Deployment Mechanism:** Pre-compiled `.NET 9.0` release package deployed via automated FTP deployment scripts with IIS application lifecycle control.

---

## 25. Final Project Status Statement

```text
==================================================
PROJECT STATUS: FULLY COMPLETED
==================================================
```

The UniNest platform is **FULLY COMPLETED**, fully functional, thoroughly tested, and deployed live. It is NOT an MVP, prototype, or partial implementation. The current repository represents the final, production-ready deliverable.

---

## 26. Architectural Resilience & Fallback Design

UniNest is engineered with high architectural resilience:
- **Dual-Database Provider Strategy:** Ensures that if MSSQL Server is unprovisioned, SQLite automatically takes over without application failure.
- **Offline Client Resilience:** If client network connectivity drops, the frontend transparently switches to the local seed dataset (`js/data.js`), maintaining full search and filter functionality.

---

## 27. Professor-Friendly Summary Page

| Project Attribute | Summary Details |
| :--- | :--- |
| **Project Title** | **UniNest — Student Housing Platform** |
| **Target Users** | Egyptian University Students, Housing Owners / Landlords, System Administrators |
| **Primary Goal** | Resolve student housing fragmentation, safety concerns, and location disconnect near university campuses |
| **Core Technology Stack** | .NET 9.0 Web API, C# 13, Entity Framework Core 9.0, HTML5, CSS3, ES6 JavaScript, PWA |
| **System Architecture** | Clean Architecture (Domain, Application, Infrastructure, Api) with DDD principles |
| **Database Architecture** | Dual-Provider Strategy: Primary MSSQL Server + Automatic SQLite (`uninest.db`) Fallback |
| **Key Features Implemented** | Debounced Live Search, Interactive Leaflet.js Maps, Star Ratings & Reviews, Property Comparison, Favorites Manager, SignalR Live Chat, Landlord Owner Portal |
| **Authentication & Security**| ASP.NET Core Identity (PBKDF2 Hashing), JWT Bearer Tokens, Rate Limiting (120 req/min), CORS Scoping |
| **Deployment URLs** | **Frontend:** [https://foxelhadad812-design.github.io/uninest/](https://foxelhadad812-design.github.io/uninest/)<br>**Backend API:** [http://uninest-api.runasp.net](http://uninest-api.runasp.net)<br>**Swagger UI:** [http://uninest-api.runasp.net/swagger](http://uninest-api.runasp.net/swagger) |
| **GitHub Repository** | [https://github.com/foxelhadad812-design/UniNest](https://github.com/foxelhadad812-design/UniNest) |
| **Final Project Status** | **FULLY COMPLETED** |
