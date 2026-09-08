namespace Helpdesk.Tickets;

public enum TicketActivityType
{
    Created = 1,
    StatusChanged = 2,
    Assigned = 3,
    PriorityChanged = 4,
    CategoryChanged = 5,
    DepartmentChanged = 6,
    CommentAdded = 7,
    InternalNoteAdded = 8,
    AttachmentAdded = 9,
    Resolved = 10,
    Closed = 11,
    Reopened = 12
}
