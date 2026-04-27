# PaScan - Advanced Gate Access Control System

PaScan is a comprehensive gate management solution designed for schools and offices. It integrates a web-based management dashboard with hardware-based RFID and QR code scanning to ensure secure and efficient access control.

## 🚀 Key Features

### 🌐 Web Dashboard (ASP.NET Core 8.0 MVC)
- **Student Module:** Profile management, device registration, and digital QR ID generation.
- **Admin Module:** Scanner management, device request approval, and RFID card issuance/renewal.
- **Real-time Monitoring:** Live scan logs with "Smart Intercept" technology for easy hardware-to-user binding.
- **Role-Based Security:** Distinct portals for Students, Admins, and automated Scanners.

### 🛡️ Hardware Bridge (WPF .NET 9.0)
- **Keyboard Hooking:** Converts standard USB RFID readers into secure background API clients.
- **Smart Tray Integration:** Live statistics (Daily scan counts, last student name) directly in the Windows System Tray.
- **Connection Diagnostics:** Built-in "Test Connection" tool for rapid setup and troubleshooting.
- **Resilient Polling:** Intelligent intercept logic that allows admins to capture RFID tags for registration without manual typing.

## 🛠️ Tech Stack
- **Backend:** C# ASP.NET Core 8.0, Entity Framework Core, MS SQL Server.
- **Frontend:** Bootstrap 5, Razor Views, JavaScript (Polling & Intercept).
- **Hardware Bridge:** WPF (C#), SharpHook (Low-level keyboard global hooks).

## 📂 Project Structure
- `/PaScan`: The main ASP.NET Core MVC web application.
- `/pascan-rfid-scanner`: The WPF Windows background service for RFID readers.
- `/docs`: Technical documentation and module breakdowns.

## 🚦 Getting Started

### 1. Web Application
1. Configure `appsettings.json` with your Database connection string.
2. Run `dotnet ef database update` to apply migrations.
3. Seed the database with `DbSeeder`.
4. Run the application via Visual Studio or `dotnet run`.

### 2. RFID Bridge App
1. Open the settings in the Bridge App.
2. Enter your **Server API URL** and **X-Api-Key**.
3. Use the **Test Connection** button to verify connectivity.
4. (Optional) Set a **Friendly Name** for the device (e.g., "Main Gate") to improve logging clarity.

---

*Part of the Elent Finals Project.*
