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

        [HttpGet("[action]")]
        public async Task<IActionResult> CreateVM()
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
                var vmName = "FirstVM";
                var groupName = "AzureMgmtResourceGroup";

                var resourceGroup = await azure.ResourceGroups.Define(groupName)
                    .WithRegion(location)
                    .CreateAsync();

                var availabilitySet = await azure.AvailabilitySets.Define("AzureMgmtAVSet")
                    .WithRegion(location)
                    .WithExistingResourceGroup(groupName)
                    .WithSku(AvailabilitySetSkuTypes.Aligned)
                    .CreateAsync();

                var publicIPAddress = await azure.PublicIPAddresses.Define("AzureMgmtPublicIP")
                    .WithRegion(location)
                    .WithExistingResourceGroup(groupName)
                    .WithDynamicIP()
                    .CreateAsync();

                var network = await azure.Networks.Define("AzureMgmtVNet")
                    .WithRegion(location)
                    .WithExistingResourceGroup(groupName)
                    .WithAddressSpace("10.0.0.0/16")
                    .WithSubnet("AzureMgmtSubnet", "10.0.0.0/24")
                    .CreateAsync();

                var networkInterface = await azure.NetworkInterfaces.Define("AzureMgmtNIC")
                    .WithRegion(location)
                    .WithExistingResourceGroup(groupName)
                    .WithExistingPrimaryNetwork(network)
                    .WithSubnet("AzureMgmtSubnet")
                    .WithPrimaryPrivateIPAddressDynamic()
                    .WithExistingPrimaryPublicIPAddress(publicIPAddress)
                    .CreateAsync();

                await azure.VirtualMachines.Define(vmName)
                    .WithRegion(location)
                    .WithExistingResourceGroup(groupName)
                    .WithExistingPrimaryNetworkInterface(networkInterface)
                    .WithLatestWindowsImage("MicrosoftWindowsServer", "WindowsServer", "2012-R2-Datacenter")
                    .WithAdminUsername("dumpatipavankumar")
                    .WithAdminPassword("Airforce@22")
                    .WithComputerName(vmName)
                    .WithExistingAvailabilitySet(availabilitySet)
                    .WithSize(VirtualMachineSizeTypes.StandardD1)
                    .CreateAsync();

                return Ok();
            }
            catch (Exception)
            {
                throw;
            }
        }

    }
}
