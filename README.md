# 🎫 HỆ THỐNG QUẢN LÝ HỖ TRỢ KỸ THUẬT (HELPDESK SYSTEM)

Dự án xây dựng hệ thống Helpdesk doanh nghiệp hiện đại, đa kênh (Omnichannel), hỗ trợ Multi-tenancy, Quản lý cam kết dịch vụ (SLA Engine), Cổng thông tin khách hàng (Customer Portal), Cơ sở tri thức (Knowledge Base) và Đánh giá chất lượng dịch vụ (CSAT) dựa trên nền tảng **ABP Framework v10.6**, **.NET 10**, **PostgreSQL** và **Angular 19 (Standalone)**.

---

## 📑 MỤC LỤC

1. [Tổng Quan Kiến Trúc & Công Nghệ](#-tổng-quan-kiến-trúc--công-nghệ)
2. [Lộ Trình Triển Khai & Trạng Thái Hệ Thống](#-lộ-trình-triển-khai--trạng-thái-hệ-thống)
3. [Chi Tiết Các Phân Hệ & Tính Năng Đã Triển Khai](#-chi-tiết-các-phân-hệ--tính-năng-đã-triển-khai)
   - [Phân Hệ 1: Quản Lý Danh Mục Hệ Thống (Master Data)](#31-phân-hệ-1-quản-lý-danh-mục-hệ-thống-master-data)
   - [Phân Hệ 2: Quản Lý Sự Vụ Cốt Lõi (Ticket Lifecycle & Dual View)](#32-phân-hệ-2-quản-lý-sự-vụ-cốt-lõi-ticket-lifecycle--dual-view)
   - [Phân Hệ 3: Động Cơ Cam Kết Dịch Vụ (SLA Engine & Lịch Làm Việc)](#33-phân-hệ-3-động-cơ-cam-kết-dịch-vụ-sla-engine--lịch-làm-việc)
   - [Phân Hệ 4: Báo Cáo, Thống Kê & Giám Sát (Dashboard Analytics)](#34-phân-hệ-4-báo-cáo-thống-kê--giám-sát-dashboard-analytics)
   - [Phân Hệ 5: Quản Lý Tệp Đính Kèm Blob & Xuất Báo Cáo Excel (Gói Mở Rộng A)](#35-phân-hệ-5-quản-lý-tệp-đính-kèm-blob--xuất-báo-cáo-excel-gói-mở-rộng-a)
   - [Phân Hệ 6: Cơ Sở Tri Thức & Gợi Ý Giải Pháp (Knowledge Base & Deflection)](#36-phân-hệ-6-cơ-sở-tri-thức--gợi-ý-giải-pháp-knowledge-base--deflection)
   - [Phân Hệ 7: Cổng Khách Hàng & Đính Kèm Ảnh Lỗi (Customer Portal & Media Upload)](#37-phân-hệ-7-cổng-khách-hàng--đính-kèm-ảnh-lỗi-customer-portal--media-upload)
   - [Phân Hệ 8: Khảo Sát & Đo Lường Độ Hài Lòng Khách Hàng (CSAT Feedback)](#38-phân-hệ-8-khảo-sát--đo-lường-độ-hài-lòng-khách-hàng-csat-feedback)
   - [Dữ Liệu Khởi Tạo Chuẩn (Data Seeding)](#39-dữ-liệu-khởi-tạo-chuẩn-data-seeding)
4. [Tài Khoản Mặc Định & Phân Quyền Vai Trò](#-tài-khoản-mặc-định--phân-quyền-vai-trò)
5. [Hướng Dẫn Cài Đặt, Migrate CSDL & Khởi Chạy](#-hướng-dẫn-cài-đặt-migrate-csdl--khởi-chạy)
6. [Tổng Hợp Các Lỗi Phát Sinh & Cách Khắc Phục (Troubleshooting Guide)](#-tổng-hợp-các-lỗi-phát-sinh--cách-khắc-phục-troubleshooting-guide)

---

## 🏛️ TỔNG QUAN KIẾN TRÚC & CÔNG NGHỆ

Hệ thống được thiết kế theo kiến trúc chuẩn **Domain-Driven Design (DDD)** của ABP Framework, đảm bảo tính mô-đun hóa cao, dễ mở rộng và bảo trì:

| Tầng / Thành Phần | Công Nghệ & Thư Viện | Vai Trò & Điểm Nổi Bật |
|:---|:---|:---|
| **Backend Core** | **.NET 10.0**, **C# 13**, **ABP Framework v10.6.0** | Xử lý nghiệp vụ chuẩn DDD, Aggregate Roots, Domain Services, Event Bus, Background Workers. |
| **Database & ORM** | **PostgreSQL 17** (Docker), **EF Core 10** | Quản lý dữ liệu quan hệ, Index tối ưu, Audit Logging, Concurrency Check, Code-First Migrations. |
| **Blob Storage** | **Volo.Abp.BlobStoring.Database** | Lưu trữ tập tin đính kèm và hình ảnh chụp lỗi trực tiếp trong PostgreSQL an toàn, đồng bộ giao dịch. |
| **Báo Cáo & Xuất Dữ Liệu**| **MiniExcel 1.46.0** | Thư viện stream Excel `.xlsx` hiệu năng cao, ngốn cực ít RAM, tốc độ xuất hàng chục ngàn dòng trong vài giây. |
| **Frontend Framework** | **Angular 19 (Standalone Components)** | Cấu trúc component độc lập, Signals & RxJS, Reactive Forms, Lazy-loading Routing, Type-safety cao. |
| **Giao Diện & UI/UX** | **Bootstrap 5**, **FontAwesome**, **Chart.js** | Giao diện hiện đại, Dashboard trực quan, Kanban board tương tác, Lightbox phóng to ảnh, Responsive. |
| **Bảo Mật & Phân Quyền**| **OpenIddict**, **JWT Bearer**, **RBAC** | Xác thực OAuth2/OIDC, phân quyền chi tiết (Permissions), tự động điều hướng trang chủ theo vai trò (Role Redirect). |

---

## 🗺️ LỘ TRÌNH TRIỂN KHAI & TRẠNG THÁI HỆ THỐNG

```
[Master Data] ──► [Ticket Core] ──► [SLA Engine] ──► [Dashboard KPI]
       │                 │                │                 │
       ▼                 ▼                ▼                 ▼
[Blob Attachments] ──► [MiniExcel] ──► [Knowledge Base] ──► [Customer Portal]
                                                                    │
                                                                    ▼
                                                            [CSAT Survey (5-Star)]
```

| STT | Phân Hệ / Gói Tính Năng | Chức Năng Cốt Lõi | Trạng Thái |
|:---:|:---|:---|:---:|
| **1** | **Quản Lý Danh Mục (Master Data)** | 6 danh mục: Categories, Priorities, Departments, Statuses, Sources, Canned Responses | **Hoàn thành 100%** |
| **2** | **Quản Lý Sự Vụ (Ticket Core)** | Vòng đời vé, sinh mã `TK-yyyyMMdd-XXXX`, Timeline, Dual View (Table & Kanban), Chuyển trạng thái, Phân công | **Hoàn thành 100%** |
| **3** | **Cam Kết Dịch Vụ (SLA Engine)** | Quy tắc SLA, Giờ làm việc hành chính & Ngày lễ, Phát hiện vi phạm hạn, Báo cáo tuân thủ | **Hoàn thành 100%** |
| **4** | **Báo Cáo & Thống Kê (Dashboard)** | Biểu đồ Chart.js, KPI số lượng, Hiệu suất nhân viên, Tỷ lệ tuân thủ SLA, Điểm CSAT, Tự động refresh | **Hoàn thành 100%** |
| **5** | **Gói Mở Rộng A (Tệp Đính Kèm & Excel)**| Lưu trữ Blob PostgreSQL, Xem trước ảnh Lightbox, Xuất file Excel MiniExcel, Bộ lọc khoảng ngày | **Hoàn thành 100%** |
| **6** | **Cơ Sở Tri Thức (Knowledge Base)** | Quản lý bài viết Markdown, Slug chuẩn URL, Đánh giá Hữu ích / Không hữu ích, Gợi ý khi tạo vé (Deflection) | **Hoàn thành 100%** |
| **7** | **Cổng Khách Hàng (Customer Portal)** | Giao diện tự phục vụ của khách hàng, tạo vé kèm ảnh lỗi (Dropzone), xem tiến độ, trao đổi phản hồi | **Hoàn thành 100%** |
| **8** | **Đo Lường Độ Hài Lòng (CSAT Survey)** | Đánh giá 5 sao & nhận xét sau khi vé giải quyết, thống kê CSAT Dashboard, Bảng xếp hạng hỗ trợ viên | **Hoàn thành 100%** |

---

## 🚀 CHI TIẾT CÁC PHÂN HỆ & TÍNH NĂNG ĐÃ TRIỂN KHAI

### 3.1. Phân Hệ 1: Quản Lý Danh Mục Hệ Thống (Master Data)
Cung cấp bộ danh mục cấu hình toàn diện cho toàn bộ quy trình tiếp nhận và phân loại hỗ trợ:
1. **Category (Danh mục sự cố)**: Hỗ trợ cấu trúc phân cấp cha-con, quản lý mã duy nhất, tên, mô tả.
2. **Priority (Mức độ ưu tiên)**: `Low`, `Medium`, `High`, `Critical`. Gắn mã màu nhận diện (Hex/Color code) và thời gian phản hồi/giải quyết SLA tiêu chuẩn.
3. **Department (Phòng ban hỗ trợ)**: Quản lý các đơn vị kỹ thuật (IT Support, Phần mềm, Mạng, Bảo hành...) và trưởng bộ phận.
4. **TicketStatus (Trạng thái vé)**: Nhóm trạng thái (`Open`, `InProgress`, `Closed`), đánh dấu trạng thái khởi tạo mặc định (`isDefault`) và trạng thái hoàn tất (`isFinal`).
5. **TicketSource (Kênh tiếp nhận)**: Kênh phát sinh (`Portal`, `Email`, `Phone`, `Chat`, `Walk-in`).
6. **CannedResponse (Mẫu phản hồi nhanh)**: Thư viện soạn sẵn mẫu trả lời thường gặp, gắn thẻ theo danh mục, phạm vi áp dụng công khai hoặc nội bộ.
- **Frontend Angular**: Bộ màn hình CRUD trực quan tại `/master-data/*` với thanh tìm kiếm, Modal Reactive Form, phân trang chuẩn và hộp thoại xác nhận xóa.

---

### 3.2. Phân Hệ 2: Quản Lý Sự Vụ Cốt Lõi (Ticket Lifecycle & Dual View)
Trung tâm điều phối và xử lý toàn bộ vòng đời của yêu cầu hỗ trợ:
- **Tự động sinh mã vé**: Domain Service `TicketManager` tự động sinh mã chuẩn `TK-yyyyMMdd-XXXX` (VD: `TK-20260908-0001`), chống trùng lặp tuyệt đối.
- **Chế độ xem linh hoạt (Dual View)**:
  - **Dạng Bảng (Table View)**: Đầy đủ cột dữ liệu nghiệp vụ, nhãn ưu tiên và trạng thái phối màu trực quan, phân trang, lọc đa tiêu chí (Trạng thái, Ưu tiên, Danh mục, Người xử lý, Khoảng ngày).
  - **Dạng Bảng Kanban (Kanban Board View)**: Nhóm thẻ vé theo từng cột trạng thái trực quan, hiển thị nhanh người yêu cầu, độ khẩn cấp, người phụ trách và số lượng trao đổi.
- **Chi Tiết Sự Vụ Toàn Diện (`/tickets/:id`)**:
  - **Phản hồi 2 chế độ**: *Phản Hồi Khách Hàng (Public Reply)* vs *Ghi Chú Kỹ Thuật Nội Bộ (Internal Note - dán nhãn vàng bảo mật)*.
  - Tích hợp chèn nhanh câu trả lời từ kho **Canned Responses**.
  - **Dòng thời gian (Timeline & Audit Trail)**: Kết hợp liên tục giữa nội dung trao đổi, ghi chú nội bộ và nhật ký kiểm toán hệ thống (đổi trạng thái, phân công, tải tệp, đánh giá CSAT).
  - Thao tác nhanh: Đổi trạng thái vé, chuyển giao người phụ trách (Assign), đóng vé.

---

### 3.3. Phân Hệ 3: Động Cơ Cam Kết Dịch Vụ (SLA Engine & Lịch Làm Việc)
Tự động hóa hoàn toàn việc giám sát và cảnh báo thời hạn xử lý:
- **Chính sách SLA linh hoạt (`SlaPolicy` & `SlaRule`)**: Thiết lập thời hạn phản hồi lần đầu (`FirstResponseTimeMinutes`) và giải quyết sự cố (`ResolutionTimeMinutes`) theo từng cặp Danh mục & Mức độ ưu tiên.
- **Khung giờ làm việc (`BusinessHour`) & Ngày lễ (`Holiday`)**: Tự động tính trừ thời gian ngoài giờ làm việc (mặc định Thứ 2 - Thứ 6, 08:30 - 17:30) và các ngày lễ tết được cấu hình trong bảng `AppHolidays`.
- **Bộ máy tính SLA (`SlaManager`)**: Tự động xác định chính xác `FirstResponseDueDate` và `DueDate` ngay khi vé được tạo.
- **Background Worker & Cảnh báo vi phạm**: Quét định kỳ phát hiện sự vụ quá hạn, ghi log vào bảng `AppSlaBreachLogs`.
- **Giao diện báo cáo tuân thủ (`/sla/compliance`)**: Tỷ lệ phản hồi đúng hạn (%), tỷ lệ giải quyết đúng hạn (%), danh sách vi phạm và xuất báo cáo Excel vi phạm SLA.

---

### 3.4. Phân Hệ 4: Báo Cáo, Thống Kê & Giám Sát (Dashboard Analytics)
Cung cấp cái nhìn 360 độ về hiệu suất vận hành của đội ngũ hỗ trợ:
- **Thẻ chỉ số trọng yếu (KPI Stat Cards)**: Tổng số vé, Vé đang mở, Đang xử lý, Đã giải quyết, Vé tạo mới trong ngày, Vé quá hạn (`Overdue`).
- **Chỉ số chất lượng dịch vụ**: Tỷ lệ tuân thủ phản hồi SLA (%), Tỷ lệ tuân thủ giải quyết SLA (%), Điểm hài lòng khách hàng trung bình (CSAT ⭐) và Tỷ lệ khách hàng hài lòng (%).
- **Biểu đồ trực quan (Chart.js)**:
  - *Biểu đồ xu hướng (Trend Chart)*: Khối lượng sự vụ tạo mới, giải quyết và đóng theo các mốc 7, 14, 30, 90 ngày.
  - *Biểu đồ cơ cấu danh mục (Category Distribution)*: Tỷ trọng % theo từng nhóm sự cố (Phần cứng, Phần mềm, Mạng, Tài khoản...).
- **Bảng xếp hạng hiệu suất nhân viên (Agent Performance Leaderboard)**: Số vé tiếp nhận, Số vé đã hoàn tất, Tỷ lệ tuân thủ SLA (%) và Điểm đánh giá CSAT trung bình của từng kỹ thuật viên.
- **Auto-Refresh**: Cơ chế tự động làm mới số liệu mỗi 30 giây giúp màn hình giám sát luôn cập nhật theo thời gian thực.

---

### 3.5. Phân Hệ 5: Quản Lý Tệp Đính Kèm Blob & Xuất Báo Cáo Excel (Gói Mở Rộng A)
- **Lưu trữ Blob trong PostgreSQL Database (`Volo.Abp.BlobStoring.Database`)**:
  - Lưu trữ tập tin đính kèm trực tiếp vào bảng cơ sở dữ liệu `AppTicketAttachments`, đảm bảo tính toàn vẹn dữ liệu, dễ sao lưu và không bị thất lạc file khi chuyển đổi môi trường server.
  - Hỗ trợ tải lên nhiều tệp cùng lúc tại màn hình tạo vé và trong khung phản hồi sự vụ.
  - Nhận diện icon theo định dạng file, hiển thị dung lượng tệp, xem trước hình ảnh lỗi phóng to (**Lightbox Modal**) và tải về an toàn.
- **Xuất Báo Cáo Excel Siêu Tốc với MiniExcel 1.46.0**:
  - Tích hợp thư viện stream Excel `.xlsx` chuyên dụng cho .NET 10.
  - **Xuất danh sách sự vụ**: Kèm đầy đủ thông tin mã vé, tiêu đề, trạng thái, mức ưu tiên, người yêu cầu, kỹ thuật viên, hạn SLA, áp dụng chính xác các bộ lọc đang chọn trên giao diện.
  - **Xuất báo cáo vi phạm SLA**: Trích xuất chi tiết các vụ việc trễ hạn cam kết.
- **Bộ lọc theo khoảng ngày (Date Range Filter)**: Cho phép lọc chính xác danh sách sự vụ phát sinh từ ngày đến ngày.

---

### 3.6. Phân Hệ 6: Cơ Sở Tri Thức & Gợi Ý Giải Pháp (Knowledge Base & Deflection)
Giảm tải khối lượng vé hỗ trợ bằng cổng tra cứu tự phục vụ:
- **Quản lý bài viết Knowledge Base**: Quản lý bài viết với tiêu đề, tóm tắt, nội dung định dạng Markdown, phân mục (`Category`), tags và trạng thái xuất bản (`IsPublished`).
- **Tự động sinh Slug chuẩn SEO**: Sinh URL thân thiện từ tiêu đề tiếng Việt không dấu (VD: `huong-dan-cai-dat-vpn-cong-ty`), tự động hậu tố chống trùng slug.
- **Cơ chế ngăn ngừa tạo vé (Ticket Deflection)**: Khi người dùng đang nhập tiêu đề sự cố tại cổng Portal, hệ thống tự động tìm kiếm và hiển thị danh sách bài viết liên quan kèm nút xem nhanh giải pháp, giúp người dùng tự khắc phục mà không cần gửi vé.
- **Đánh giá Hữu ích**: Người đọc bình chọn "Hữu ích" (👍) hoặc "Chưa hữu ích" (👎), tự động thống kê xếp hạng các bài viết phổ biến nhất (`popular-articles`).

---

### 3.7. Phân Hệ 7: Cổng Khách Hàng & Đính Kèm Ảnh Lỗi (Customer Portal & Media Upload)
Giao diện portal riêng biệt tối ưu hóa trải nghiệm người dùng cuối (`/portal`):
- **Giao diện tự phục vụ thân thiện**: Khách hàng theo dõi danh sách "Vé của tôi", lọc theo trạng thái (Đang xử lý / Đã đóng), tìm kiếm theo mã vé.
- **Tải tệp & Hình ảnh chụp lỗi trực quan (Attachment Dropzone)**:
  - Khách hàng có thể kéo thả hoặc chọn nhiều hình ảnh lỗi/chụp màn hình khi gửi yêu cầu hỗ trợ mới.
  - Hỗ trợ đính kèm tệp bổ sung trong khung gửi phản hồi (Reply).
  - Tích hợp xem trước dạng thumbnail và click để phóng to ảnh xem chi tiết lỗi.
- **API chuyên biệt & Bảo mật tuyệt đối**:
  - Cung cấp các endpoint riêng: `UploadMyAttachmentAsync`, `DownloadMyAttachmentAsync` trong `CustomerPortalAppService`.
  - Kiểm tra xác thực quyền sở hữu vé nghiêm ngặt (`CheckCustomerAccess`), chặn hoàn toàn hành vi truy cập hoặc tải tệp của vé người khác.
  - Ẩn hoàn toàn các ghi chú nội bộ (`IsInternal`) của kỹ thuật viên khỏi màn hình của khách hàng.

---

### 3.8. Phân Hệ 8: Khảo Sát & Đo Lường Độ Hài Lòng Khách Hàng (CSAT Feedback)
Đo lường trực tiếp chất lượng phục vụ của đội ngũ hỗ trợ kỹ thuật:
- **Cơ sở dữ liệu**: Bổ sung các trường `CsatRating` (1 - 5 sao), `CsatComment` (Nhận xét góp ý), `CsatSubmittedAt` (Thời gian đánh giá) vào bảng `AppTickets` qua migration `20260909072858_Add_Ticket_CSAT_Fields`.
- **Luồng đánh giá trên Customer Portal**:
  - Khi sự vụ chuyển sang trạng thái đã xử lý (`Resolved`) hoặc đã đóng (`Closed`), khung đánh giá CSAT 5 sao tương tác sẽ tự động xuất hiện nổi bật tại trang chi tiết `/portal/tickets/:id`.
  - Khách hàng chọn số sao (từ 1 sao "Rất không hài lòng" đến 5 sao "Rất tuyệt vời"), điền nhận xét và gửi đánh giá.
  - Sau khi gửi thành công, giao diện hiển thị thông báo cảm ơn trang trọng và khóa form để chống sửa đổi tùy tiện.
- **Hiển thị trên Quản trị & Chi tiết vé**:
  - Thẻ thông tin CSAT xuất hiện trên trang chi tiết sự vụ của kỹ thuật viên (`/tickets/:id`) hiển thị số sao vàng, nội dung nhận xét và thời gian khách hàng gửi đánh giá.
  - Huy hiệu số sao CSAT (VD: ⭐ 5/5) hiển thị trực tiếp trên danh sách vé của khách hàng.
- **Tích hợp Dashboard KPI**:
  - Thẻ KPI hiển thị Điểm CSAT trung bình và Tỷ lệ phản hồi hài lòng (%).
  - Cột CSAT trung bình được tích hợp trực tiếp vào Bảng xếp hạng hiệu suất nhân viên (Agent Performance Leaderboard).

---

### 3.9. Dữ Liệu Khởi Tạo Chuẩn (Data Seeding)
Hệ thống tích hợp sẵn `HelpdeskDataSeedContributor` tự động nạp dữ liệu mẫu hoàn chỉnh:
- **10 sự vụ mẫu** đa dạng trạng thái, mức ưu tiên, kênh tiếp nhận, hạn SLA chuẩn thực tế.
- **6 danh mục sự cố**, 4 mức độ ưu tiên chuẩn, 7 trạng thái vòng đời vé, 5 kênh tiếp nhận, 2 phòng ban kỹ thuật.
- **Chính sách SLA tiêu chuẩn** và lịch làm việc hành chính chuẩn cùng các ngày lễ lớn trong năm.
- **Bài viết cơ sở tri thức mẫu** hướng dẫn xử lý các sự cố văn phòng phổ biến (VPN, Máy in, Đổi mật khẩu...).

---

## 👥 TÀI KHOẢN MẶC ĐỊNH & PHÂN QUYỀN VAI TRÒ

Hệ thống hỗ trợ cơ chế tự động chuyển hướng trang chủ thông minh theo vai trò người dùng (Role-based Navigation Guard):

| Tài Khoản | Mật Khẩu | Vai Trò (Role) | Trang Mặc Định Sau Khi Đăng Nhập | Quyền Hạn Chính |
|:---|:---|:---|:---|:---|
| **`admin`** | `Huy123@` | **Admin / Quản trị viên** | `/dashboard` | Toàn quyền quản trị hệ thống, cấu hình SLA, Master Data, xem toàn bộ vé, quản lý tài khoản & phân quyền. |
| **`staff1`** | `Huy123@` | **IT Support Staff** | `/tickets` | Tiếp nhận và xử lý sự vụ, phân công, đổi trạng thái, viết ghi chú nội bộ, xem thẻ CSAT và báo cáo SLA. |
| **`customer`**| `Huy123@` | **Customer / Người dùng**| `/portal` | Tự tạo yêu cầu hỗ trợ kèm ảnh lỗi, xem tiến độ vé của chính mình, tra cứu Knowledge Base, gửi đánh giá CSAT. |

---

## 🛠️ HƯỚNG DẪN CÀI ĐẶT, MIGRATE CSDL & KHỞI CHẠY

### 1. Chuẩn Bị Môi Trường
- **.NET 10 SDK** (hoặc .NET 9/10 tương thích).
- **Node.js 20+** & **npm 10+**.
- **PostgreSQL 16/17** (đang chạy cổng `5432` với database `appdb`).
- **Docker** (nếu sử dụng container PostgreSQL).

### 2. Khởi Động Cơ Sở Dữ Liệu PostgreSQL (Docker)
```powershell
docker start postgres
```
*(Đảm bảo PostgreSQL đang lắng nghe tại cổng `localhost:5432`, user: `postgres`, password khớp với cấu hình trong `appsettings.json`)*.

### 3. Cập Nhật Cấu Trúc CSDL (EF Core Database Update)
Chạy lệnh migration từ thư mục gốc của dự án:
```powershell
cd d:\helpdesk\Helpdesk
dotnet ef database update --project src/Helpdesk.EntityFrameworkCore --startup-project src/Helpdesk.HttpApi.Host
```

### 4. Khởi Động Backend API
```powershell
cd d:\helpdesk\Helpdesk
dotnet run --project src/Helpdesk.HttpApi.Host
```
- **Backend API & Swagger UI**: `https://localhost:44346/swagger`

### 5. Khởi Động Frontend Angular
```powershell
cd d:\helpdesk\Helpdesk\angular
npm start
```
- **Giao diện Web Helpdesk**: `http://localhost:4200`

---

## ⚠️ TỔNG HỢP CÁC LỖI PHÁT SINH & CÁCH KHẮC PHỤC (TROUBLESHOOTING GUIDE)

Trong toàn bộ quá trình phát triển và hoàn thiện hệ thống, các vấn đề kỹ thuật sau đây đã xuất hiện và được giải quyết triệt để:

### 1. Không nhìn thấy các bảng dữ liệu trong DBeaver
- **Hiện tượng**: Kết nối PostgreSQL trong DBeaver thành công nhưng mục `Schemas -> public -> Tables` trống trơn.
- **Nguyên nhân**: Mặc định DBeaver kết nối vào database hệ thống `postgres` (rỗng), trong khi toàn bộ các bảng của dự án nằm ở database **`appdb`**.
- **Cách khắc phục**:
  - Chuột phải vào kết nối trong DBeaver $\rightarrow$ **Edit Connection** $\rightarrow$ Tại ô **Database**, đổi từ `postgres` thành **`appdb`** $\rightarrow$ Nhấn **OK**.
  - Hoặc trong **Edit Connection** $\rightarrow$ chọn tab **PostgreSQL** $\rightarrow$ tích chọn **Show all databases** $\rightarrow$ mở rộng nhánh `appdb -> Schemas -> public -> Tables`.

---

### 2. Lỗi 403 Forbidden khi truy cập trang Tickets trên giao diện Angular
- **Hiện tượng**: Menu `Tickets` xuất hiện trên thanh điều hướng nhưng khi bấm vào bị chặn với thông báo lỗi 403 Forbidden.
- **Nguyên nhân**:
  1. Khi bổ sung nhóm quyền mới `Helpdesk.Tickets` trong code, database chưa tự động cấp các quyền này cho vai trò `admin` trong bảng `AbpPermissionGrants`.
  2. Backend API đang chạy vẫn giữ bộ nhớ đệm (Permission Cache) từ lúc khởi động.
  3. Lỗi từ API phụ trợ (như nạp danh sách user `IdentityUserService.getList()`) bị Interceptor mặc định của ABP chuyển hướng toàn bộ trang sang 403.
- **Cách khắc phục**:
  - Cấp toàn bộ quyền `Helpdesk.Tickets.*` cho vai trò `admin` vào bảng `AbpPermissionGrants`:
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
  - Khởi động lại Backend API để làm mới cache quyền.
  - Trên Angular: Bọc các lệnh gọi API phụ trong `tickets.component.ts` bằng `catchError(() => of(...))` từ RxJS.

---

### 3. Khóa file DLL khi chạy `dotnet build` hoặc `dotnet ef migrations` (Lỗi MSB3027 / MSB3021)
- **Hiện tượng**: Báo lỗi không thể sao chép hoặc ghi đè file `*.dll` trong thư mục `bin\Debug\net10.0\` vì file đang bị tiến trình khác sử dụng (*"The process cannot access the file because it is being used by another process"*).
- **Nguyên nhân**: Tiến trình Backend `Helpdesk.HttpApi.Host` đang chạy ngầm và khóa các file DLL của solution.
- **Cách khắc phục**: Tắt tiến trình Backend API (Ctrl+C hoặc kill process) trước khi biên dịch hoặc tạo migration mới, sau đó khởi động lại.

---

### 4. Xung đột Async LINQ trong tầng Application (`ToListAsync` vs `AsyncExecuter`)
- **Hiện tượng**: Gọi trực tiếp `await query.ToListAsync()` hoặc `await query.CountAsync()` trong tầng Application gây lỗi biên dịch hoặc xung đột thư viện EF Core.
- **Nguyên nhân**: Kiến trúc chuẩn của ABP Framework khuyến nghị tầng Application không phụ thuộc trực tiếp vào package `Microsoft.EntityFrameworkCore` để bảo đảm tính độc lập với ORM.
- **Cách khắc phục**: Sử dụng đối tượng `AsyncExecuter` được tích hợp sẵn trong `ApplicationService` của ABP:
  ```csharp
  var totalCount = await AsyncExecuter.CountAsync(query);
  var items = await AsyncExecuter.ToListAsync(query);
  ```

---

### 5. Lỗi đường dẫn import và sai lệch trường DTO khi chạy `abp generate-proxy -t ng`
- **Hiện tượng**: Sau khi chạy lệnh sinh mã proxy Angular, một số component cũ bị lỗi compile: `Cannot find module ...` hoặc báo lỗi thuộc tính không tồn tại trên DTO.
- **Nguyên nhân**: ABP CLI phiên bản mới tự động phân tách proxy thành các thư mục con theo từng namespace backend (`proxy/tickets/`, `proxy/categories/`, `proxy/ticket-statuses/`...).
- **Cách khắc phục**:
  - Cập nhật lại đường dẫn import tương đối trỏ chính xác vào thư mục con (`../../proxy/categories/category.service`).
  - Đồng bộ lại các trường trong Reactive Form và HTML template đúng với cấu trúc `models.ts` được sinh ra bởi ABP Proxy.

---

### 6. Lỗi TypeScript Strict Nullability (`TS2322: undefined is not assignable to string`)
- **Hiện tượng**: Lệnh `ng build` bị lỗi do Angular 19 bật cấu hình kiểm tra kiểu nghiêm ngặt (Strict Type Checking): `Type 'string | undefined' is not assignable to type 'string'`.
- **Nguyên nhân**: Các trường trong ABP EntityDto sinh ra dạng optional (`title?: string`, `name?: string`). Khi truyền trực tiếp vào các hàm yêu cầu kiểu `string`, TypeScript sẽ chặn lại.
- **Cách khắc phục**:
  - Bổ sung giá trị dự phòng (fallback): `item.name ?? ''`, `item.title ?? ''`.
  - Sử dụng non-null assertion `item.id!` khi đã chắc chắn dữ liệu tồn tại.

---

### 7. Lỗi bảng rỗng dù footer báo "Tổng: 10 sự vụ" (Lệch chuẩn chỉ số trang 0-index vs 1-index)
- **Hiện tượng**: Footer trang luôn báo đúng tổng số bản ghi (ví dụ: *"Tổng: 10 sự vụ"*), nhưng thân bảng rỗng hoàn toàn: *"Không tìm thấy sự vụ nào"*.
- **Nguyên nhân gốc rễ**:
  1. Service phân trang của ABP Framework (`ListService`) hoạt động theo chuẩn **0-indexed** (`_page = 0` là trang đầu; `skipCount = _page * maxResultCount = 0 * 10 = 0`).
  2. Component `<ngb-pagination>` của Angular UI Bootstrap lại hoạt động theo chuẩn **1-indexed** (trang đầu tiên bắt buộc phải là `1`).
  3. Gắn binding hai chiều `[(page)]="list.page"` làm `<ngb-pagination>` tự ép giá trị thành `1` và gọi `pageChange` gán ngược `list.page = 1`.
  4. Backend nhận `SkipCount = 10` nên bỏ qua toàn bộ 10 bản ghi hiện có và trả về danh sách rỗng.
- **Cách khắc phục**: Chuẩn hóa lại cơ chế binding giữa `0-indexed` và `1-indexed` trên toàn bộ template HTML:
  ```html
  <ngb-pagination
    [page]="list.page + 1"
    [pageSize]="list.maxResultCount"
    [collectionSize]="totalCount"
    (pageChange)="list.page = $event - 1"
    [maxSize]="5"
  />
  ```

---

### 8. Lỗi Modal Tạo Yêu Cầu Mới bị cắt mất chân trang (Mất nút "Tạo Yêu Cầu" và "Đóng")
- **Hiện tượng**: Mở modal nhập thông tin sự vụ trên `/tickets`, nhưng phía đáy không có nút "Tạo Yêu Cầu" hay "Đóng"; form bị cắt cụt ngang ô thông tin người gửi.
- **Nguyên nhân**: Cấu trúc Flexbox trong modal Bootstrap kết hợp `modal-dialog-scrollable` chưa thiết lập `min-height: 0` và `overflow-y: auto` cho `.modal-body`, khiến form giãn dài tự do và đẩy footer ra ngoài khung nhìn của container `overflow: hidden`.
- **Cách khắc phục**: Cấu trúc lại thẻ `<form>` và `.modal-body`:
  - Header và Footer đặt class `flex-shrink-0`.
  - Thẻ `<form>`: `class="d-flex flex-column flex-grow-1 overflow-hidden" style="min-height: 0;"`.
  - Thẻ `.modal-body`: `class="modal-body p-4 flex-grow-1" style="overflow-y: auto;"`.

---

### 9. Lỗi ô chọn Danh mục không hiển thị giá trị mặc định khi mở Modal Tạo Sự Vụ
- **Hiện tượng**: Khi mở modal Tạo Yêu Cầu Mới, ô Danh mục hiển thị `-- Chọn danh mục --` thay vì chọn sẵn danh mục đầu tiên như các ô Priority, Status.
- **Nguyên nhân**: Hàm `loadLookups()` chưa gán giá trị mặc định cho `categoryId`, và modal không reset lại trạng thái khi đóng/mở lại.
- **Cách khắc phục**: Chuyển property `isOpen` thành Getter/Setter để tự động kích hoạt hàm `resetAndInitForm()` mỗi khi modal được mở, tự động gán `categoryId: this.categories[0]?.id`.

---

### 10. Trình duyệt chặn API do chứng chỉ SSL tự ký trên cổng HTTPS 44346
- **Hiện tượng**: Mở ứng dụng Angular tại `http://localhost:4200` nhưng không nạp được dữ liệu từ backend; console báo `Failed to fetch` hoặc `Network Error`.
- **Nguyên nhân**: Backend chạy trên `https://localhost:44346` với chứng chỉ SSL phát triển nội bộ chưa được trình duyệt tin cậy.
- **Cách khắc phục**: Mở tab mới truy cập trực tiếp `https://localhost:44346/swagger` $\rightarrow$ Nhấp **Nâng cao (Advanced)** $\rightarrow$ Chọn **Tiếp tục truy cập localhost (Proceed to localhost)**, sau đó quay lại trang Angular tải lại (Ctrl + F5).

---

### 11. Lỗi `cannot convert from 'byte[]' to 'System.IO.Stream'` khi lưu Blob vào IBlobContainer
- **Hiện tượng**: Trình biên dịch báo lỗi `CS1503: Argument 2: cannot convert from 'byte[]' to 'System.IO.Stream'` khi gọi `_blobContainer.SaveAsync(blobName, bytes, overrideExisting: true)`.
- **Nguyên nhân**: `IBlobContainer` cốt lõi chỉ định nghĩa phương thức nhận `Stream`. Phương thức nhận mảng byte `byte[]` là một Extension Method nằm trong namespace `Volo.Abp.BlobStoring`.
- **Cách khắc phục**: Bổ sung `using Volo.Abp.BlobStoring;` ở đầu file Service C# để đưa extension method vào tầm vực hoạt động.

---

### 12. Lỗi 403 Forbidden khi người dùng Customer tải lên hoặc xem ảnh đính kèm
- **Hiện tượng**: Khách hàng tạo vé hoặc phản hồi trong Customer Portal tải ảnh lên thì backend từ chối với mã lỗi 403 Forbidden.
- **Nguyên nhân**: Phương thức `UploadAttachmentAsync` và `DownloadAttachmentAsync` nằm trong `TicketAppService` và bị bảo vệ bởi quyền `[Authorize(HelpdeskPermissions.Tickets.Default)]` dành riêng cho kỹ thuật viên/quản trị.
- **Cách khắc phục**:
  - Bổ sung 2 API chuyên biệt trong `CustomerPortalAppService`: `UploadMyAttachmentAsync` và `DownloadMyAttachmentAsync`.
  - Áp dụng hàm bảo mật `CheckCustomerAccess(ticket)` để xác thực nghiêm ngặt rằng vé thuộc quyền sở hữu của chính người dùng đang đăng nhập trước khi cho phép tải/xem tệp.

---

### 13. Lỗi `The ConnectionString property has not been initialized` khi chạy DbMigrator
- **Hiện tượng**: Chạy lệnh `dotnet run --project src/Helpdesk.DbMigrator` từ thư mục gốc bị lỗi crash với thông báo chuỗi kết nối chưa được khởi tạo.
- **Nguyên nhân**: Dự án `DbMigrator` đọc file cấu hình `appsettings.json` tại thư mục làm việc hiện tại (Working Directory). Nếu chạy từ bên ngoài mà không chuyển thư mục, tiến trình không nạp được cấu hình kết nối.
- **Cách khắc phục**: Sử dụng trực tiếp công cụ Entity Framework Core CLI với tham số chỉ định rõ startup project:
  ```powershell
  dotnet ef database update --project src/Helpdesk.EntityFrameworkCore --startup-project src/Helpdesk.HttpApi.Host
  ```

---

### 14. Lỗi TypeScript Strict Mode: `Type 'undefined' cannot be used as an index type` (TS2538)
- **Hiện tượng**: Lệnh biên dịch Angular (`ng build`) báo lỗi `TS2538: Type 'undefined' cannot be used as an index type` khi sử dụng `att.id` làm khóa truy cập object lưu thumbnail `this.thumbnails[att.id]`.
- **Nguyên nhân**: Interface `TicketAttachmentDto` khai báo trường `id` dạng optional (`id?: string`). Angular 19 với cờ `strict` ngăn cản việc dùng giá trị có thể mang kiểu `undefined` làm key cho dictionary.
- **Cách khắc phục**: Áp dụng cơ chế thu hẹp kiểu (Type Narrowing):
  ```typescript
  const attId = att.id;
  if (attId) {
    this.thumbnails[attId] = url;
  }
  ```
  Hoặc xây dựng hàm helper: `getImageUrl(attachmentId?: string): string`.

---

### 15. Lỗi `The type or namespace name 'Mvc' does not exist in 'Microsoft.AspNetCore'` trong tầng Application
- **Hiện tượng**: Thêm annotation `[Microsoft.AspNetCore.Mvc.HttpPost]` vào phương thức trong `CustomerPortalAppService` gây lỗi build dự án `Helpdesk.Application`.
- **Nguyên nhân**: Tầng `Application` trong kiến trúc chuẩn ABP Framework tách biệt hoàn toàn với Web/MVC, không tham chiếu trực tiếp tới assembly ASP.NET Core MVC.
- **Cách khắc phục**: Loại bỏ attribute MVC. Hệ thống Conventional API của ABP Framework sẽ tự động nhận diện và chuyển đổi các phương thức có tiền tố `Submit...`, `Create...`, `Upload...` thành HTTP POST endpoint tương ứng.

---

### 16. Xung đột và cấp phép thương mại khi xử lý Excel trên .NET 10
- **Hiện tượng**: Sử dụng các thư viện như `EPPlus` đòi hỏi bản quyền thương mại và cấu hình LicenseContext phức tạp; các thư viện COM Interop hoặc `ClosedXML` cũ gây tốn nhiều RAM và cảnh báo tương thích với .NET 10.
- **Nguyên nhân**: .NET 10 nâng cấp cơ chế JIT và tối ưu hóa bộ nhớ, nhiều thư viện Excel đời cũ chưa tương thích hoàn toàn.
- **Cách khắc phục**: Tích hợp **MiniExcel 1.46.0** — thư viện stream Excel mã nguồn mở hiệu năng cao hàng đầu thế giới cho .NET, ghi trực tiếp luồng nhị phân vào `MemoryStream`, tối ưu hóa bộ nhớ RAM ở mức tối đa và tương thích 100% với .NET 10.
