# 🍕 FOODI Food Admin — Digital Menu Studio

![FOODI Food Admin](wwwroot/images/foodi-logo.png)

The official **FOODI Food Admin** smiling pizza mascot and red/green/white outlined wordmark is used as **one shared PNG** (`wwwroot/images/foodi-logo.png`) in the TV header, home hero, admin sidebar, dashboard, footer and favicon. Any future sign-in or boards pages should reuse this same asset.

**A cinematic dark-mode TV food menu and admin studio, built with ASP.NET Core MVC, C# and JSON files instead of SQL.**

[![.NET 8](https://img.shields.io/badge/.NET-8.0-512BD4?logo=dotnet)](https://dotnet.microsoft.com/)
![ASP.NET Core MVC](https://img.shields.io/badge/ASP.NET_Core-MVC-24292f)
![JSON storage](https://img.shields.io/badge/Database-JSON_files-21262d)
![Theme count](https://img.shields.io/badge/TV_Themes-3-f4b860)

## ✨ Features

| Area | Capabilities |
| --- | --- |
| 📺 **TV showcase** | Premium fullscreen digital menu, large type, clock, photo-led featured area, automatic category rotation on `/Tv` |
| 🍔 **Food CRUD** | Create, edit, delete, sort, price, badge, availability, featured flag and allergens |
| 🗂️ **Category CRUD** | Create, edit, delete, sort and hide categories |
| 📸 **Image CRUD** | Upload JPEG/PNG/WebP up to 8 MB, edit image descriptions, delete and assign images to foods |
| 🎨 **Brand studio** | Restaurant name, logo initials, headline, description, accent color, currency, price visibility |
| 🖥️ **Theme picker** | Three distinct selectable themes with mini-previews, full TV preview and persistent active selection |
| 💾 **Data** | Auto-created `Data/menu.json` using locked atomic writes; local uploaded images in `wwwroot/uploads` |
| 🔒 **Admin** | Cookie-based admin session, anti-forgery tokens for forms; bootstrap URL for initial access |

## 🎨 Three premium TV menu designs

### 1. ✦ Noir & Gold

Black and champagne-gold. Editorial serif headings, restrained borders, elegant food presentations and subtle luxury lighting.

### 2. 🔥 Ember & Flame

Charcoal and orange. Glowing amber details, bold uppercase typography, smoky backgrounds and fire-grilled energy.

### 3. 💜 Neon Bistro

Electric purple and pink. Glossy futuristic cards, neon highlights, gradients and luminous accents.

Visit **Admin → TV Themes** to preview, select and **save** your favorite. All three styles share the same foods, categories, prices and photo library.

| Preview | URL |
| --- | --- |
| Noir & Gold | `/Tv?theme=noir` |
| Ember & Flame | `/Tv?theme=ember` |
| Neon Bistro | `/Tv?theme=neon` |
| Saved active selection | `/Tv` |

Preview links do not change the saved selection. Choosing a theme and clicking **Save selected theme** writes the value to `Data/menu.json`.

## 🚀 Run locally

Requires the [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0).

```bash
git clone https://github.com/1x4x/walter.nu-foodadmin.git
cd walter.nu-foodadmin
dotnet restore
dotnet run
```

Open the localhost address printed in your terminal and use:

- **Digital menu:** `/` or `/Tv`
- **Administrator bootstrap:** `/Admin?superadmin=walter`
- **Theme selection:** `/Admin#themes` after signing in

## 🧑‍🍳 Manage your restaurant menu

1. Open the admin URL.
2. Create categories in **Categories**.
3. Upload food photography in **Image Library**. Add or edit descriptive text, or delete old photos.
4. Add dishes in **Food Items** with prices, categories, badges, allergens and photo assignments.
5. Open **TV Themes** and try all three design previews; save the active design.
6. Personalize the restaurant using **Restaurant Settings**.
7. Open `/Tv` on a restaurant display and select **Fullscreen**. TV mode cycles through categories using the configured rotation interval.

## 📁 Project structure

```text
walter.nu-foodadmin/
├── Controllers/
│   ├── AdminController.cs
│   └── HomeController.cs
├── Models/MenuModels.cs
├── Services/MenuStore.cs
├── Views/
│   ├── Admin/Index.cshtml
│   ├── Home/Index.cshtml
│   └── Shared/_Layout.cshtml
├── wwwroot/
│   ├── css/site.css
│   ├── js/site.js
│   └── uploads/               # created on first upload
├── Data/menu.json              # created automatically, ignored by git
├── Program.cs
└── RestaurantVision.csproj
```

## 🔐 Security considerations

The requested `?superadmin=walter` URL is **a development convenience, not robust production authentication**. The key is in the URL and may be recorded by logs or browser history. For deployment, replace it with genuine login/identity controls, HTTPS and rate limiting, and restrict admin access to trusted users/networks. At minimum override the secret using `Admin__BootstrapKey` as an environment variable; **never expose the default key on a public server**.

All modifying admin forms require an authenticated admin cookie and anti-forgery token. Uploaded files have an 8 MB limit and basic format/signature validation; consider image re-encoding and malware scanning for public deployment. Uploaded files and JSON are intentionally excluded from source control.

## 📺 Display and deployment notes

- Display works best on a widescreen 16:9 TV with browser fullscreen (F11 or the app's **Fullscreen** action).
- Sample dishes are seeded on first launch. Upload your own photos to replace stylish placeholders.
- Showcase background images and Google Fonts are currently fetched from external hosts; replace with licensed local assets for offline/kiosk installation.
- The menu JSON is suitable for a single server process. Multi-server deployment requires more robust storage and coordination.
- Code is supplied for local development; verify builds and test in your target environment before public deployment.
