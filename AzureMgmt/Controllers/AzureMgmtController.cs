using System;
using System.Threading.Tasks;
using AzureMgmt.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
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

        private readonly Region location;

        private readonly string groupName;

        public AzureMgmtController(IWebHostEnvironment webHostEnvironment)
        {
            _webHostEnvironment = webHostEnvironment;
            groupName = "AzureMgmtResourceGroup";
            location = Region.USWest;
        }

        [HttpPost("[action]")]
        public async Task<IActionResult> CreateVM([FromBody]VMConfig vmConfig)
        {
            try
            {
                var credentials = SdkContext.AzureCredentialsFactory.FromFile(_webHostEnvironment.ContentRootPath + "\\azureauth.properties");

                var azure = Azure
                    .Configure()
                    .WithLogLevel(HttpLoggingDelegatingHandler.Level.Basic)
                    .Authenticate(credentials)
                    .WithDefaultSubscription();

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

                VirtualMachineSizeTypes size;
                if (vmConfig.size == "StandardD1")
                {
                    size = VirtualMachineSizeTypes.StandardD1;
                }
                else if(vmConfig.size == "StandardD2")
                {
                    size = VirtualMachineSizeTypes.StandardD2;
                }
                else
                {
                    size = VirtualMachineSizeTypes.StandardD11;
                }

                await azure.VirtualMachines.Define(vmConfig.name)
                    .WithRegion(location)
                    .WithExistingResourceGroup(groupName)
                    .WithExistingPrimaryNetworkInterface(networkInterface)
                    .WithLatestWindowsImage("MicrosoftWindowsServer", "WindowsServer", "2012-R2-Datacenter")
                    .WithAdminUsername("dumpatipavankumar")
                    .WithAdminPassword("Airforce@22")
                    .WithComputerName(vmConfig.name)
                    .WithExistingAvailabilitySet(availabilitySet)
                    .WithSize(size)
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
