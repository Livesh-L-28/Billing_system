using System;

namespace BillingSoftware.Core.Entities;

public class LoginHistory
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public DateTime LoginTime { get; set; } = DateTime.UtcNow;
    public DateTime? LogoutTime { get; set; }
    public int TodayBillsCount { get; set; }
    public decimal TodaySalesSum { get; set; }
    public decimal TodayGSTCollected { get; set; }
}
