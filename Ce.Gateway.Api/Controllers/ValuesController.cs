using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Ocelot.Configuration.File;

namespace Ce.Gateway.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ValuesController : ControllerBase
    {
        private readonly IConfiguration _configuration;

        public ValuesController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        [HttpGet]
        [Route("get-env")]
        public IActionResult GetEnvironment()
        {
            if (Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") != null)
            {
                return new ObjectResult(Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT"));
            }
            return new ObjectResult("Enviroment variable empty!");
        }

        [HttpGet]
        [Route("get-config-value")]
        public IActionResult GetConfigurationValue(string key)
        {
            var value = _configuration.GetValue(key, "Value is null!");
            return new ObjectResult(value);
        }

        [HttpGet]
        [Route("get-ocelot-config")]
        public IActionResult GetOcelotConfiguration()
        {
            string key = "Routes";
            var value = _configuration.GetSection(key).Get<List<FileRoute>>();
            return new ObjectResult(value);
        }

        [HttpGet]
        [Route("get-ocelot-file-config")]
        public IActionResult GetOcelotFileConfiguration()
        {
            var env = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
            var files = string.IsNullOrEmpty(env)
                ? new DirectoryInfo(Directory.GetCurrentDirectory()).EnumerateFiles()
                    .Where(fi => fi.Name.Contains("configuration.json")).ToList()
                : new DirectoryInfo(Directory.GetCurrentDirectory()).EnumerateFiles()
                    .Where(fi => fi.Name.Contains($"configuration.{env}.json")).ToList();

            Console.WriteLine($"HuyTD: files.Count => {files.Count}");

            var fileConfiguration = new FileConfiguration();

            foreach (var file in files)
            {
                var lines = System.IO.File.ReadAllText(file.FullName);
                var config = JsonConvert.DeserializeObject<FileConfiguration>(lines);

                fileConfiguration.Aggregates.AddRange(config.Aggregates);
                fileConfiguration.Routes.AddRange(config.Routes);
            }

            var json = JsonConvert.SerializeObject(fileConfiguration);

            return new ObjectResult(json);
        }

        [HttpGet]
        [Route("get-serilog-using")]
        public IActionResult GetSerilogUsing()
        {
            var value = _configuration.GetSection("Serilog:Using").Get<List<string>>();
            return new ObjectResult(value);
        }

        [HttpGet]
        [Route("get-serilog-write-to")]
        public IActionResult GetSerilogWriteTo()
        {
            var value = _configuration.GetSection("Serilog:WriteTo").Get<List<WriteToItem>>();
            return new ObjectResult(value);
        }
    }

    public class WriteToItem
    {
        public string Name { get; set; }
        public Args Args { get; set; }
    }

    public class Args
    {
        public string path { get; set; }
        public long fileSizeLimitBytes { get; set; }
        public int retainedFileCountLimit { get; set; }
        public string rollingInterval { get; set; }
        public bool rollOnFileSizeLimit { get; set; }
    }
}
