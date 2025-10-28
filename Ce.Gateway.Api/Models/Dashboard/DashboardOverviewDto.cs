using System.Collections.Generic;

namespace Ce.Gateway.Api.Models.Dashboard
{
    public class DashboardOverviewDto
    {
        public int TotalNodes { get; set; }
        public int NodesDown { get; set; }
        public long TotalRequests { get; set; }
        public double ErrorRate { get; set; }
        public long AvgLatencyMs { get; set; }
        public List<TimelineChartDataDto> RequestTimeline { get; set; }
        public List<TimelineChartDataDto> LatencyTimeline { get; set; }
        public List<DonutChartDataDto> HttpStatusDistribution { get; set; }
    }

    public class TimelineChartDataDto
    {
        public string Timestamp { get; set; }
        public long RequestCount { get; set; }
    }

    public class DonutChartDataDto
    {
        public string Label { get; set; }
        public long Count { get; set; }
    }
}
