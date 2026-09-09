using System;
using System.ComponentModel.DataAnnotations;

namespace Helpdesk.CustomerPortal.Dtos;

public class SubmitTicketFeedbackDto
{
    [Range(1, 5, ErrorMessage = "Điểm đánh giá phải từ 1 đến 5 sao.")]
    public int Rating { get; set; }

    [MaxLength(1000, ErrorMessage = "Nhận xét không vượt quá 1000 ký tự.")]
    public string? Comment { get; set; }
}
