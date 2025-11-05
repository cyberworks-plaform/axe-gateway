using Ce.Gateway.Api.Controllers.Api;
using Ce.Gateway.Api.Models;
using Ce.Gateway.Api.Services.Interface;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Ce.Gateway.Api.Tests
{
    public class RequestReportControllerTests
    {
        private readonly ITestOutputHelper _output;
        private readonly Mock<IRequestReportService> _mockService;

        public RequestReportControllerTests(ITestOutputHelper output)
        {
            _output = output;
            _mockService = new Mock<IRequestReportService>();
        }

        [Theory]
        [InlineData("1d", Granularity.Hour)]
        [InlineData("7d", Granularity.Day)]
        [InlineData("1m", Granularity.Day)]
        [InlineData("3m", Granularity.Month)]
        [InlineData("9m", Granularity.Month)]
        [InlineData("12m", Granularity.Month)]
        public async Task GetReportData_ShouldCallServiceWithCorrectGranularity(string period, Granularity expectedGranularity)
        {
            // Arrange
            var expectedReport = new RequestReportDto
            {
                TotalRequests = 100,
                SuccessRequests = 80,
                ClientErrorRequests = 15,
                ServerErrorRequests = 5
            };

            _mockService
                .Setup(s => s.GetReportAsync(
                    It.IsAny<DateTime>(),
                    It.IsAny<DateTime>(),
                    expectedGranularity,
                    null))
                .ReturnsAsync(expectedReport);

            var controller = new RequestReportController(_mockService.Object);

            // Act
            var result = await controller.GetReportData(period);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var returnedReport = Assert.IsType<RequestReportDto>(okResult.Value);
            Assert.Equal(100, returnedReport.TotalRequests);

            _mockService.Verify(s => s.GetReportAsync(
                It.IsAny<DateTime>(),
                It.IsAny<DateTime>(),
                expectedGranularity,
                null), Times.Once);

            _output.WriteLine($"Period: {period}, Granularity: {expectedGranularity}, Total: {returnedReport.TotalRequests}");
        }

        [Fact]
        public async Task GetReportData_WithInvalidPeriod_ShouldUseDefaultGranularity()
        {
            // Arrange
            var expectedReport = new RequestReportDto { TotalRequests = 50 };

            _mockService
                .Setup(s => s.GetReportAsync(
                    It.IsAny<DateTime>(),
                    It.IsAny<DateTime>(),
                    Granularity.Hour, // Default is Hour for invalid period
                    null))
                .ReturnsAsync(expectedReport);

            var controller = new RequestReportController(_mockService.Object);

            // Act
            var result = await controller.GetReportData("invalid_period");

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(okResult.Value);

            _mockService.Verify(s => s.GetReportAsync(
                It.IsAny<DateTime>(),
                It.IsAny<DateTime>(),
                Granularity.Hour,
                null), Times.Once);

            _output.WriteLine("Invalid period correctly defaults to Hour granularity");
        }

        [Fact]
        public async Task GetReportData_ShouldCalculateCorrectDateRanges()
        {
            // Arrange
            DateTime? capturedFrom = null;
            DateTime? capturedTo = null;

            _mockService
                .Setup(s => s.GetReportAsync(
                    It.IsAny<DateTime>(),
                    It.IsAny<DateTime>(),
                    It.IsAny<Granularity>(),
                    null))
                .Callback<DateTime, DateTime, Granularity, ReportFilter>((from, to, gran, filter) =>
                {
                    capturedFrom = from;
                    capturedTo = to;
                })
                .ReturnsAsync(new RequestReportDto());

            var controller = new RequestReportController(_mockService.Object);

            // Act
            await controller.GetReportData("7d");

            // Assert
            Assert.NotNull(capturedFrom);
            Assert.NotNull(capturedTo);
            
            var daysDiff = (capturedTo.Value - capturedFrom.Value).TotalDays;
            Assert.True(daysDiff >= 6.9 && daysDiff <= 7.1, $"Expected ~7 days difference, got {daysDiff}");

            _output.WriteLine($"Date range: {capturedFrom} to {capturedTo} ({daysDiff} days)");
        }

        [Fact]
        public async Task GetReportData_WithoutPeriod_ShouldUseDefault()
        {
            // Arrange
            var expectedReport = new RequestReportDto { TotalRequests = 25 };

            _mockService
                .Setup(s => s.GetReportAsync(
                    It.IsAny<DateTime>(),
                    It.IsAny<DateTime>(),
                    Granularity.Hour,
                    null))
                .ReturnsAsync(expectedReport);

            var controller = new RequestReportController(_mockService.Object);

            // Act - No period parameter
            var result = await controller.GetReportData();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var returnedReport = Assert.IsType<RequestReportDto>(okResult.Value);
            Assert.Equal(25, returnedReport.TotalRequests);

            _output.WriteLine("Default period (1d) works correctly");
        }
    }
}
