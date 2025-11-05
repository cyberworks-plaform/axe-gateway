using Ce.Gateway.Api.Data;
using Ce.Gateway.Api.Repositories;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Ce.Gateway.Api.Tests
{
    public class NodeSummaryGroupingTests
    {
        private readonly ITestOutputHelper _output;
        private readonly DbContextOptions<GatewayDbContext> _options;
        private readonly IMemoryCache _memoryCache;

        public NodeSummaryGroupingTests(ITestOutputHelper output)
        {
            _output = output;
            var dbPath = Path.Combine("..", "..", "..", "..", "Ce.Gateway.Api", "Data", "gateway.db");
            var connectionString = $"DataSource={dbPath}";
            var connection = new SqliteConnection(connectionString);
            connection.Open();
            _options = new DbContextOptionsBuilder<GatewayDbContext>()
                .UseSqlite(connection)
                .Options;
            _memoryCache = new MemoryCache(new MemoryCacheOptions());
        }

        [Fact]
        public async Task GetNodeSummaryAggregateAsync_ShouldGroupByDownstreamNode()
        {
            // Arrange
            var dbContextFactory = new TestDbContextFactory(_options);
            var logRepository = new LogRepository(dbContextFactory, _memoryCache);

            var now = new DateTime(2025, 10, 31);
            var from = now.AddDays(-1);

            // Act
            var nodeSummary = await logRepository.GetNodeSummaryAggregateAsync(from, now);

            // Assert
            Assert.NotNull(nodeSummary);
            Assert.NotEmpty(nodeSummary);

            _output.WriteLine($"Total nodes found: {nodeSummary.Count}");

            foreach (var node in nodeSummary.Take(10))
            {
                _output.WriteLine($"Node: {node.Node}");
                _output.WriteLine($"  - Total Requests: {node.TotalRequests}");
                _output.WriteLine($"  - Avg Latency: {node.AvgLatencyMs}ms");
                _output.WriteLine($"  - Min/Max: {node.MinLatencyMs}ms / {node.MaxLatencyMs}ms");
                _output.WriteLine("");

                // Verify node format is "host:port"
                Assert.Contains(":", node.Node);
            }
        }

        [Fact]
        public async Task GetNodeSummaryAggregateAsync_ShouldReturnDistinctNodePorts()
        {
            // Arrange
            var dbContextFactory = new TestDbContextFactory(_options);
            var logRepository = new LogRepository(dbContextFactory, _memoryCache);

            var now = new DateTime(2025, 10, 31);
            var from = now.AddDays(-7);

            // Act
            var nodeSummary = await logRepository.GetNodeSummaryAggregateAsync(from, now);

            // Assert
            Assert.NotNull(nodeSummary);

            var nodeKeys = nodeSummary.Select(n => n.Node).ToList();
            var distinctNodes = nodeKeys.Distinct().ToList();

            // All nodes should be unique (no duplicates)
            Assert.Equal(nodeKeys.Count, distinctNodes.Count);

            _output.WriteLine($"Total unique downstream nodes: {distinctNodes.Count}");
            
            // Check that nodes include port numbers
            foreach (var node in distinctNodes.Take(5))
            {
                var parts = node.Split(':');
                Assert.Equal(2, parts.Length); // Should be "host:port"
                
                _output.WriteLine($"  - {node} (Host: {parts[0]}, Port: {parts[1]})");
            }
        }

        [Fact]
        public async Task GetNodeSummaryAggregateAsync_ShouldNotIncludeNullDownstreamHosts()
        {
            // Arrange
            var dbContextFactory = new TestDbContextFactory(_options);
            var logRepository = new LogRepository(dbContextFactory, _memoryCache);

            var now = new DateTime(2025, 10, 31);
            var from = now.AddDays(-1);

            // Act
            var nodeSummary = await logRepository.GetNodeSummaryAggregateAsync(from, now);

            // Assert
            foreach (var node in nodeSummary)
            {
                // Node should not be null or empty
                Assert.False(string.IsNullOrEmpty(node.Node));
                
                // Should not contain "null" or ":0" (null port)
                Assert.DoesNotContain("null", node.Node.ToLower());
            }

            _output.WriteLine($"All {nodeSummary.Count} nodes have valid downstream host information");
        }

        [Fact]
        public async Task GetNodeSummaryAggregateAsync_ShouldCalculateMetricsPerNode()
        {
            // Arrange
            var dbContextFactory = new TestDbContextFactory(_options);
            var logRepository = new LogRepository(dbContextFactory, _memoryCache);

            var now = new DateTime(2025, 10, 31);
            var from = now.AddDays(-1);

            // Act
            var nodeSummary = await logRepository.GetNodeSummaryAggregateAsync(from, now);

            // Assert
            Assert.NotEmpty(nodeSummary);

            foreach (var node in nodeSummary)
            {
                // Metrics should be valid
                Assert.True(node.TotalRequests > 0, $"Node {node.Node} should have requests");
                Assert.True(node.MinLatencyMs >= 0, $"Node {node.Node} min latency should be >= 0");
                Assert.True(node.MaxLatencyMs >= node.MinLatencyMs, 
                    $"Node {node.Node} max latency should be >= min latency");
                Assert.True(node.AvgLatencyMs >= 0, $"Node {node.Node} avg latency should be >= 0");
            }

            var topNode = nodeSummary.First();
            _output.WriteLine($"Top node by requests: {topNode.Node}");
            _output.WriteLine($"  Requests: {topNode.TotalRequests}");
            _output.WriteLine($"  Latency: Min={topNode.MinLatencyMs}ms, Avg={topNode.AvgLatencyMs}ms, Max={topNode.MaxLatencyMs}ms");
        }

        [Fact]
        public async Task GetNodeSummaryAggregateAsync_ShouldOrderByTotalRequests()
        {
            // Arrange
            var dbContextFactory = new TestDbContextFactory(_options);
            var logRepository = new LogRepository(dbContextFactory, _memoryCache);

            var now = new DateTime(2025, 10, 31);
            var from = now.AddDays(-1);

            // Act
            var nodeSummary = await logRepository.GetNodeSummaryAggregateAsync(from, now);

            // Assert
            Assert.NotEmpty(nodeSummary);

            // Check that nodes are ordered by TotalRequests DESC
            for (int i = 0; i < nodeSummary.Count - 1; i++)
            {
                Assert.True(nodeSummary[i].TotalRequests >= nodeSummary[i + 1].TotalRequests,
                    $"Nodes should be ordered by TotalRequests DESC");
            }

            _output.WriteLine("Node summary correctly ordered by total requests:");
            foreach (var node in nodeSummary.Take(5))
            {
                _output.WriteLine($"  {node.Node}: {node.TotalRequests} requests");
            }
        }
    }
}
