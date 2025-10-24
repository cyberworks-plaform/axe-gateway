namespace Ce.Gateway.Api.Models
{
    public class NodeStatusSummaryDto
    {
        public int TotalNodes { get; set; }
        public int NodesUp { get; set; }
        public int NodesDown { get; set; }
    }
}
