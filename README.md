# hellodocter

HelloDoctor.Care — an ASP.NET Core 8.0 MVC web application for doctor appointment booking (departments, doctors, consultation slots, appointments, subscriptions, and Razorpay payments).

## Project structure

```
hellodocter/
├── Controllers/          # MVC controllers (Admin, Auth, Home, Renewal)
├── Data/                 # EF Core DbContext (ApplicationDbContext)
├── Database/             # SQL Server scripts
│   ├── tablescript.sql   #   schema (tables)
│   ├── datascript.sql    #   seed data
│   ├── patientreport.sql #   Patient Report feature tables (run after tablescript.sql)
│   └── reset-admin-login.sql  # fix script for the admin login row
├── Models/               # Domain and view models
├── Properties/           # launchSettings.json, publish profiles
├── Views/                # Razor views (Admin, Auth, Home, Renewal, Shared)
├── wwwroot/              # Static assets (css, js, lib)
├── Program.cs            # App entry point (DI, auth, session, routing)
├── appsettings.json      # Connection string (SQL Server)
├── HelloDoctor.Care.csproj
├── HelloDoctor.Care.sln
├── libman.json           # Client-side library manager
└── package.json          # npm (Tailwind CSS)
```

---

# Patient Report feature

The admin side has a complete **Patient Report** module (sidebar → *Patient Report* / *Medical Reports*) with three parts:

1. **Patients-per-doctor report** — list every patient visit, filter by doctor and date, and print it (`Report → Patient Report`).
2. **Medical visit reports** — write **diagnosis / prescription / advice** for each patient visit, edit and print a doctor-friendly report (`Add Report` on a visit, or `New Report` for walk-ins).
3. **Report files** — upload **PDF / image** lab reports and scans to each report (max 20 MB per file), view and download them any time.

**One-time setup:** run `Database/patientreport.sql` in SSMS (after `tablescript.sql`) to create the two new tables. Uploaded files are stored in `wwwroot/uploads/reports/` (ignored by git).

Doctors (roles 2/3) also see these menu items and automatically see only their own patients' reports.

---

# Beginner guide — run this on your own computer

You will do 7 short parts. Every step says exactly what to click and what you should see.
(Steps written for **Windows**. On Mac/Linux, the only difference is Part 1.4: use Docker for SQL Server.)

## Part 1 — Install 4 programs (only once)

### 1.1 Install the .NET 8 SDK (this is what runs the app)

1. Open your browser and go to: **https://dotnet.microsoft.com/download/dotnet/8.0**
2. Under **".NET 8.0 SDK"**, click the download button for **Windows x64**.
3. Open the downloaded file → click **Install** → **Finish**.
4. Check that it worked: press the **Windows key**, type `cmd`, press **Enter**. In the black window type `dotnet --version` and press **Enter**.
   ✅ You should see a number like `8.0.404`.
   ❌ If it says *"not recognized"*: restart the computer and try again.

### 1.2 Install VS Code (the editor)

1. Go to **https://code.visualstudio.com/** and click **Download for Windows**.
2. Run the installer. On the "additional tasks" screen, tick **"Add to PATH"** and **"Register Code as an editor for supported file types"**, then keep clicking **Next → Install → Finish**.

### 1.3 Install the C# extension (makes VS Code understand the code)

1. Open VS Code.
2. Click the **Extensions** icon in the left sidebar (it looks like four squares ▦).
3. Type **C# Dev Kit** in the search box.
4. Click **Install** on the one published by **Microsoft**.
   ✅ It also installs a plain "C#" extension automatically. Good.

### 1.4 Install SQL Server (the database) and SSMS (the program that manages it)

1. Go to **https://www.microsoft.com/sql-server/sql-server-downloads**
2. Under **Express**, click **Download now**. Open the file you just downloaded.
3. Click **Basic** → **Accept** → **Install**.
4. When it finishes, a screen shows your **Server name** — usually `localhost\SQLEXPRESS`. **Write this down** (or take a photo). You need it in Parts 3 and 4.
5. Now install SSMS: go to **https://learn.microsoft.com/sql/ssms/download-sql-server-management-studio-ssms**, click **Download SSMS**, run it → **Install** → restart the computer if it asks.

## Part 2 — Open the code in VS Code

1. Download the code: open the GitHub page of this project, click the green **Code** button → **Download ZIP**.
2. Right-click the ZIP file → **Extract All...** → extract to `Documents\hellodoctor`.
3. Open VS Code → menu **File → Open Folder...** → select the `hellodoctor` folder → click **Select Folder**.
   ✅ The left sidebar should now show files like `Program.cs` and `appsettings.json`.
4. Open the terminal inside VS Code: menu **Terminal → New Terminal** (keyboard shortcut **Ctrl + `**). A panel opens at the bottom.
5. Type `dotnet --version` and press **Enter**.
   ✅ You should see `8.0.x`. If not, go back to Part 1.1.

## Part 3 — Create the database (only once)

1. Open **SQL Server Management Studio** (SSMS) from the Start menu.
2. In the "Connect to Server" window:
   - **Server name:** the name you wrote down in Part 1.4 (e.g. `localhost\SQLEXPRESS`)
   - **Authentication:** Windows Authentication
   - Click **Connect**.
   ✅ The left side ("Object Explorer") now shows your server.
3. Click the **New Query** button at the top-left.
4. Paste this and press **F5** (or click the **Execute** button):

   ```sql
   CREATE DATABASE docsekpn_hellodoctor;
   ```

   ✅ You should see *"Commands completed successfully"*.

5. Load the table structure: menu **File → Open → File...** → go to the `hellodocter\Database` folder → select **`tablescript.sql`** → **Open**. Press **F5**.
   ✅ Wait until you see *"Commands completed successfully"* (it creates all the `tbl_...` tables).
6. Do the same again for **`datascript.sql`** (File → Open → select it → press **F5**). This fills the database with the starting data (departments, districts, plans, and the admin user).
7. Quick check that it worked: click **New Query**, paste this, press **F5**:

   ```sql
   SELECT * FROM tbl_adminuser;
   ```

   ✅ You should see one row with username `admin`.

   💡 If Execute complains about a database, look at the dropdown box left of the Execute button and choose `docsekpn_hellodoctor`.

## Part 4 — Tell the app where the database is

1. In VS Code, in the left file list, click **`appsettings.json`**.
2. Find the `DefaultConnection` line. It looks like this:

   ```json
   "DefaultConnection": "Server=(localdb)\\MSSQLLocalDB;Database=docsekpn_hellodoctor;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True"
   ```

3. Change **only the `Server=...` part** to the server name from Part 1.4. For `localhost\SQLEXPRESS` the line becomes:

   ```json
   "DefaultConnection": "Server=.\\SQLEXPRESS;Database=docsekpn_hellodoctor;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True"
   ```

   Plain-English explanation of the parts:
   - `Server=.\SQLEXPRESS` → the database lives on **this computer** (`.` = this machine).
   - `Database=docsekpn_hellodoctor` → the database you created in Part 3. **Leave this unchanged.**
   - `Trusted_Connection=True` → use your Windows login (no database password needed).
   - `TrustServerCertificate=True` → accept the local server's security certificate (needed for local development).
   - ⚠️ In this file always type **`\\`** where you would normally type `\` — that is just how this file type works.
4. Press **Ctrl + S** to save the file.

## Part 5 — Start the app

1. Click in the terminal panel at the bottom of VS Code (open it with **Ctrl + `** if it is closed).
2. Type `dotnet restore` and press **Enter**. *(Only needed the first time — it downloads the packages the project needs. Wait until it says "Restored ...".)*
3. Type `dotnet run` and press **Enter**. Wait 10–30 seconds. When it is ready you will see lines like:

   ```
   info: Microsoft.Hosting.Lifetime[14]
         Now listening on: https://localhost:7132
   ```

4. Hold **Ctrl** and click `https://localhost:7132` (or type it in your browser).
5. Your browser may warn *"Your connection is not private"*. That is normal on your own computer. Click **Advanced** → **Continue to localhost (unsafe)**.
   ✅ You should see the HelloDoctor home page with the list of districts. 🎉

## Part 6 — Log in

1. In the browser address bar, add `/Auth/Login` after the address: **https://localhost:7132/Auth/Login**
2. Log in with the starter admin account (it comes from `datascript.sql`):
   - **Username:** `admin`
   - **Password:** `adhir@9856`
3. Click the login button.
   ✅ You should land on the **Admin dashboard** (appointments, doctors, departments, slots, ...).

## Part 7 — Stop now, start again later

- **To stop:** click in the terminal and press **Ctrl + C**.
- **Next time:** open VS Code → **File → Open Folder** → the `hellodocter` folder → **Terminal → New Terminal** → type `dotnet run`.
  The database is already created — you do **not** need Parts 1, 3 or 4 again, and usually not `dotnet restore` either.

---

## If something goes wrong

| What you see | What to do |
| --- | --- |
| `'dotnet' is not recognized` | The SDK is missing, or VS Code was open while installing it. Install again (Part 1.1), close and reopen VS Code, or restart the PC. |
| `A network-related or instance-specific error...` | SQL Server is not running. Press **Windows + R**, type `services.msc`, press Enter. Find **SQL Server (SQLEXPRESS)** in the list, right-click it → **Start**. Then `dotnet run` again. |
| `Cannot open database "docsekpn_hellodoctor"` | The database was not created (or you connected to a different server). Redo Part 3 and check Part 4 uses the same server name. |
| `Invalid object name 'tbl_...'` | The scripts ran in the wrong order or on the wrong database. Run `tablescript.sql` first, then `datascript.sql`, against `docsekpn_hellodoctor`. |
| Login always says `Invalid Username or Password` | The admin row is missing or damaged. In SSMS run `SELECT userid, uname, pwd FROM tbl_adminuser` — you should see `admin` / `K/3Wpm/8l6dHE//GrGiqrw==`. If not, run `Database/reset-admin-login.sql` (F5 in SSMS) and log in with `admin` / `adhir@9856`, typed manually. Also make sure SSMS and `appsettings.json` use the same server name. |
| `The remote certificate is invalid...` | Add `TrustServerCertificate=True` to the connection string (see Part 4). |
| "This site can't be reached" | The app is not running. Check the terminal still shows "Now listening on: ..." — otherwise run `dotnet run` again. |
| Ports 5099 / 7132 already in use | Change `applicationUrl` in `Properties/launchSettings.json` to another number (e.g. `https://localhost:7232`) and use that URL. |

## Notes

- **Razorpay payments** will not work until you paste your key and secret into `HomeController.GenerateOrderBooking`, `RenewalController.GenerateOrder` and `Views/Home/Razorpay.cshtml` (they are empty placeholders). The payment flow also writes to a `tbl_PaymentOrder` table that is not part of `tablescript.sql` yet.
- **Time zones:** the code looks up `"India Standard Time"` (a Windows name). On Linux, change it to `"Asia/Kolkata"`.
- **Tailwind CSS:** `wwwroot/css/tailwind.css` is already built. Only if you edit `wwwroot/css/input.css`, run `npx tailwindcss -i ./wwwroot/css/input.css -o ./wwwroot/css/tailwind.css` (a `tailwind.config.js` is still missing for theme customization).
- **Mac/Linux users:** instead of Part 1.4, run SQL Server in Docker: `docker run -e "ACCEPT_EULA=Y" -e "MSSQL_SA_PASSWORD=YourStrong!Passw0rd" -p 1433:1433 -d mcr.microsoft.com/mssql/server:2022-latest`, and in Part 4 use `Server=localhost,1433;Database=docsekpn_hellodoctor;User Id=sa;Password=YourStrong!Passw0rd;TrustServerCertificate=True`.
