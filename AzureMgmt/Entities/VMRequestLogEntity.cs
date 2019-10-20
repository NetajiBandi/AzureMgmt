using Microsoft.Azure.Cosmos.Table;

namespace AzureMgmt.Entities
{
    public class VMRequestLogEntity : TableEntity
    {
        public VMRequestLogEntity()
        {
        }

        public VMRequestLogEntity(string vmName, string vmSize)
        {
            PartitionKey = vmName;
            RowKey = vmSize;
        }

        public string VMName { get; set; }

        public string VMSize { get; set; }

        public string ErrorMessage { get; set; }
    }
}
