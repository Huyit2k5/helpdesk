# 🎫 HỆ THỐNG QUẢN LÝ HỖ TRỢ KỸ THUẬT (HELPDESK SYSTEM)

Dự án xây dựng hệ thống Helpdesk doanh nghiệp hiện đại, đa kênh (Omnichannel), hỗ trợ Multi-tenancy và SLA dựa trên nền tảng **ABP Framework v10.6**, **.NET 10**, **PostgreSQL** và **Angular 19 (Standalone)**.

---

## 📑 MỤC LỤC

1. [Tổng Quan Kiến Trúc & Công Nghệ](#-tổng-quan-kiến-trúc--công-nghệ)
2. [Lộ Trình Triển Khai (Roadmap)](#-lộ-trình-triển-khai-roadmap)
3. [Tóm Tắt Các Phần Đã Triển Khai](#-tóm-tắt-các-phần-đã-triển-khai)
   - [Phân Hệ 1: Quản Lý Danh Mục (Master Data)](#31-phân-hệ-1-quản-lý-danh-mục-master-data)
   - [Phân Hệ 2: Quản Lý Sự Vụ Cốt Lõi (Ticket Core)](#32-phân-hệ-2-quản-lý-sự-vụ-cốt-lõi-ticket-core)
4. [Hướng Dẫn Khởi Chạy & Đăng Nhập](#-hướng-dẫn-khởi-chạy--đăng-nhập)
5. [Các Lỗi Đã Xảy Ra & Cách Khắc Phục (Troubleshooting)](#-các-lỗi-đã-xảy-ra--cách-khắc-phục-troubleshooting)

---

## 🏛️ TỔNG QUAN KIẾN TRÚC & CÔNG NGHỆ

- **Backend**: .NET 10.0, C# 13, ABP Framework v10.6.0 (Domain-Driven Design).
- **Database**: PostgreSQL 17 (Docker), Entity Framework Core 10.
- **Frontend**: Angular 19 (Standalone Components), Bootstrap 5, FontAwesome, ABP Angular SDK.
- **Authentication & Security**: OpenIddict, JWT Bearer Token, RBAC Permissions.

---

## 🗺️ LỘ TRÌNH TRIỂN KHAI (ROADMAP)

| STT | Phân Hệ | Chức Năng Chính | Trạng Thái |
|:---:|:---|:---|:---:|
| **1** | **Quản Lý Danh Mục (Master Data)** | Quản lý Categories, Priorities, Departments, Statuses, Sources, Canned Responses | **Hoàn thành (Backend + Frontend)** |
| **2** | **Quản Lý Ticket (Ticket Core)** | Vòng đời vé, sinh mã tự động, chuyển trạng thái, phân công, bình luận, Kanban, Timeline | **Hoàn thành (Backend + Frontend)** |
| **3** | **Quản Lý Cam Kết Dịch Vụ (SLA Engine)** | Quy tắc tính SLA theo giờ làm việc, cảnh báo vi phạm hạn xử lý | Sắp triển khai |
| **4** | **Báo Cáo & Thống Kê (Dashboard)** | Biểu đồ trực quan, KPI xử lý sự vụ theo nhân viên và phòng ban | Kế hoạch |
| **5** | **Hệ Thống Thông Báo (Notifications)** | Thông báo thời gian thực qua SignalR & Email | Kế hoạch |
| **6** | **Cơ Sở Tri Thức (Knowledge Base - FAQ)**| Thư viện bài viết tự phục vụ người dùng | Kế hoạch |
| **7** | **Cổng Khách Hàng (Customer Portal)** | Portal riêng để người dùng tạo và tra cứu tiến độ vé | Kế hoạch |

---

## 🚀 TÓM TẮT CÁC PHẦN ĐÃ TRIỂN KHAI

### 3.1. Phân Hệ 1: Quản Lý Danh Mục (Master Data)

Đã hoàn thành toàn diện 6 mô-đun danh mục cấu hình hệ thống:

1. **Category (Danh mục sự cố)**: Phân loại đa cấp cha-con, quản lý mã, tên, mô tả.
2. **Priority (Mức độ ưu tiên)**: Quản lý mức khẩn cấp, gắn mã màu hiển thị, định nghĩa thời gian phản hồi và xử lý SLA tiêu chuẩn (`Low`, `Medium`, `High`, `Critical`).
3. **Department (Phòng ban xử lý)**: Quản lý phòng ban kỹ thuật, hỗ trợ viên, người quản lý phòng ban.
4. **TicketStatus (Trạng thái vé)**: Phân nhóm trạng thái (`Open`, `InProgress`, `Closed`), đánh dấu trạng thái mặc định (`isDefault`) và kết thúc (`isFinal`).
5. **TicketSource (Kênh tiếp nhận)**: Kênh phát sinh yêu cầu (`Email`, `Phone`, `Web Portal`, `Chat`, `Walk-in`).
6. **CannedResponse (Mẫu phản hồi nhanh)**: Thư viện soạn sẵn câu trả lời mẫu cho hỗ trợ viên, gắn theo danh mục, phạm vi công khai hoặc nội bộ.

- **Backend**: Entity, Domain Manager kiểm tra trùng mã `Code`, EF Core mapping, Migration `Added_MasterData_Entities`, Seeder tự động nạp dữ liệu chuẩn, 24 quyền hạn `Helpdesk.*`.
- **Frontend Angular**: 6 màn hình CRUD danh mục độc lập tại `/master-data/*`, hỗ trợ tìm kiếm, phân trang, Modal Reactive Form, xác nhận xóa.

---

### 3.2. Phân Hệ 2: Quản Lý Sự Vụ Cốt Lõi (Ticket Core)

Trọng tâm vận hành của Helpdesk, xử lý toàn bộ vòng đời của yêu cầu hỗ trợ:

#### A. Backend (.NET 10 & EF Core)
- **Aggregate Root `Ticket`**: Quản lý toàn bộ thông tin sự vụ, mã vé `TicketNumber`, tiêu đề, mô tả, thông tin người yêu cầu (Tên, Email, Điện thoại), liên kết Danh mục, Ưu tiên, Trạng thái, Kênh tiếp nhận, Phòng ban và Người phụ trách (`AssigneeId`), hạn SLA (`DueDate`), thời điểm giải quyết (`ResolvedAt`), đóng vé (`ClosedAt`).
- **Domain Service `TicketManager`**: Tự động sinh mã sự vụ chuẩn định dạng duy nhất `TK-yyyyMMdd-XXXX` (ví dụ: `TK-20260908-0001`).
- **Entity `TicketComment`**: Hỗ trợ trao đổi công khai với khách hàng hoặc ghi chú nội bộ kỹ thuật (`IsInternal = true`).
- **Entity `TicketActivity` & Enum `TicketActivityType`**: Tự động ghi nhật ký kiểm toán (Audit Trail) khi tạo vé, đổi trạng thái, gán người xử lý, thêm bình luận...
- **Application Service `TicketAppService`**: Triển khai đầy đủ CRUD, đa tiêu chí lọc linh hoạt (từ khóa, status, priority, category, department, assignee), các API chuyên biệt: `AssignAsync`, `ChangeStatusAsync`, `AddCommentAsync`, `GetActivitiesAsync`.
- **CSDL**: Đã chạy Migration `20260908042328_Add_Tickets_Module` vào PostgreSQL, tạo sẵn các Index tối ưu hóa truy vấn (`IX_AppTickets_TicketNumber`, `IX_AppTickets_StatusId`, `IX_AppTickets_AssigneeId`, `IX_AppTickets_CreationTime`).
- **Phân quyền**: Bổ sung nhóm quyền `Helpdesk.Tickets` (`Create`, `Edit`, `Delete`, `Assign`, `ChangeStatus`, `AddComment`) và đã cấp toàn quyền cho vai trò `admin`.

#### B. Frontend (Angular 19 Standalone)
- **Chế độ xem kép (Dual View)**:
  - **Dạng Bảng (Table View)**: Hiển thị danh sách vé đầy đủ cột nghiệp vụ, nhãn trạng thái và mức ưu tiên có màu sắc trực quan, phân trang `ngb-pagination`.
  - **Bảng Kanban (Kanban Board View)**: Nhóm vé theo từng cột trạng thái trực quan, thẻ vé hiển thị người gửi, ưu tiên, người xử lý và số lượng bình luận.
- **Thanh công cụ lọc đa tiêu chí**: Tìm kiếm theo từ khóa/số vé, dropdown lọc theo Trạng thái, Mức ưu tiên, Danh mục, Nhân viên xử lý; nút đặt lại bộ lọc.
- **Modal tạo mới sự vụ (`app-ticket-create-modal`)**: Form nhập liệu rõ ràng, chia 3 nhóm thông tin (Thông tin sự vụ, Khách hàng, Phân công), tự động nạp danh mục và trạng thái mặc định.
- **Trang chi tiết sự vụ (`/tickets/:id`)**:
  - Header hiển thị mã vé `#TK-xxx`, nhãn trạng thái và ưu tiên, nút thao tác nhanh (Đổi trạng thái, Phân công, Xóa).
  - Khung soạn thảo phản hồi 2 chế độ: **Phản Hồi Khách Hàng (Public Reply)** vs **Ghi Chú Nội Bộ (Internal Note - nhãn vàng bảo mật)**.
  - Tích hợp chèn nhanh nội dung từ danh sách **Câu trả lời mẫu (Canned Responses)**.
  - **Dòng thời gian (Timeline & Audit Trail)**: Hợp nhất lịch sử trao đổi của khách hàng, ghi chú kỹ thuật và nhật ký hành động hệ thống.
  - Sidebar hiển thị đầy đủ thông tin người gửi, phòng ban, hạn SLA và các mốc thời gian giải quyết/đóng sự vụ.

---

## 🛠️ HƯỚNG DẪN KHỞI CHẠY & ĐĂNG NHẬP

### 1. Khởi động CSDL PostgreSQL (Docker)
```powershell
docker start postgres
```

### 2. Khởi động Backend API
```powershell
cd d:\helpdesk\Helpdesk
dotnet run --project src/Helpdesk.HttpApi.Host
```
- API & Swagger UI: `https://localhost:44346/swagger`

### 3. Khởi động Frontend Angular
```powershell
cd d:\helpdesk\Helpdesk\angular
npm start
```
- Giao diện ứng dụng: `http://localhost:4200`

### 4. Tài khoản quản trị mặc định
- **Tài khoản**: `admin`
- **Mật khẩu**: `Huy123@`
- **Các đường dẫn chính**:
  - Quản lý sự vụ (Tickets): `http://localhost:4200/tickets`
  - Quản lý danh mục (Master Data): `http://localhost:4200/master-data`

---

## ⚠️ CÁC LỖI ĐÃ XẢY RA & CÁCH KHẮC PHỤC (TROUBLESHOOTING)

Trong quá trình phát triển và tích hợp hệ thống, các lỗi phổ biến sau đã xuất hiện và được xử lý triệt để:

### 1. Không nhìn thấy các bảng dữ liệu trong DBeaver
- **Hiện tượng**: Kết nối PostgreSQL trong DBeaver thành công nhưng khi mở mục `Schemas -> public -> Tables` thì danh sách bảng trống trơn.
- **Nguyên nhân**: Mặc định DBeaver kết nối vào database hệ thống có tên là `postgres` (database này rỗng). Trong khi toàn bộ 51 bảng của dự án Helpdesk nằm ở database **`appdb`**.
- **Cách khắc phục**:
  - **Cách 1**: Chuột phải vào kết nối trong DBeaver $\rightarrow$ **Edit Connection** $\rightarrow$ Tại ô **Database**, đổi từ `postgres` thành **`appdb`** $\rightarrow$ Nhấn **OK**.
  - **Cách 2**: Trong **Edit Connection** $\rightarrow$ chọn tab **PostgreSQL** $\rightarrow$ tích chọn **Show all databases** $\rightarrow$ mở rộng nhánh `appdb -> Schemas -> public -> Tables`.
  - *(Lưu ý: Nhấn **F5** để Refresh nếu DBeaver đang lưu cache cũ)*.

---

### 2. Lỗi 403 Forbidden khi truy cập trang Tickets trên giao diện Angular
- **Hiện tượng**: Menu `Tickets` đã xuất hiện trên thanh điều hướng nhưng khi bấm vào bị chặn với thông báo lỗi 403 Forbidden.
- **Nguyên nhân**:
  1. Khi bổ sung nhóm quyền mới `Helpdesk.Tickets` trong code, database chưa tự động cấp các quyền này cho vai trò `admin` trong bảng `AbpPermissionGrants`.
  2. Backend API đang chạy vẫn giữ bộ nhớ đệm (Permission Cache) từ lúc khởi động, chưa nhận diện quyền mới.
  3. Trên frontend, nếu một API phụ trợ (như nạp danh sách user `IdentityUserService.getList()`) bị từ chối quyền, cơ chế Interceptor mặc định của ABP sẽ chuyển hướng toàn bộ trang sang 403.
- **Cách khắc phục**:
  - Cấp toàn bộ 7 quyền `Helpdesk.Tickets.*` cho vai trò `admin` vào bảng `AbpPermissionGrants` trong PostgreSQL:
    ```sql
    INSERT INTO "AbpPermissionGrants" ("Id", "Name", "ProviderName", "ProviderKey") VALUES
    (gen_random_uuid(), 'Helpdesk.Tickets', 'R', 'admin'),
    (gen_random_uuid(), 'Helpdesk.Tickets.Create', 'R', 'admin'),
    (gen_random_uuid(), 'Helpdesk.Tickets.Edit', 'R', 'admin'),
    (gen_random_uuid(), 'Helpdesk.Tickets.Delete', 'R', 'admin'),
    (gen_random_uuid(), 'Helpdesk.Tickets.Assign', 'R', 'admin'),
    (gen_random_uuid(), 'Helpdesk.Tickets.ChangeStatus', 'R', 'admin'),
    (gen_random_uuid(), 'Helpdesk.Tickets.AddComment', 'R', 'admin')
    ON CONFLICT DO NOTHING;
    ```
  - Khởi động lại Backend API (`Helpdesk.HttpApi.Host`) để xóa cache quyền cũ và nạp lại từ database.
  - Trên Angular: Bỏ cấu hình `requiredPolicy` trên thanh điều hướng top-level menu để menu luôn hiển thị, đồng thời bọc các lệnh gọi API phụ trong `tickets.component.ts` bằng `catchError(() => of(...))` từ RxJS.

---

### 3. Khóa file DLL khi chạy `dotnet build` hoặc `dotnet ef migrations` (Lỗi MSB3027 / MSB3021)
- **Hiện tượng**: Báo lỗi không thể sao chép hoặc ghi đè file `*.dll` trong thư mục `bin\Debug\net10.0\` vì file đang được sử dụng bởi tiến trình khác (*"The process cannot access the file because it is being used by another process"*).
- **Nguyên nhân**: Tiến trình Backend `Helpdesk.HttpApi.Host` đang chạy ngầm và giữ khóa (file lock) các DLL của solution.
- **Cách khắc phục**:
  - Tắt tiến trình Backend API trước khi biên dịch hoặc tạo migration mới.
  - Sau khi build / migrate xong xuôi mới chạy lại `dotnet run --project src/Helpdesk.HttpApi.Host`.

---

### 4. Xung đột Async LINQ trong tầng Application (`ToListAsync` vs `AsyncExecuter`)
- **Hiện tượng**: Gọi trực tiếp `await query.ToListAsync()` hoặc `await query.CountAsync()` trong tầng Application gây lỗi biên dịch hoặc xung đột thư viện EF Core.
- **Nguyên nhân**: Kiến trúc chuẩn của ABP Framework khuyến nghị tầng Application không phụ thuộc trực tiếp vào package `Microsoft.EntityFrameworkCore` để đảm bảo tính độc lập với ORM.
- **Cách khắc phục**:
  - Sử dụng đối tượng `AsyncExecuter` được tích hợp sẵn trong `ApplicationService` của ABP:
    ```csharp
    var totalCount = await AsyncExecuter.CountAsync(query);
    var items = await AsyncExecuter.ToListAsync(query);
    ```

---

### 5. Lỗi đường dẫn import và sai lệch trường DTO khi chạy `abp generate-proxy -t ng`
- **Hiện tượng**: Sau khi chạy lệnh sinh mã proxy Angular, một số component cũ bị lỗi compile: `Cannot find module ...` hoặc báo lỗi thuộc tính không tồn tại trên DTO (ví dụ `item.icon`, `item.shortcut`).
- **Nguyên nhân**: ABP CLI phiên bản mới tự động phân tách proxy thành các thư mục con theo từng namespace backend (`proxy/tickets/`, `proxy/categories/`, `proxy/ticket-statuses/`...), đồng thời một số trường thử nghiệm ở giao diện không có trong thực thể backend DTO.
- **Cách khắc phục**:
  - Sửa lại đường dẫn import tương đối trỏ chính xác vào thư mục con (`../../proxy/categories/category.service`).
  - Đồng bộ lại các trường trong Reactive Form và HTML template đúng với cấu trúc `models.ts` được sinh ra bởi ABP Proxy.

---

### 6. Lỗi TypeScript Strict Nullability (`TS2322: undefined is not assignable to string`)
- **Hiện tượng**: Lệnh `ng build` bị lỗi do Angular 19 bật cấu hình kiểm tra kiểu nghiêm ngặt (Strict Type Checking): `Type 'string | undefined' is not assignable to type 'string'`.
- **Nguyên nhân**: Các trường trong ABP EntityDto sinh ra dạng optional (`title?: string`, `name?: string`, `creationTime?: string | Date`). Khi truyền trực tiếp vào các hàm yêu cầu kiểu `string` (như tham số localization `messageLocalizationParams`), TypeScript sẽ báo lỗi.
- **Cách khắc phục**:
  - Bổ sung giá trị dự phòng (fallback): `item.name ?? ''`, `item.title ?? ''`.
  - Dùng non-null assertion `item.id!` khi đã chắc chắn dữ liệu tồn tại.
  - Mở rộng kiểu dữ liệu interface (ví dụ `date: string | Date`) để tương thích với cả kiểu chuỗi lẫn đối tượng Date của ABP.

