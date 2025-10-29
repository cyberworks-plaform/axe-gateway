using System.Collections.Generic;

namespace Ce.Gateway.Api.Models;

public class RequestReportDto
{
    public List<TimeSlotData> TimeSlots { get; set; } = new();
    public string TimeFormat { get; set; }
    public int TotalRequests { get; set; }
    public int SuccessRequests { get; set; }
    public int ClientErrorRequests { get; set; }
    public int ServerErrorRequests { get; set; }
    public int OtherRequests { get; set; }
}

public class TimeSlotData
{
    public string Label { get; set; }
    public int SuccessCount { get; set; }
    public int ClientErrorCount { get; set; }
    public int ServerErrorCount { get; set; }
    public int OtherCount { get; set; }
    public int TotalCount => SuccessCount + ClientErrorCount + ServerErrorCount + OtherCount;
}
