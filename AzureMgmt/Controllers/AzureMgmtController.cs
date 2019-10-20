using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AzureMgmt.Entities;
using AzureMgmt.Helpers;
using AzureMgmt.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Cosmos.Table;
using Microsoft.Azure.Management.Compute.Fluent;
using Microsoft.Azure.Management.Compute.Fluent.Models;
using Microsoft.Azure.Management.Fluent;
using Microsoft.Azure.Management.Network.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent.Authentication;
using Microsoft.Azure.Management.ResourceManager.Fluent.Core;

namespace AzureMgmt.Controllers
{
    [Route("api/[controller]")]
    public class AzureMgmtController : Controller
    {
        /// <summary>
        /// The location
        /// </summary>
        private static readonly Region location;

        /// <summary>
        /// The random
        /// </summary>
        private static readonly Random random;

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
        /// The table
        /// </summary>
        private static readonly CloudTable table;

        /// <summary>
        /// The credentials
        /// </summary>
        private static readonly AzureCredentials credentials;

        /// <summary>
        /// The azure
        /// </summary>
        private static readonly IAzure azure;

        /// <summary>
        /// The resource group
        /// </summary>
        private static readonly IResourceGroup resourceGroup;

        /// <summary>
        /// The availability set
        /// </summary>
        private static readonly IAvailabilitySet availabilitySet;

        /// <summary>
        /// The network
        /// </summary>
        private static readonly INetwork network;

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
            table = tableClient.GetTableReference("VMRequestLog");

            credentials = SdkContext.AzureCredentialsFactory.FromServicePrincipal("86ef4bad-b38b-4c3f-9357-978ab053c831", "UH[7m1l=GL?Iu@si68d4qO]9Dg0NM?N-", "4bc521c7-c8c1-4e45-bfd3-157a81938f71", AzureEnvironment.AzureGlobalCloud);
            azure = Azure.Configure().WithLogLevel(HttpLoggingDelegatingHandler.Level.Basic).Authenticate(credentials).WithDefaultSubscription();
            resourceGroup = azure.ResourceGroups.Define(groupName).WithRegion(location).Create();
            availabilitySet = azure.AvailabilitySets.Define("AzureMgmtAVSet").WithRegion(location).WithExistingResourceGroup(groupName).WithSku(AvailabilitySetSkuTypes.Aligned).Create();
            network = azure.Networks.Define("AzureMgmtVNet").WithRegion(location).WithExistingResourceGroup(groupName).WithAddressSpace("10.0.0.0/16").WithSubnet("AzureMgmtSubnet", "10.0.0.0/24").Create();
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

                    var publicIPAddress = await azure.PublicIPAddresses.Define("AzureMgmtPublicIP" + RandomString(5)).WithRegion(location).WithExistingResourceGroup(groupName).WithDynamicIP().CreateAsync();

                    var networkInterface = await azure.NetworkInterfaces.Define("AzureMgmtNIC" + RandomString(5)).WithRegion(location).WithExistingResourceGroup(groupName).WithExistingPrimaryNetwork(network).WithSubnet("AzureMgmtSubnet").WithPrimaryPrivateIPAddressDynamic().WithExistingPrimaryPublicIPAddress(publicIPAddress).CreateAsync();

                    var size = GetVMSize(vmConfig);
                    await azure.VirtualMachines.Define(vmConfig.VMName)
                            .WithRegion(location)
                            .WithExistingResourceGroup(groupName)
                            .WithExistingPrimaryNetworkInterface(networkInterface)
                            .WithLatestWindowsImage("MicrosoftWindowsServer", "WindowsServer", "2012-R2-Datacenter")
                            .WithAdminUsername("dumpatipavankumar")
                            .WithAdminPassword("Airforce@22")
                            .WithComputerName(vmConfig.VMName)
                            .WithExistingAvailabilitySet(availabilitySet)
                            .WithSize(size)
                            .CreateAsync();
                }
                catch (Exception ex)
                {
                    var message = "Message: " + ex.Message + " InnerException: " + ex.StackTrace.ToString();
                    await SaveRequestLogToDB(vmConfig, message);
                }
            });

            return Ok();
        }

        /// <summary>
        /// Lists the VMs.
        /// </summary>
        /// <returns>Lists the VMs</returns>
        [HttpGet("[action]")]
        public async Task<IEnumerable<VMRequestLogEntity>> ListVM()
        {
            return await CosmosDBStorageHelper.GetAllVMsAsync(table);
        }

        /// <summary>
        /// Saves the request log to database.
        /// </summary>
        /// <param name="vmConfig">The VM configuration.</param>
        private static async Task SaveRequestLogToDB(VMConfig vmConfig, string exceptionMessage = "NA")
        {
            VMRequestLogEntity vmRequestLogEntity = new VMRequestLogEntity(vmConfig.VMName, vmConfig.VMSize)
            {
                VMName = vmConfig.VMName,
                VMSize = vmConfig.VMSize,
                ErrorMessage = exceptionMessage
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
            if (vmConfig.VMSize == "StandardD1")
            {
                size = VirtualMachineSizeTypes.StandardD1;
            }
            else if (vmConfig.VMSize == "StandardD2")
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
