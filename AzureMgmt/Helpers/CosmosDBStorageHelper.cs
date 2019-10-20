using AzureMgmt.Entities;
using Microsoft.Azure.Cosmos.Table;
using System;
using System.Threading.Tasks;

namespace AzureMgmt.Helpers
{
    internal static class CosmosDBStorageHelper
    {
        /// <summary>
        /// Inserts the or merge entity asynchronous.
        /// </summary>
        /// <param name="table">The table.</param>
        /// <param name="entity">The entity.</param>
        /// <returns>The entity</returns>
        public static async Task<VMRequestLogEntity> InsertOrMergeEntityAsync(CloudTable table, VMRequestLogEntity entity)
        {
            if (entity == null)
            {
                throw new ArgumentNullException("entity");
            }

            try
            {
                // Create the Insert table operation
                TableOperation insertOperation = TableOperation.Insert(entity);

                // Execute the operation.
                TableResult result = await table.ExecuteAsync(insertOperation);
                VMRequestLogEntity insertedVMRequestLogEntity = result.Result as VMRequestLogEntity;

                if (result.RequestCharge.HasValue)
                {
                    Console.WriteLine("Request Charge of InsertOrMerge Operation: " + result.RequestCharge);
                }

                return insertedVMRequestLogEntity;
            }
            catch (StorageException)
            {
                throw;
            }
        }
    }
}
