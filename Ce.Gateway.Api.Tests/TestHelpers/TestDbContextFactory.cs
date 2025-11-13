using Ce.Gateway.Api.Data;
using Ce.Gateway.Api.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;

namespace Ce.Gateway.Api.Tests.TestHelpers
{
    public static class TestDbContextFactory
    {
        public static GatewayDbContext CreateInMemoryDbContext()
        {
            var options = new DbContextOptionsBuilder<GatewayDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            return new GatewayDbContext(options);
        }

        public static IConfiguration CreateTestConfiguration()
        {
            var config = new Dictionary<string, string>
            {
                { "Tokens:Key", "ThisIsATestKeyForJWTTokenGenerationWithAtLeast32Characters" },
                { "Tokens:Issuer", "http://test.com" },
                { "Tokens:Audience", "http://test-audience.com" }
            };

            return new ConfigurationBuilder()
                .AddInMemoryCollection(config)
                .Build();
        }
    }
}
