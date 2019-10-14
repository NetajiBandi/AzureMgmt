using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Management.Compute.Fluent;
using Microsoft.Azure.Management.Compute.Fluent.Models;
using Microsoft.Azure.Management.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent.Core;

namespace AzureMgmt.Controllers
{
    [Route("api/[controller]")]
    public class AzureMgmtController : Controller
    {
        private readonly IWebHostEnvironment _webHostEnvironment;

        public AzureMgmtController(IWebHostEnvironment webHostEnvironment)
        {
            _webHostEnvironment = webHostEnvironment;
        }

        private static string[] Summaries = new[]
        {
            "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
        };

        [HttpGet("[action]")]
        public IEnumerable<WeatherForecast> WeatherForecasts()
        {
            var rng = new Random();
            return Enumerable.Range(1, 5).Select(index => new WeatherForecast
            {
                DateFormatted = DateTime.Now.AddDays(index).ToString("d"),
                TemperatureC = rng.Next(-20, 55),
                Summary = Summaries[rng.Next(Summaries.Length)]
            });
        }

        [HttpGet("[action]")]
        public void CreateVM()
        {
            try
            {
                string contentRootPath = _webHostEnvironment.ContentRootPath;

                var credentials = SdkContext.AzureCredentialsFactory.FromFile(contentRootPath + "\\azureauth.properties");

                var azure = Azure
                    .Configure()
                    .WithLogLevel(HttpLoggingDelegatingHandler.Level.Basic)
                    .Authenticate(credentials)
                    .WithDefaultSubscription();

                var location = Region.USWest;
                var vmName = "AzureMgmtFirstVM";
                var groupName = "AzureMgmtResourceGroup";

                var resourceGroup = azure.ResourceGroups.Define(groupName)
                    .WithRegion(location)
                    .Create();

                var availabilitySet = azure.AvailabilitySets.Define("AzureMgmtAVSet")
                    .WithRegion(location)
                    .WithExistingResourceGroup(groupName)
                    .WithSku(AvailabilitySetSkuTypes.Aligned)
                    .Create();

                var publicIPAddress = azure.PublicIPAddresses.Define("AzureMgmtPublicIP")
                    .WithRegion(location)
                    .WithExistingResourceGroup(groupName)
                    .WithDynamicIP()
                    .Create();

                var network = azure.Networks.Define("AzureMgmtVNet")
                    .WithRegion(location)
                    .WithExistingResourceGroup(groupName)
                    .WithAddressSpace("10.0.0.0/16")
                    .WithSubnet("mySubnet", "10.0.0.0/24")
                    .Create();

                var networkInterface = azure.NetworkInterfaces.Define("AzureMgmtNIC")
                    .WithRegion(location)
                    .WithExistingResourceGroup(groupName)
                    .WithExistingPrimaryNetwork(network)
                    .WithSubnet("AzureMgmtSubnet")
                    .WithPrimaryPrivateIPAddressDynamic()
                    .WithExistingPrimaryPublicIPAddress(publicIPAddress)
                    .Create();

                azure.VirtualMachines.Define(vmName)
                    .WithRegion(location)
                    .WithExistingResourceGroup(groupName)
                    .WithExistingPrimaryNetworkInterface(networkInterface)
                    .WithLatestWindowsImage("MicrosoftWindowsServer", "WindowsServer", "2012-R2-Datacenter")
                    .WithAdminUsername("dumpatipavankumar@gmail.com")
                    .WithAdminPassword("Airforce@22")
                    .WithComputerName(vmName)
                    .WithExistingAvailabilitySet(availabilitySet)
                    .WithSize(VirtualMachineSizeTypes.StandardB1s)
                    .Create();

            }
            catch (Exception ex)
            {
                throw;
            }
        }

        public class WeatherForecast
        {
            public string DateFormatted { get; set; }
            public int TemperatureC { get; set; }
            public string Summary { get; set; }

            public int TemperatureF
            {
                get
                {
                    return 32 + (int)(TemperatureC / 0.5556);
                }
            }
        }
    }
}
