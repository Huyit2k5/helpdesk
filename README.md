# 🎫 HỆ THỐNG QUẢN LÝ HỖ TRỢ KỸ THUẬT (HELPDESK SYSTEM)

Dự án xây dựng hệ thống Helpdesk doanh nghiệp hiện đại, đa kênh (Omnichannel), hỗ trợ Multi-tenancy và SLA dựa trên nền tảng **ABP Framework v10.6**, **.NET 10**, **PostgreSQL** và **Angular 19 (Standalone)**.

---

## 📑 MỤC LỤC

1. [Tổng Quan Kiến Trúc & Công Nghệ](#-tổng-quan-kiến-trúc--công-nghệ)
2. [Lộ Trình Triển Khai (Roadmap)](#-lộ-trình-triển-khai-roadmap)
11: 3. [Tóm Tắt Các Phần Đã Triển Khai](#-tóm-tắt-các-phần-đã-triển-khai)
12:    - [Phân Hệ 1: Quản Lý Danh Mục (Master Data)](#31-phân-hệ-1-quản-lý-danh-mục-master-data)
13:    - [Phân Hệ 2: Quản Lý Sự Vụ Cốt Lõi (Ticket Core)](#32-phân-hệ-2-quản-lý-sự-vụ-cốt-lõi-ticket-core)
14:    - [Phân Hệ 3: Quản Lý Cam Kết Dịch Vụ (SLA Engine)](#33-phân-hệ-3-quản-lý-cam-kết-dịch-vụ-sla-engine)
15:    - [Phân Hệ 4: Báo Cáo & Thống Kê (Dashboard)](#34-phân-hệ-4-báo-cáo--thống-kê-dashboard)
16:    - [Dữ Liệu Khởi Tạo Chuẩn (Data Seeding)](#35-dữ-liệu-khởi-tạo-chuẩn-data-seeding)
17: 4. [Hướng Dẫn Khởi Chạy & Đăng Nhập](#-hướng-dẫn-khởi-chạy--đăng-nhập)
18: 5. [Các Lỗi Đã Xảy Ra & Cách Khắc Phục (Troubleshooting)](#-các-lỗi-đã-xảy-ra--cách-khắc-phục-troubleshooting)
19: 
20: ---
21: 
22: ## 🏛️ TỔNG QUAN KIẾN TRÚC & CÔNG NGHỆ
23: 
24: - **Backend**: .NET 10.0, C# 13, ABP Framework v10.6.0 (Domain-Driven Design).
25: - **Database**: PostgreSQL 17 (Docker), Entity Framework Core 10.
26: - **Frontend**: Angular 19 (Standalone Components), Bootstrap 5, FontAwesome, ABP Angular SDK, Chart.js.
27: - **Authentication & Security**: OpenIddict, JWT Bearer Token, RBAC Permissions.
28: 
29: ---
30: 
31: ## 🗺️ LỘ TRÌNH TRIỂN KHAI (ROADMAP)
32: 
33: | STT | Phân Hệ | Chức Năng Chính | Trạng Thái |
34: |:---:|:---|:---|:---:|
35: | **1** | **Quản Lý Danh Mục (Master Data)** | Quản lý Categories, Priorities, Departments, Statuses, Sources, Canned Responses | **Hoàn thành (Backend + Frontend)** |
36: | **2** | **Quản Lý Ticket (Ticket Core)** | Vòng đời vé, sinh mã tự động, chuyển trạng thái, phân công, bình luận, Kanban, Timeline | **Hoàn thành (Backend + Frontend)** |
37: | **3** | **Quản Lý Cam Kết Dịch Vụ (SLA Engine)** | Quy tắc tính SLA theo giờ làm việc & ngày lễ, cảnh báo vi phạm hạn xử lý, báo cáo tuân thủ | **Hoàn thành (Backend + Frontend)** |
38: | **4** | **Báo Cáo & Thống Kê (Dashboard)** | Biểu đồ trực quan (Chart.js), KPI xử lý sự vụ theo nhân viên và phòng ban, Auto-refresh | **Hoàn thành (Backend + Frontend)** |
39: | **5** | **Hệ Thống Thông Báo (Notifications)** | Thông báo thời gian thực qua SignalR & Email | Kế hoạch |
40: | **6** | **Cơ Sở Tri Thức (Knowledge Base - FAQ)**| Thư viện bài viết tự phục vụ người dùng | Kế hoạch |
41: | **7** | **Cổng Khách Hàng (Customer Portal)** | Portal riêng để người dùng tạo và tra cứu tiến độ vé | Kế hoạch |

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

### 3.3. Phân Hệ 3: Quản Lý Cam Kết Dịch Vụ (SLA Engine)

Đảm bảo chất lượng cam kết dịch vụ khách hàng với các quy tắc tự động hóa:

1. **Chính sách SLA (`SlaPolicy` & `SlaRule`)**:
   - Cấu hình linh hoạt thời gian cam kết phản hồi lần đầu (`ResponseTimeMinutes`) và thời gian giải quyết sự cố (`ResolutionTimeMinutes`) dựa trên Mức độ ưu tiên (`Priority`) và Danh mục (`Category`).
   - Thiết lập chính sách mặc định (`IsDefault`) áp dụng cho toàn hệ thống.
2. **Khung giờ làm việc (`BusinessHour`) & Ngày lễ (`Holiday`)**:
   - Định nghĩa lịch làm việc theo từng ngày trong tuần (mặc định Thứ Hai - Thứ Sáu, 08:30 - 17:30).
   - Quản lý danh mục ngày lễ, tết nghỉ định kỳ tự động loại trừ khỏi thời gian tính SLA.
3. **Bộ máy tính SLA (`SlaManager`)**:
   - Tự động tính hạn chót phản hồi (`FirstResponseDueDate`) và hạn chót giải quyết (`DueDate`) dựa trên lịch làm việc thực tế.
   - Cơ chế phát hiện vi phạm SLA (`IsFirstResponseBreached`, `IsResolutionBreached`).
4. **Giao diện quản trị SLA**:
   - Màn hình cấu hình chính sách SLA (`/sla/policies`).
   - Màn hình cấu hình giờ làm việc & ngày lễ (`/sla/business-hours`).
   - Báo cáo tuân thủ SLA (`/sla/compliance`): Thống kê tỷ lệ phản hồi đúng hạn, tỷ lệ giải quyết đúng hạn, danh sách sự vụ vi phạm SLA.

---

### 3.4. Phân Hệ 4: Báo Cáo & Thống Kê (Dashboard)

Cung cấp góc nhìn toàn diện, tức thời về hiệu suất hỗ trợ và khối lượng công việc:

1. **Thẻ chỉ số trọng yếu (KPI Stat Cards)**:
   - Tổng số sự vụ, sự vụ đang mở (`Open`), đang xử lý (`InProgress`), đã giải quyết/đóng (`Closed`).
   - Sự vụ mới tiếp nhận trong ngày, sự vụ đã xử lý trong ngày.
   - Tỷ lệ tuân thủ phản hồi lần đầu (%), tỷ lệ tuân thủ giải quyết sự vụ (%).
   - Cảnh báo vi phạm SLA và số sự vụ đã quá hạn xử lý (`Overdue`).
2. **Biểu đồ trực quan hóa dữ liệu (Chart.js)**:
   - **Biểu đồ xu hướng (Trend Chart)**: Theo dõi số lượng sự vụ tạo mới, giải quyết và đóng theo thời gian (chu kỳ 7, 14, 30, 90 ngày).
   - **Biểu đồ cơ cấu danh mục (Category Distribution Chart)**: Phân bổ tỷ lệ % sự vụ theo từng loại sự cố (Hardware, Software, Network, Account...).
3. **Hiệu suất nhân viên & Nhật ký hoạt động**:
   - **Bảng xếp hạng nhân viên (Agent Performance)**: Số vé được phân công, số vé đã giải quyết, thời gian giải quyết trung bình, tỷ lệ tuân thủ SLA của từng hỗ trợ viên.
   - **Dòng nhật ký hoạt động gần đây (Recent Activities)**: Giám sát thời gian thực các thao tác tạo vé, đổi trạng thái, phân công trong hệ thống.
4. **Tự động làm mới (Auto-Refresh)**: Cơ chế cập nhật định kỳ mỗi 30 giây giúp dashboard luôn hiển thị số liệu mới nhất mà không cần tải lại trang.

---

### 3.5. Dữ Liệu Khởi Tạo Chuẩn (Data Seeding)

Hệ thống tích hợp sẵn `HelpdeskDataSeedContributor` tự động nạp dữ liệu mẫu hoàn chỉnh:
- **10 sự vụ mẫu** đa dạng trạng thái, mức ưu tiên, kênh tiếp nhận, hạn SLA chuẩn thực tế.
- **6 danh mục sự cố**, 4 mức độ ưu tiên chuẩn, 7 trạng thái vòng đời vé, 5 kênh tiếp nhận, 2 phòng ban kỹ thuật.
- **Chính sách SLA tiêu chuẩn** và lịch làm việc hành chính chuẩn cùng các ngày lễ lớn trong năm.

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

---

### 7. Lỗi bảng rỗng dù footer báo "Tổng: 10 sự vụ" / "Tổng: 6 bản ghi" (Lệch chuẩn chỉ số trang 0-index vs 1-index)
- **Hiện tượng**:
  - Tại trang Quản lý sự vụ (`/tickets`), Danh mục (`/master-data/categories`), Mức độ ưu tiên (`/master-data/priorities`)... ở footer luôn hiển thị đúng tổng số lượng (ví dụ: *"Tổng: 10 sự vụ"* hoặc *"Tổng: 6 bản ghi"*).
  - Tuy nhiên thân bảng hoàn toàn trống trơn và hiển thị thông báo rỗng: *"Không tìm thấy sự vụ nào phù hợp với điều kiện lọc"* hoặc *"Chưa có danh mục nào"*.
- **Nguyên nhân gốc rễ**:
  1. Service phân trang của ABP Framework (`ListService`) hoạt động theo chuẩn **0-indexed** (`_page = 0` đại diện cho trang đầu tiên; công thức tính vị trí: `skipCount = _page * maxResultCount = 0 * 10 = 0`).
  2. Component phân trang `<ngb-pagination>` của thư viện Angular UI Bootstrap lại hoạt động theo chuẩn **1-indexed** (trang đầu tiên bắt buộc phải là `1`).
  3. Khi gắn binding hai chiều `[(page)]="list.page"`, `<ngb-pagination>` phát hiện giá trị ban đầu là `0` (không hợp lệ với nó), tự động ép thành `1` và kích hoạt sự kiện `pageChange` gán ngược lại `list.page = 1`.
  4. Ngay lập tức `ListService` nhận `page = 1`, tính lại `skipCount = 1 * 10 = 10` và phát query gọi API backend với `SkipCount = 10`.
  5. Vì cơ sở dữ liệu ban đầu chỉ có 10 sự vụ (hoặc 6 danh mục, 4 ưu tiên), backend khi nhận `SkipCount = 10` đã **bỏ qua toàn bộ 10 bản ghi hiện có** và trả về danh sách rỗng (`items: []`).
- **Cách khắc phục**:
  - Chuẩn hóa lại cơ chế binding giữa `0-indexed` (ListService) và `1-indexed` (ngb-pagination) trên toàn bộ các file template HTML:
    ```html
    <ngb-pagination
      [page]="list.page + 1"
      [pageSize]="list.maxResultCount"
      [collectionSize]="totalCount"
      (pageChange)="list.page = $event - 1"
      [maxSize]="5"
      class="mb-0"
    />
    ```
  - Áp dụng đồng bộ cho tất cả các trang: `tickets`, `categories`, `priorities`, `ticket-statuses`, `ticket-sources`, `departments`, `canned-responses`, `sla-policies`, `sla-compliance`.

---

### 8. Lỗi Modal Tạo Yêu Cầu Mới bị cắt mất chân trang (Không thấy nút "Tạo Yêu Cầu" và "Đóng")
- **Hiện tượng**: Bấm nút "Tạo Yêu Cầu" trên trang `/tickets` mở modal nhập thông tin sự vụ, nhưng ở phía dưới cùng không hề có nút bấm "Tạo Yêu Cầu" (Submit) hay nút "Đóng" (Cancel); phần dưới modal bị cắt cụt ngang ô thông tin người gửi.
- **Nguyên nhân**:
  1. Trong cấu trúc modal của Bootstrap 5 kết hợp `modal-dialog-scrollable`, thẻ `<form>` được đặt trực tiếp bên trong `.modal-content` mang class `h-100`.
  2. Do `.modal-content` chứa cả `.modal-header` và `<form>`, chiều cao `<form>` cộng thêm header vượt quá 100% chiều cao của modal container.
  3. Thẻ `.modal-body` bên trong form không được cấu hình `overflow-y: auto` và `min-height: 0`, dẫn đến việc nội dung form giãn dài tự do theo chiều dọc, đẩy phần `.modal-footer` ra ngoài màn hình và bị thuộc tính `overflow: hidden` của modal container cắt bỏ hoàn toàn.
- **Cách khắc phục**:
  - Thiết lập Layout Flexbox chuẩn mực trong `ticket-create-modal.component.html`:
    - Header và Footer dùng class `flex-shrink-0` để luôn giữ cố định ở đầu và đáy dialog.
    - Thẻ `<form>` dùng `class="d-flex flex-column flex-grow-1 overflow-hidden" style="min-height: 0;"`.
    - Thẻ `.modal-body` dùng `class="modal-body p-4 flex-grow-1" style="overflow-y: auto;"`.
  - Kết quả: Khi nội dung form dài, chỉ có thân modal cuộn nội bộ; hai nút "Đóng" và "Tạo Yêu Cầu" luôn ghim cố định ở đáy modal, hiển thị rõ ràng và tiện thao tác.

---

### 9. Lỗi ô chọn Danh mục không hiển thị giá trị mặc định khi mở Modal Tạo Sự Vụ
- **Hiện tượng**: Mở modal Tạo Yêu Cầu Mới, các trường Mức độ ưu tiên (`Low`), Trạng thái (`New`), Kênh tiếp nhận (`Walk-in`) đều được chọn sẵn nhưng ô Danh mục lại hiển thị `-- Chọn danh mục --`.
- **Nguyên nhân**:
  1. Trong `ticket-create-modal.component.ts`, hàm `loadLookups()` chỉ gán giá trị mặc định cho priority, status, source nhưng bỏ quên `categoryId`.
  2. Khi người dùng đóng modal và mở lại (`isOpen = true`), do component không bị destroy nên form vẫn lưu trạng thái cũ mà không được reset và tái thiết lập giá trị mặc định.
- **Cách khắc phục**:
  - Viết lại property `isOpen` dạng Getter/Setter: mỗi khi giá trị chuyển từ `false` sang `true`, tự động kích hoạt hàm `resetAndInitForm()`.
  - Trong `resetAndInitForm()` và callback của `categorySvc.getLookup()`, tự động gán `categoryId: this.categories[0]?.id` nếu danh mục đang rỗng.

---

### 10. Trình duyệt chặn API do chứng chỉ SSL tự ký (Self-Signed Certificate) trên cổng HTTPS 44346
- **Hiện tượng**: Mở ứng dụng Angular tại `http://localhost:4200`, giao diện tải được khung trang nhưng không lấy được dữ liệu từ backend, kiểm tra console thấy lỗi kết nối mạng (Network Error / Failed to fetch).
- **Nguyên nhân**: Backend chạy trên cổng HTTPS `https://localhost:44346` sử dụng chứng chỉ phát triển nội bộ của ASP.NET Core (`dotnet dev-certs https`). Trình duyệt mặc định chặn các request ngầm chạy qua XHR/Fetch tới địa chỉ HTTPS có chứng chỉ chưa được người dùng chấp thuận (Untrusted / Self-signed).
- **Cách khắc phục**:
  - Mở một tab mới trên trình duyệt và truy cập trực tiếp vào: **`https://localhost:44346/swagger`**.
  - Nhấp vào **Nâng cao (Advanced)** $\rightarrow$ Chọn **Tiếp tục truy cập localhost (Proceed to localhost)** để trình duyệt ghi nhận quyền tin cậy chứng chỉ.
  - Quay lại tab `http://localhost:4200` và tải lại trang (Ctrl + F5), toàn bộ API sẽ thông suốt bình thường.


