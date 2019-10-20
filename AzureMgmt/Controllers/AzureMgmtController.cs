using System;
using System.Linq;
using System.Threading.Tasks;
using AzureMgmt.Entities;
using AzureMgmt.Helpers;
using AzureMgmt.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Cosmos.Table;
using Microsoft.Azure.Management.Compute.Fluent.Models;
using Microsoft.Azure.Management.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent.Core;

namespace AzureMgmt.Controllers
{
    [Route("api/[controller]")]
    public class AzureMgmtController : Controller
    {
        /// <summary>
        /// The web host environment
        /// </summary>
        private readonly IWebHostEnvironment _webHostEnvironment;

        /// <summary>
        /// The location
        /// </summary>
        private static readonly Region location;

        /// <summary>
        /// The random
        /// </summary>
        private static Random random;

        /// <summary>
        /// The group name
        /// </summary>
        private static readonly string groupName;

        /// <summary>
        /// The connection string
        /// </summary>
        private static readonly string connectionString;

        /// <summary>
        /// The storage account
        /// </summary>
        private static readonly CloudStorageAccount storageAccount;

        /// <summary>
        /// The table client
        /// </summary>
        private static readonly CloudTableClient tableClient;

        /// <summary>
        /// Initializes the <see cref="AzureMgmtController"/> class.
        /// </summary>
        static AzureMgmtController()
        {
            random = new Random();
            location = Region.USWest;
            groupName = "AzureMgmtResourceGroup";
            connectionString = "DefaultEndpointsProtocol=https;AccountName=azuremgmtdb;AccountKey=OrRZd2n4uZkdtDLSAIynYp9oZLMsyGF4I7FLwnxOUQ0QRSmMQZhdvOjreGH2tA8sRGehzWcyYg8ExCXBGAa7Zg==;TableEndpoint=https://azuremgmtdb.table.cosmos.azure.com:443/;";

            storageAccount = CloudStorageAccount.Parse(connectionString);
            tableClient = storageAccount.CreateCloudTableClient();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AzureMgmtController"/> class.
        /// </summary>
        /// <param name="webHostEnvironment">The web host environment.</param>
        public AzureMgmtController(IWebHostEnvironment webHostEnvironment)
        {
            _webHostEnvironment = webHostEnvironment;
        }

        /// <summary>
        /// Creates the VM.
        /// </summary>
        /// <param name="vmConfig">The VM configuration.</param>
        [HttpPost("[action]")]
        public IActionResult CreateVM([FromBody]VMConfig vmConfig)
        {
            Task.Run(async () =>
            {
                try
                {
                    await SaveRequestLogToDB(vmConfig);

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

                    var publicIPAddress = await azure.PublicIPAddresses.Define("AzureMgmtPublicIP" + RandomString(5))
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

                    var networkInterface = await azure.NetworkInterfaces.Define("AzureMgmtNIC" + RandomString(5))
                            .WithRegion(location)
                            .WithExistingResourceGroup(groupName)
                            .WithExistingPrimaryNetwork(network)
                            .WithSubnet("AzureMgmtSubnet")
                            .WithPrimaryPrivateIPAddressDynamic()
                            .WithExistingPrimaryPublicIPAddress(publicIPAddress)
                            .CreateAsync();

                    var size = GetVMSize(vmConfig);
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
                }
                catch (Exception ex)
                {
                    throw ex;
                }
            });

            return Ok();
        }

        /// <summary>
        /// Saves the request log to database.
        /// </summary>
        /// <param name="vmConfig">The VM configuration.</param>
        private static async Task SaveRequestLogToDB(VMConfig vmConfig)
        {
            CloudTable table = tableClient.GetTableReference("VMRequestLog");
            await table.CreateIfNotExistsAsync();

            VMRequestLogEntity vmRequestLogEntity = new VMRequestLogEntity(vmConfig.name, vmConfig.size)
            {
                VMName = vmConfig.name,
                VMSize = vmConfig.size
            };

            await CosmosDBStorageHelper.InsertOrMergeEntityAsync(table, vmRequestLogEntity);
        }

        /// <summary>
        /// Gets the size of the VM.
        /// </summary>
        /// <param name="vmConfig">The VM configuration.</param>
        /// <returns>the size of the VM</returns>
        private static VirtualMachineSizeTypes GetVMSize(VMConfig vmConfig)
        {
            VirtualMachineSizeTypes size;
            if (vmConfig.size == "StandardD1")
            {
                size = VirtualMachineSizeTypes.StandardD1;
            }
            else if (vmConfig.size == "StandardD2")
            {
                size = VirtualMachineSizeTypes.StandardD2;
            }
            else
            {
                size = VirtualMachineSizeTypes.StandardD11;
            }

            return size;
        }

        /// <summary>
        /// The random string.
        /// </summary>
        /// <param name="length">The length.</param>
        /// <returns>The random string</returns>
        public static string RandomString(int length)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            return new string(Enumerable.Repeat(chars, length).Select(s => s[random.Next(s.Length)]).ToArray());
        }
    }
}
