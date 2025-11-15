using Ce.Gateway.Api.Data;
using Ce.Gateway.Api.Entities;
using Ce.Gateway.Api.Services;
using Ce.Gateway.Api.Tests.TestHelpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Ce.Gateway.Api.Tests.Services
{
    /// <summary>
    /// Unit tests for UpdateService
    /// </summary>
    public class UpdateServiceTests : IDisposable
    {
        private readonly IDbContextFactory<GatewayDbContext> _dbContextFactory;
        private readonly Mock<IConfiguration> _mockConfiguration;
        private readonly Mock<ILogger<UpdateService>> _mockLogger;
        private readonly Mock<IHostApplicationLifetime> _mockApplicationLifetime;
        private readonly Mock<IHttpClientFactory> _mockHttpClientFactory;
        private readonly string _testDirectory;

        public UpdateServiceTests()
        {
            // Create test database
            var options = new DbContextOptionsBuilder<GatewayDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _dbContextFactory = new TestDbContextFactory(options);

            // Setup mocks
            _mockConfiguration = new Mock<IConfiguration>();
            _mockLogger = new Mock<ILogger<UpdateService>>();
            _mockApplicationLifetime = new Mock<IHostApplicationLifetime>();
            _mockHttpClientFactory = new Mock<IHttpClientFactory>();

            // Setup test directory
            _testDirectory = Path.Combine(Path.GetTempPath(), $"UpdateServiceTests_{Guid.NewGuid()}");
            Directory.CreateDirectory(_testDirectory);

            // Setup configuration
            SetupConfiguration();
        }

        public void Dispose()
        {
            // Cleanup test directory
            if (Directory.Exists(_testDirectory))
            {
                try
                {
                    Directory.Delete(_testDirectory, true);
                }
                catch
                {
                    // Ignore cleanup errors
                }
            }
        }

        private void SetupConfiguration()
        {
            var configSection = new Mock<IConfigurationSection>();

            _mockConfiguration.Setup(c => c.GetSection("Update")).Returns(configSection.Object);
            _mockConfiguration.Setup(c => c["Update:GitHubOwner"]).Returns("test-owner");
            _mockConfiguration.Setup(c => c["Update:GitHubRepo"]).Returns("test-repo");
            _mockConfiguration.Setup(c => c["Update:UpdateUrl"]).Returns("");
            _mockConfiguration.Setup(c => c["Update:AutoCheckEnabled"]).Returns("false");
            _mockConfiguration.Setup(c => c["Update:AutoCheckIntervalHours"]).Returns("24");
            _mockConfiguration.Setup(c => c["Update:AutoDownloadEnabled"]).Returns("false");
            _mockConfiguration.Setup(c => c["Update:AutoInstallEnabled"]).Returns("false");
            _mockConfiguration.Setup(c => c["Update:UpdatesDirectory"]).Returns(Path.Combine(_testDirectory, "updates"));
            _mockConfiguration.Setup(c => c["Update:BackupsDirectory"]).Returns(Path.Combine(_testDirectory, "backups"));
            _mockConfiguration.Setup(c => c["Update:MaxBackupsToKeep"]).Returns("5");
        }

        [Fact]
        public void GetCurrentVersion_ReturnsVersion()
        {
            // Arrange
            var service = CreateService();

            // Act
            var version = service.GetCurrentVersion();

            // Assert
            Assert.NotNull(version);
            Assert.NotEmpty(version);
            Assert.Matches(@"^\d+\.\d+\.\d+$", version);
        }

        [Fact]
        public async Task UploadUpdateAsync_WithValidZip_CreatesUpdateRecord()
        {
            // Arrange
            var service = CreateService();
            var fileName = "test-v1.0.0-update.zip";
            var fileContent = new byte[] { 0x50, 0x4B, 0x03, 0x04 }; // ZIP file header
            using var stream = new MemoryStream(fileContent);

            // Act
            var result = await service.UploadUpdateAsync(fileName, stream, "testuser", CancellationToken.None);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("1.0.0", result.Version);
            Assert.Equal(fileName, result.FileName);
            Assert.Equal("testuser", result.InitiatedBy);
            Assert.Equal(UpdateStatus.Downloaded, result.Status);

            // Verify file was saved
            var filePath = Path.Combine(_testDirectory, "updates", fileName);
            Assert.True(File.Exists(filePath));
        }

        [Fact]
        public async Task UploadUpdateAsync_WithNullStream_ThrowsArgumentNullException()
        {
            // Arrange
            var service = CreateService();

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(() =>
                service.UploadUpdateAsync("test.zip", null!, "testuser", CancellationToken.None));
        }

        [Fact]
        public async Task UploadUpdateAsync_WithEmptyFileName_ThrowsArgumentException()
        {
            // Arrange
            var service = CreateService();
            using var stream = new MemoryStream(new byte[] { 1, 2, 3 });

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() =>
                service.UploadUpdateAsync("", stream, "testuser", CancellationToken.None));
        }

        [Fact]
        public async Task GetUpdateHistoryAsync_ReturnsAllUpdates()
        {
            // Arrange
            var service = CreateService();
            await using var context = await _dbContextFactory.CreateDbContextAsync();

            // Add test updates
            context.SystemUpdates.AddRange(
                new SystemUpdate { Version = "1.0.0", Status = UpdateStatus.Installed, CreatedAt = DateTime.UtcNow.AddDays(-2) },
                new SystemUpdate { Version = "1.0.1", Status = UpdateStatus.Downloaded, CreatedAt = DateTime.UtcNow.AddDays(-1) },
                new SystemUpdate { Version = "1.0.2", Status = UpdateStatus.Failed, CreatedAt = DateTime.UtcNow }
            );
            await context.SaveChangesAsync();

            // Act
            var updates = await service.GetUpdateHistoryAsync(CancellationToken.None);

            // Assert
            Assert.NotNull(updates);
            Assert.Equal(3, updates.Count);
            // Should be ordered by CreatedAt descending
            Assert.Equal("1.0.2", updates[0].Version);
            Assert.Equal("1.0.1", updates[1].Version);
            Assert.Equal("1.0.0", updates[2].Version);
        }

        [Fact]
        public async Task GetUpdateByIdAsync_WithValidId_ReturnsUpdate()
        {
            // Arrange
            var service = CreateService();
            await using var context = await _dbContextFactory.CreateDbContextAsync();

            var update = new SystemUpdate { Version = "1.0.0", Status = UpdateStatus.Downloaded };
            context.SystemUpdates.Add(update);
            await context.SaveChangesAsync();

            // Act
            var result = await service.GetUpdateByIdAsync(update.Id, CancellationToken.None);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(update.Id, result.Id);
            Assert.Equal("1.0.0", result.Version);
        }

        [Fact]
        public async Task GetUpdateByIdAsync_WithInvalidId_ReturnsNull()
        {
            // Arrange
            var service = CreateService();

            // Act
            var result = await service.GetUpdateByIdAsync(999, CancellationToken.None);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task DeleteUpdateAsync_WithValidId_DeletesUpdate()
        {
            // Arrange
            var service = CreateService();
            await using var context = await _dbContextFactory.CreateDbContextAsync();

            var update = new SystemUpdate 
            { 
                Version = "1.0.0", 
                Status = UpdateStatus.Downloaded,
                IsCurrentVersion = false 
            };
            context.SystemUpdates.Add(update);
            await context.SaveChangesAsync();

            // Act
            var result = await service.DeleteUpdateAsync(update.Id, CancellationToken.None);

            // Assert
            Assert.True(result);

            // Verify deletion
            var deletedUpdate = await context.SystemUpdates.FindAsync(update.Id);
            Assert.Null(deletedUpdate);
        }

        [Fact]
        public async Task DeleteUpdateAsync_WithCurrentVersion_ReturnsFalse()
        {
            // Arrange
            var service = CreateService();
            await using var context = await _dbContextFactory.CreateDbContextAsync();

            var update = new SystemUpdate 
            { 
                Version = "1.0.0", 
                Status = UpdateStatus.Installed,
                IsCurrentVersion = true 
            };
            context.SystemUpdates.Add(update);
            await context.SaveChangesAsync();

            // Act
            var result = await service.DeleteUpdateAsync(update.Id, CancellationToken.None);

            // Assert
            Assert.False(result);

            // Verify not deleted
            var existingUpdate = await context.SystemUpdates.FindAsync(update.Id);
            Assert.NotNull(existingUpdate);
        }

        [Fact]
        public async Task DeleteUpdateAsync_WithInvalidId_ReturnsFalse()
        {
            // Arrange
            var service = CreateService();

            // Act
            var result = await service.DeleteUpdateAsync(999, CancellationToken.None);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task ApplyUpdateAsync_WithoutDownloadedUpdate_ReturnsFailure()
        {
            // Arrange
            var service = CreateService();
            await using var context = await _dbContextFactory.CreateDbContextAsync();

            var update = new SystemUpdate 
            { 
                Version = "1.0.0", 
                Status = UpdateStatus.Pending 
            };
            context.SystemUpdates.Add(update);
            await context.SaveChangesAsync();

            // Act
            var result = await service.ApplyUpdateAsync(update.Id, true, "testuser", CancellationToken.None);

            // Assert
            Assert.False(result.Success);
            Assert.Contains("not ready for installation", result.Message);
        }

        [Fact]
        public async Task ApplyUpdateAsync_WithMissingFile_ReturnsFailure()
        {
            // Arrange
            var service = CreateService();
            await using var context = await _dbContextFactory.CreateDbContextAsync();

            var update = new SystemUpdate 
            { 
                Version = "1.0.0", 
                Status = UpdateStatus.Downloaded,
                FilePath = "/nonexistent/file.zip"
            };
            context.SystemUpdates.Add(update);
            await context.SaveChangesAsync();

            // Act
            var result = await service.ApplyUpdateAsync(update.Id, true, "testuser", CancellationToken.None);

            // Assert
            Assert.False(result.Success);
            Assert.Contains("Update file not found", result.Message);
        }

        [Fact]
        public async Task CheckForUpdatesAsync_ReturnsCurrentVersion()
        {
            // Arrange
            var service = CreateService();

            // Act
            var result = await service.CheckForUpdatesAsync(CancellationToken.None);

            // Assert
            Assert.NotNull(result);
            Assert.NotEmpty(result.CurrentVersion);
            Assert.True(result.CheckedAt > DateTime.MinValue);
        }

        private UpdateService CreateService()
        {
            return new UpdateService(
                _dbContextFactory,
                _mockConfiguration.Object,
                _mockLogger.Object,
                _mockApplicationLifetime.Object,
                _mockHttpClientFactory.Object
            );
        }

        // Helper class to create DbContext from DbContextOptions
        private class TestDbContextFactory : IDbContextFactory<GatewayDbContext>
        {
            private readonly DbContextOptions<GatewayDbContext> _options;

            public TestDbContextFactory(DbContextOptions<GatewayDbContext> options)
            {
                _options = options;
            }

            public GatewayDbContext CreateDbContext()
            {
                return new GatewayDbContext(_options);
            }

            public Task<GatewayDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default)
            {
                return Task.FromResult(new GatewayDbContext(_options));
            }
        }
    }
}
