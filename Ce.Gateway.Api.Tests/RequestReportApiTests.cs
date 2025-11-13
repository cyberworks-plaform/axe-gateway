using Ce.Gateway.Api.Controllers.Api;
using Ce.Gateway.Api.Models;
using Ce.Gateway.Api.Services.Interface;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Ce.Gateway.Api.Tests
{
    /// <summary>
    /// API endpoint tests for RequestReportController
    /// Tests HTTP status codes, response types, error handling, and edge cases
    /// </summary>
    public class RequestReportApiTests
    {
        private readonly ITestOutputHelper _output;
        private readonly Mock<IRequestReportService> _mockService;

        public RequestReportApiTests(ITestOutputHelper output)
        {
            _output = output;
            _mockService = new Mock<IRequestReportService>();
        }

        #region GET /api/requestreport/data

        [Fact]
        public async Task GetReportData_ReturnsOkResult_WithValidData()
        {
            // Arrange
            var expectedReport = new RequestReportDto
            {
                TotalRequests = 1000,
                SuccessRequests = 800,
                ClientErrorRequests = 150,
                ServerErrorRequests = 50,
                OtherRequests = 0,
                TimeSlots = new List<TimeSlotData>
                {
                    new TimeSlotData { Label = "2025-11-01", SuccessCount = 400, ClientErrorCount = 75, ServerErrorCount = 25 },
                    new TimeSlotData { Label = "2025-11-02", SuccessCount = 400, ClientErrorCount = 75, ServerErrorCount = 25 }
                },
                TimeFormat = "yyyy-MM-dd"
            };

            _mockService
                .Setup(s => s.GetReportAsync(
                    It.IsAny<DateTime>(),
                    It.IsAny<DateTime>(),
                    It.IsAny<Granularity>(),
                    null))
                .ReturnsAsync(expectedReport);

            var controller = new RequestReportController(_mockService.Object);

            // Act
            var result = await controller.GetReportData("7d");

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(200, okResult.StatusCode);
            
            var returnedReport = Assert.IsType<RequestReportDto>(okResult.Value);
            Assert.Equal(1000, returnedReport.TotalRequests);
            Assert.Equal(800, returnedReport.SuccessRequests);
            Assert.Equal(2, returnedReport.TimeSlots.Count);

            _output.WriteLine($"API returned OK (200) with {returnedReport.TotalRequests} requests");
        }

        [Theory]
        [InlineData("1d")]
        [InlineData("7d")]
        [InlineData("1m")]
        [InlineData("3m")]
        [InlineData("9m")]
        [InlineData("12m")]
        public async Task GetReportData_AllValidPeriods_ReturnOk(string period)
        {
            // Arrange
            var expectedReport = new RequestReportDto { TotalRequests = 100 };
            
            _mockService
                .Setup(s => s.GetReportAsync(
                    It.IsAny<DateTime>(),
                    It.IsAny<DateTime>(),
                    It.IsAny<Granularity>(),
                    null))
                .ReturnsAsync(expectedReport);

            var controller = new RequestReportController(_mockService.Object);

            // Act
            var result = await controller.GetReportData(period);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(200, okResult.StatusCode);

            _output.WriteLine($"Period '{period}' returned OK (200)");
        }

        [Fact]
        public async Task GetReportData_WithNullPeriod_UsesDefaultPeriod()
        {
            // Arrange
            var expectedReport = new RequestReportDto { TotalRequests = 50 };
            
            _mockService
                .Setup(s => s.GetReportAsync(
                    It.IsAny<DateTime>(),
                    It.IsAny<DateTime>(),
                    Granularity.Hour, // Default is Hour for 1d
                    null))
                .ReturnsAsync(expectedReport);

            var controller = new RequestReportController(_mockService.Object);

            // Act
            var result = await controller.GetReportData();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(200, okResult.StatusCode);

            _mockService.Verify(s => s.GetReportAsync(
                It.IsAny<DateTime>(),
                It.IsAny<DateTime>(),
                Granularity.Hour,
                null), Times.Once);

            _output.WriteLine("Null period defaults to 1d (Hour granularity)");
        }

        [Fact]
        public async Task GetReportData_WithInvalidPeriod_StillReturnsOk()
        {
            // Arrange
            var expectedReport = new RequestReportDto { TotalRequests = 25 };
            
            _mockService
                .Setup(s => s.GetReportAsync(
                    It.IsAny<DateTime>(),
                    It.IsAny<DateTime>(),
                    It.IsAny<Granularity>(),
                    null))
                .ReturnsAsync(expectedReport);

            var controller = new RequestReportController(_mockService.Object);

            // Act
            var result = await controller.GetReportData("invalid_period");

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(200, okResult.StatusCode);

            _output.WriteLine("Invalid period falls back to default and returns OK");
        }

        [Fact]
        public async Task GetReportData_ServiceReturnsEmptyData_ReturnsOkWithEmptyReport()
        {
            // Arrange
            var emptyReport = new RequestReportDto
            {
                TotalRequests = 0,
                SuccessRequests = 0,
                ClientErrorRequests = 0,
                ServerErrorRequests = 0,
                OtherRequests = 0,
                TimeSlots = new List<TimeSlotData>(),
                TimeFormat = "yyyy-MM-dd"
            };
            
            _mockService
                .Setup(s => s.GetReportAsync(
                    It.IsAny<DateTime>(),
                    It.IsAny<DateTime>(),
                    It.IsAny<Granularity>(),
                    null))
                .ReturnsAsync(emptyReport);

            var controller = new RequestReportController(_mockService.Object);

            // Act
            var result = await controller.GetReportData("7d");

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var returnedReport = Assert.IsType<RequestReportDto>(okResult.Value);
            Assert.Equal(0, returnedReport.TotalRequests);
            Assert.Empty(returnedReport.TimeSlots);

            _output.WriteLine("Empty data returns OK (200) with zero counts");
        }

        [Fact]
        public async Task GetReportData_MultipleCallsSamePeriod_CallsServiceMultipleTimes()
        {
            // Arrange - Controller doesn't cache, service does
            var report = new RequestReportDto { TotalRequests = 100 };
            
            _mockService
                .Setup(s => s.GetReportAsync(
                    It.IsAny<DateTime>(),
                    It.IsAny<DateTime>(),
                    It.IsAny<Granularity>(),
                    null))
                .ReturnsAsync(report);

            var controller = new RequestReportController(_mockService.Object);

            // Act
            await controller.GetReportData("7d");
            await controller.GetReportData("7d");
            await controller.GetReportData("7d");

            // Assert
            _mockService.Verify(s => s.GetReportAsync(
                It.IsAny<DateTime>(),
                It.IsAny<DateTime>(),
                It.IsAny<Granularity>(),
                null), Times.Exactly(3));

            _output.WriteLine("Controller calls service each time (service handles caching)");
        }

        #endregion

        #region Response Content Validation

        [Fact]
        public async Task GetReportData_ResponseHasCorrectStructure()
        {
            // Arrange
            var expectedReport = new RequestReportDto
            {
                TotalRequests = 1500,
                SuccessRequests = 1000,
                ClientErrorRequests = 300,
                ServerErrorRequests = 200,
                OtherRequests = 0,
                TimeSlots = new List<TimeSlotData>
                {
                    new TimeSlotData 
                    { 
                        Label = "2025-11-01",
                        SuccessCount = 500,
                        ClientErrorCount = 150,
                        ServerErrorCount = 100,
                        OtherCount = 0
                    }
                },
                TimeFormat = "yyyy-MM-dd"
            };

            _mockService
                .Setup(s => s.GetReportAsync(
                    It.IsAny<DateTime>(),
                    It.IsAny<DateTime>(),
                    It.IsAny<Granularity>(),
                    null))
                .ReturnsAsync(expectedReport);

            var controller = new RequestReportController(_mockService.Object);

            // Act
            var result = await controller.GetReportData("7d");

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var report = Assert.IsType<RequestReportDto>(okResult.Value);
            
            // Verify structure
            Assert.NotNull(report);
            Assert.True(report.TotalRequests > 0);
            Assert.NotNull(report.TimeSlots);
            Assert.NotEmpty(report.TimeSlots);
            Assert.NotNull(report.TimeFormat);
            
            // Verify data consistency
            Assert.Equal(
                report.TotalRequests,
                report.SuccessRequests + report.ClientErrorRequests + report.ServerErrorRequests + report.OtherRequests);

            _output.WriteLine("Response structure is valid and consistent");
        }

        [Fact]
        public async Task GetReportData_TimeSlots_HaveRequiredFields()
        {
            // Arrange
            var expectedReport = new RequestReportDto
            {
                TotalRequests = 100,
                TimeSlots = new List<TimeSlotData>
                {
                    new TimeSlotData 
                    { 
                        Label = "2025-11-01",
                        SuccessCount = 50,
                        ClientErrorCount = 25,
                        ServerErrorCount = 20,
                        OtherCount = 5
                    }
                },
                TimeFormat = "yyyy-MM-dd"
            };

            _mockService
                .Setup(s => s.GetReportAsync(
                    It.IsAny<DateTime>(),
                    It.IsAny<DateTime>(),
                    It.IsAny<Granularity>(),
                    null))
                .ReturnsAsync(expectedReport);

            var controller = new RequestReportController(_mockService.Object);

            // Act
            var result = await controller.GetReportData("1d");

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var report = Assert.IsType<RequestReportDto>(okResult.Value);
            
            foreach (var slot in report.TimeSlots)
            {
                Assert.NotNull(slot.Label);
                Assert.True(slot.SuccessCount >= 0);
                Assert.True(slot.ClientErrorCount >= 0);
                Assert.True(slot.ServerErrorCount >= 0);
                Assert.True(slot.OtherCount >= 0);
            }

            _output.WriteLine("All time slots have required fields with valid values");
        }

        #endregion

        #region Date Range Validation

        [Fact]
        public async Task GetReportData_7Days_CalculatesCorrectDateRange()
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
            Assert.True(daysDiff >= 6.9 && daysDiff <= 7.1, $"Expected ~7 days, got {daysDiff}");
            Assert.True(capturedTo.Value > capturedFrom.Value);

            _output.WriteLine($"Date range: {capturedFrom:yyyy-MM-dd} to {capturedTo:yyyy-MM-dd} ({daysDiff:F1} days)");
        }

        [Theory]
        [InlineData("1m", 28, 32)] // ~30 days
        [InlineData("3m", 85, 95)] // ~90 days
        [InlineData("12m", 360, 370)] // ~365 days
        public async Task GetReportData_VariousPeriods_CalculateCorrectDateRanges(string period, double minDays, double maxDays)
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
            await controller.GetReportData(period);

            // Assert
            Assert.NotNull(capturedFrom);
            Assert.NotNull(capturedTo);
            
            var daysDiff = (capturedTo.Value - capturedFrom.Value).TotalDays;
            Assert.True(daysDiff >= minDays && daysDiff <= maxDays, 
                $"Period {period}: Expected {minDays}-{maxDays} days, got {daysDiff:F1}");

            _output.WriteLine($"Period '{period}': {daysDiff:F1} days (expected {minDays}-{maxDays})");
        }

        #endregion

        #region Granularity Mapping

        [Theory]
        [InlineData("1d", Granularity.Hour)]
        [InlineData("7d", Granularity.Day)]
        [InlineData("1m", Granularity.Day)]
        [InlineData("3m", Granularity.Month)]
        [InlineData("9m", Granularity.Month)]
        [InlineData("12m", Granularity.Month)]
        public async Task GetReportData_MapsPeriodsToCorrectGranularity(string period, Granularity expectedGranularity)
        {
            // Arrange
            Granularity? capturedGranularity = null;

            _mockService
                .Setup(s => s.GetReportAsync(
                    It.IsAny<DateTime>(),
                    It.IsAny<DateTime>(),
                    It.IsAny<Granularity>(),
                    null))
                .Callback<DateTime, DateTime, Granularity, ReportFilter>((from, to, gran, filter) =>
                {
                    capturedGranularity = gran;
                })
                .ReturnsAsync(new RequestReportDto());

            var controller = new RequestReportController(_mockService.Object);

            // Act
            await controller.GetReportData(period);

            // Assert
            Assert.NotNull(capturedGranularity);
            Assert.Equal(expectedGranularity, capturedGranularity.Value);

            _output.WriteLine($"Period '{period}' correctly maps to {expectedGranularity}");
        }

        #endregion

        #region Error Handling

        [Fact]
        public async Task GetReportData_ServiceThrowsException_PropagatesException()
        {
            // Arrange
            _mockService
                .Setup(s => s.GetReportAsync(
                    It.IsAny<DateTime>(),
                    It.IsAny<DateTime>(),
                    It.IsAny<Granularity>(),
                    null))
                .ThrowsAsync(new InvalidOperationException("Database connection failed"));

            var controller = new RequestReportController(_mockService.Object);

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(
                async () => await controller.GetReportData("7d"));

            _output.WriteLine("Service exceptions are propagated to caller");
        }

        [Fact]
        public async Task GetReportData_ServiceReturnsNull_HandlesGracefully()
        {
            // Arrange
            _mockService
                .Setup(s => s.GetReportAsync(
                    It.IsAny<DateTime>(),
                    It.IsAny<DateTime>(),
                    It.IsAny<Granularity>(),
                    null))
                .ReturnsAsync((RequestReportDto?)null);

            var controller = new RequestReportController(_mockService.Object);

            // Act
            var result = await controller.GetReportData("7d");

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Null(okResult.Value);

            _output.WriteLine("Null result from service is handled (returns 200 with null)");
        }

        #endregion

        #region Performance Characteristics

        [Fact]
        public async Task GetReportData_MultipleSequentialCalls_ExecuteIndependently()
        {
            // Arrange
            var callCount = 0;
            _mockService
                .Setup(s => s.GetReportAsync(
                    It.IsAny<DateTime>(),
                    It.IsAny<DateTime>(),
                    It.IsAny<Granularity>(),
                    null))
                .ReturnsAsync(() =>
                {
                    callCount++;
                    return new RequestReportDto { TotalRequests = callCount * 100 };
                });

            var controller = new RequestReportController(_mockService.Object);

            // Act
            var result1 = await controller.GetReportData("7d");
            var result2 = await controller.GetReportData("7d");
            var result3 = await controller.GetReportData("7d");

            // Assert
            var okResult1 = Assert.IsType<OkObjectResult>(result1);
            var okResult2 = Assert.IsType<OkObjectResult>(result2);
            var okResult3 = Assert.IsType<OkObjectResult>(result3);

            var report1 = Assert.IsType<RequestReportDto>(okResult1.Value);
            var report2 = Assert.IsType<RequestReportDto>(okResult2.Value);
            var report3 = Assert.IsType<RequestReportDto>(okResult3.Value);

            Assert.Equal(100, report1.TotalRequests);
            Assert.Equal(200, report2.TotalRequests);
            Assert.Equal(300, report3.TotalRequests);
            Assert.Equal(3, callCount);

            _output.WriteLine("Each API call executes independently (service handles caching)");
        }

        [Fact]
        public async Task GetReportData_DifferentPeriods_CallServiceWithDifferentParameters()
        {
            // Arrange
            var capturedGranularities = new List<Granularity>();
            
            _mockService
                .Setup(s => s.GetReportAsync(
                    It.IsAny<DateTime>(),
                    It.IsAny<DateTime>(),
                    It.IsAny<Granularity>(),
                    null))
                .Callback<DateTime, DateTime, Granularity, ReportFilter>((from, to, gran, filter) =>
                {
                    capturedGranularities.Add(gran);
                })
                .ReturnsAsync(new RequestReportDto());

            var controller = new RequestReportController(_mockService.Object);

            // Act
            await controller.GetReportData("1d");
            await controller.GetReportData("7d");
            await controller.GetReportData("3m");

            // Assert
            Assert.Equal(3, capturedGranularities.Count);
            Assert.Contains(Granularity.Hour, capturedGranularities);
            Assert.Contains(Granularity.Day, capturedGranularities);
            Assert.Contains(Granularity.Month, capturedGranularities);

            _output.WriteLine("Different periods result in different granularities");
        }

        #endregion
    }
}
