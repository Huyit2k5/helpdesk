namespace Helpdesk.Assets;

public enum AssetType
{
    Laptop = 1,
    Desktop = 2,
    Monitor = 3,
    NetworkDevice = 4,
    PrinterPeripheral = 5,
    ServerStorage = 6,
    SoftwareLicense = 7,
    MobileDevice = 8,
    Other = 9
}

public enum AssetStatus
{
    InStock = 1,
    Assigned = 2,
    UnderRepair = 3,
    Reserved = 4,
    Retired = 5,
    LostStolen = 6
}

public enum AssetActivityType
{
    Created = 1,
    Assigned = 2,
    Returned = 3,
    StatusChanged = 4,
    SentToRepair = 5,
    Repaired = 6,
    TicketLinked = 7,
    NoteAdded = 8,
    HandoverConfirmed = 9,
    MaintenanceStarted = 10,
    MaintenanceCompleted = 11
}

public enum MaintenanceType
{
    Repair = 1,          // Sửa chữa sự cố hỏng hóc
    Preventive = 2,      // Bảo trì định kỳ dự phòng (vệ sinh, tra keo, kiểm tra)
    Upgrade = 3,         // Nâng cấp phần cứng (RAM, SSD, Card)
    Inspection = 4       // Kiểm tra đánh giá tình trạng kỹ thuật
}

public enum MaintenanceStatus
{
    Draft = 1,           // Dự thảo / Chờ duyệt gửi
    InProgress = 2,      // Đang gửi sửa / Đang tiến hành
    Completed = 3,       // Đã hoàn tất nghiệm thu
    Cancelled = 4        // Đã hủy phiếu
}
