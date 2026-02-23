# Deploy TechStore API – Cho mọi người cùng dùng

Hướng dẫn deploy **Database** và **Backend API** lên một máy chủ/cloud để cả nhóm (hoặc user) dùng chung một URL API.

---

## Tổng quan

1. **Database**: Tạo database SQL Server (Azure SQL hoặc SQL Server trên VPS), chạy script tạo bảng + cột PublicId.
2. **Backend**: Publish API lên hosting (Azure App Service, Railway, Render, hoặc VPS), cấu hình chuỗi kết nối + JWT + CORS.
3. **Kết quả**: Mọi người dùng chung một URL (vd: `https://techstore-api.azurewebsites.net`) cho Postman / app Mobile.

---

## Bước 1: Chuẩn bị Database

### Cách A: Azure SQL Database (đề xuất cho nhóm)

1. Vào [Azure Portal](https://portal.azure.com) → **Create a resource** → **Azure SQL** (hoặc **SQL Database**).
2. Tạo **Server** (vd: `techstore-sql.database.windows.net`) và **Database** (vd: `TechStore`).
3. Trong **Networking** của server: bật **Allow Azure services and public access** (hoặc thêm IP của máy deploy BE) để API kết nối được.
4. Lấy **connection string**:
   - Vào database → **Connection strings** → copy chuỗi **ADO.NET**, thay `{your_password}` bằng mật khẩu đã đặt.
   - Dạng: `Server=tcp:techstore-sql.database.windows.net,1433;Initial Catalog=TechStore;User ID=admin;Password=xxx;Encrypt=True;TrustServerCertificate=False;`
5. Tạo bảng và schema:
   - Nếu project có **EF Core Migrations**: trên máy dev chạy  
     `dotnet ef database update --project TechStore.Infrastructure --startup-project TechStore.API`  
     (cần cấu hình connection string trỏ tới Azure SQL trước).
   - Nếu tạo tay: chạy script SQL tạo bảng (Users, Categories, Products, Orders, OrderItems, ProductSpecs) tương ứng model trong code.
6. Chạy script **PublicId**: mở file `Scripts/AddPublicId_UpdateDatabase.sql`, đổi `USE TechStore` nếu cần (đúng tên database), chạy toàn bộ script trên Azure SQL.

### Cách B: SQL Server trên VPS / máy chủ

1. Cài **SQL Server** (Windows hoặc Linux) trên VPS.
2. Tạo database `TechStore`, tạo user (vd: `techstore_app`) và cấp quyền.
3. Connection string dạng:  
   `Server=IP_hoặc_tên_máy,1433;Database=TechStore;User Id=techstore_app;Password=xxx;TrustServerCertificate=True;`
4. Tạo bảng (EF migrations hoặc script tay), sau đó chạy `Scripts/AddPublicId_UpdateDatabase.sql`.

---

## Bước 2: Deploy Backend API

API cần **connection string** trỏ tới DB ở bước 1 và **JWT secret** (cùng một secret cho mọi client).

### Cách A: Azure App Service

1. Trong Azure Portal: **Create a resource** → **Web App**.
2. Chọn **Runtime**: .NET 10 (hoặc .NET 8 nếu đổi project sang net8.0), OS Windows hoặc Linux.
3. Sau khi tạo, vào **Deployment Center**:
   - Chọn **GitHub** / **Git** hoặc **Zip Deploy**: push code rồi build trên Azure, hoặc build local rồi deploy folder `publish`.
4. **Configuration** (Application settings) – thêm biến môi trường:
   - `ConnectionStrings__DefaultConnection` = chuỗi kết nối DB (bước 1).
   - `JwtSettings__SecretKey` = chuỗi bí mật dài (vd: 32+ ký tự ngẫu nhiên).
   - `JwtSettings__Issuer` = `TechStoreAPI`, `JwtSettings__Audience` = `TechStoreClient`.
   - `CORS__AllowedOrigins` = danh sách origin cách nhau dấu phẩy (vd: `https://your-app.vercel.app,https://yourapp.com`). Để trống = cho phép mọi origin (chỉ nên dùng khi demo).
5. **Save** và restart app. URL API: `https://<tên-app>.azurewebsites.net`.

### Cách B: Railway / Render (free tier)

1. **Railway**: Tạo project → **New** → **GitHub repo** (chọn repo TechStore). Root folder đặt đúng (chứa `TechStore.API.csproj`). Thêm **SQL Server** service (hoặc dùng **Azure SQL** / DB bên ngoài). Trong **Variables** thêm `ConnectionStrings__DefaultConnection`, `JwtSettings__SecretKey`, v.v. Giống Azure.
2. **Render**: **New** → **Web Service** → connect repo. Build: `dotnet publish TechStore.API -c Release -o out`. Start: `./out/TechStore.API`. Thêm **Environment** biến tương tự. Render không có SQL Server sẵn → dùng Azure SQL hoặc DB bên ngoài.

### Cách C: VPS (Windows / Linux)

1. Trên VPS cài **.NET 10 Runtime** (hoặc 8 nếu đổi target).
2. Build trên máy dev:  
   `dotnet publish TechStore.API -c Release -o ./publish`
3. Copy folder `publish` lên VPS (FTP, RDP, hoặc SCP).
4. Tạo file `appsettings.Production.json` trong folder chạy (hoặc dùng biến môi trường):
   - `ConnectionStrings:DefaultConnection`
   - `JwtSettings:SecretKey`, Issuer, Audience
   - `CORS:AllowedOrigins`
5. Chạy: `dotnet TechStore.API.dll` (trong thư mục chứa DLL). Có thể dùng **Nginx** / **IIS** làm reverse proxy, HTTPS (Let’s Encrypt).

---

## Bước 3: Cấu hình Production (chung)

- **ConnectionStrings:DefaultConnection**: Chuỗi kết nối tới DB đã tạo (Azure SQL hoặc SQL Server VPS).
- **JwtSettings:SecretKey**: Một chuỗi bí mật cố định (≥ 32 ký tự), **không** commit lên Git. Có thể dùng biến môi trường `JWT_SECRET` (code đã hỗ trợ).
- **CORS:AllowedOrigins**: Danh sách origin frontend/app được gọi API (cách nhau dấu phẩy). Ví dụ: `https://myapp.vercel.app`.
- **EmailSettings**: Nếu cần gửi mail thật, điền SmtpHost, SmtpUser, SmtpPassword (qua biến môi trường hoặc appsettings.Production.json, không commit mật khẩu).

---

## Sau khi deploy

- **URL API** (vd): `https://techstore-api.azurewebsites.net`
- **Postman**: Đổi biến `baseUrl` trong collection thành URL trên, mọi người dùng chung.
- **Mobile (Flutter)**: Trong app đổi base URL sang URL deploy (env hoặc config).
- **Swagger**: Nếu bật trong Production: `https://<url>/swagger`.

---

## Checklist nhanh

- [ ] DB đã tạo (Azure SQL hoặc SQL Server), đã chạy script bảng + `AddPublicId_UpdateDatabase.sql`.
- [ ] API đã publish và deploy lên Azure / Railway / Render / VPS.
- [ ] Đã cấu hình `ConnectionStrings__DefaultConnection`, `JwtSettings__SecretKey`, `CORS__AllowedOrigins`.
- [ ] Test: Postman gọi `https://<url>/api/categories` (GET) và Login → nhận token.
- [ ] Gửi URL API + hướng dẫn đổi baseUrl cho nhóm.

Nếu bạn chọn **một nền cụ thể** (chỉ Azure, hoặc chỉ VPS), có thể thu gọn lại các bước cho đúng với lựa chọn đó.

---

## Đẩy DB local lên Azure SQL (TechStoreDB)

Bạn đang có database **TechStore** trên SQL Server local (có bảng, có data). Cách đẩy lên **Azure SQL** (database **TechStoreDB** đã tạo sẵn).

**Bước tiếp theo ngay (khi đã kết nối Azure như bạn):**

| Bước | Việc cần làm |
|------|----------------|
| 1 | Mở **tab/cửa sổ SSMS khác**, kết nối tới **SQL Server local** (máy bạn, database **TechStore**). |
| 2 | Chuột phải **TechStore** (local) → **Tasks** → **Generate Scripts** → chọn tất cả **Tables** → **Advanced** → **Types of data to script** = **Schema and data** → Save file (vd: `TechStore_Export.sql`). |
| 3 | Quay lại cửa sổ đang kết nối **Azure** (TechStoreDB). **File** → **Open** → mở file `TechStore_Export.sql`. Trong file tìm `USE [TechStore]` đổi thành `USE [TechStoreDB]`. |
| 4 | Chọn database **TechStoreDB** ở dropdown phía trên (hoặc thêm dòng `USE [TechStoreDB];` đầu script). Bấm **Execute** (F5). |
| 5 | Chạy tiếp script **PublicId**: mở `Scripts/AddPublicId_UpdateDatabase.sql`, đổi `USE TechStore` thành `USE TechStoreDB`, Execute trên Azure. |
| 6 | Refresh **Tables** trong TechStoreDB (Azure) → sẽ thấy bảng và data. |

### Cách 1: SSMS – Generate Scripts (schema + data) – chi tiết

1. Mở **SQL Server Management Studio**, kết nối tới **local SQL Server** (database TechStore).
2. Chuột phải vào database **TechStore** → **Tasks** → **Generate Scripts**.
3. Chọn **Select specific database objects** → tick **Tables** (chọn hết: Users, Categories, Products, ProductSpecs, Orders, OrderItems).
4. Bấm **Advanced**:
   - **Types of data to script** → chọn **Schema and data** (để có cả INSERT dữ liệu).
   - **Script DROP and CREATE** → chọn **Script CREATE** (hoặc tùy bạn).
5. **Save to file** → chọn đường dẫn (vd: `TechStore_Export.sql`), **Next** → **Finish**.
6. Mở **SSMS**, kết nối tới **Azure SQL**:
   - Server: `techstore-prm393.database.windows.net`
   - Authentication: **SQL Server Authentication**, Login: `prm393`, Password: `Passmon393`
   - Chọn database **TechStoreDB** (hoặc master rồi chọn TechStoreDB).
7. Trong script vừa export, **đổi tên database** nếu cần: tìm `USE [TechStore]` và đổi thành `USE [TechStoreDB]` (hoặc đảm bảo đang chạy trong context TechStoreDB).
8. **Mở file script** → **Execute** (F5). Nếu bảng đã tồn tại trên Azure (trống), có thể cần xóa bảng hoặc chỉ chạy phần **INSERT** (hoặc chạy từ đầu trên database trống).
9. Nếu trên Azure **chưa có cột PublicId**, chạy tiếp file `Scripts/AddPublicId_UpdateDatabase.sql`: mở file, đổi `USE TechStore` thành `USE TechStoreDB`, rồi Execute trên Azure SQL.

**Lưu ý:** Nếu Azure đã có bảng (tạo tay hoặc từ EF) nhưng **chưa có dữ liệu**, có thể chỉ cần generate script **Data only** (INSERT) và chạy trên TechStoreDB. Thứ tự bảng: Users → Categories → Products → ProductSpecs → Orders → OrderItems (đúng foreign key).

### Cách 2: Export / Import Data-tier Application (Bacpac)

1. **Export từ local (SSMS):**
   - Chuột phải database **TechStore** → **Tasks** → **Export Data-tier Application**.
   - Chọn đường dẫn lưu file **.bacpac** (vd: `TechStore.bacpac`).
   - Hoàn tất export.

2. **Import lên Azure:**
   - Vào **Azure Portal** → **SQL databases** → **TechStoreDB** (hoặc server `techstore-prm393`).
   - Có thể dùng **Azure Data Studio** hoặc **SqlPackage**:
     - **SqlPackage** (command line):  
       `SqlPackage /Action:Import /SourceFile:"C:\path\TechStore.bacpac" /TargetConnectionString:"Server=tcp:techstore-prm393.database.windows.net,1433;Database=TechStoreDB;User ID=prm393;Password=Passmon393;Encrypt=True;TrustServerCertificate=False;"`
   - Hoặc trong **Azure Portal** → chọn server → **Import database** (nếu có), chọn file .bacpac và database đích **TechStoreDB**.

3. Sau khi import xong, kiểm tra bảng và dữ liệu. Nếu thiếu cột **PublicId**, chạy `Scripts/AddPublicId_UpdateDatabase.sql` trên **TechStoreDB** (đổi `USE TechStore` thành `USE TechStoreDB`).

### Sau khi đẩy xong

- API (local hoặc App Service) đổi connection string trỏ tới Azure:
  - `Server=tcp:techstore-prm393.database.windows.net,1433;Initial Catalog=TechStoreDB;...User ID=prm393;Password=Passmon393;...`
- Test: mở Postman, gọi API (login, get categories, get products) để xác nhận dữ liệu từ Azure.

---

## Azure SQL – TechStore PRM393 (đã tạo sẵn)

- **Server:** `techstore-prm393.database.windows.net`
- **Database:** `TechStoreDB`
- **User ID:** `prm393`
- **Password:** `Passmon393`

**Connection string (dùng cho API / App Service / biến môi trường):**

```
Server=tcp:techstore-prm393.database.windows.net,1433;Initial Catalog=TechStoreDB;Persist Security Info=False;User ID=prm393;Password=Passmon393;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;
```

Trong **Azure App Service** hoặc **biến môi trường**, đặt:

- `ConnectionStrings__DefaultConnection` = chuỗi trên (copy nguyên).