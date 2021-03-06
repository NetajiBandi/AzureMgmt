﻿using AzureMgmt.Entities;
using Microsoft.Azure.Cosmos.Table;
using System;
using System.Collections.Generic;
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
                await table.CreateIfNotExistsAsync();

                // Create the InsertOrMerge table operation
                var insertOrMergeOperation = TableOperation.InsertOrMerge(entity);

                // Execute the operation.
                var result = await table.ExecuteAsync(insertOrMergeOperation);
                var insertedVMRequestLogEntity = result.Result as VMRequestLogEntity;

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

        /// <summary>
        /// Gets all VMs asynchronous.
        /// </summary>
        /// <param name="table">The table.</param>
        /// <returns>Gets all VMs</returns>
        public static async Task<IEnumerable<VMRequestLogEntity>> GetAllVMsAsync(CloudTable table)
        {
            return await table.ExecuteQuerySegmentedAsync(new TableQuery<VMRequestLogEntity>(), null);
        }
    }
}
