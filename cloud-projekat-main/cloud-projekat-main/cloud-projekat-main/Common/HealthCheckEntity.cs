using Microsoft.WindowsAzure.Storage.Table;
using System;
using Microsoft.WindowsAzure.Storage;
namespace Common
{
    public class HealthCheckEntity : TableEntity
    {
        public HealthCheckEntity()
        {
            PartitionKey = "HealthCheck";
            RowKey = Guid.NewGuid().ToString();
        }
        public HealthCheckEntity(string serviceName, string status) : this()
        {
            ServiceName = serviceName;
            Status = status;
            Timestamp = DateTime.UtcNow;
        }
        public string ServiceName { get; set; }
        public string Status { get; set; } // "OK" ili "NOT_OK"
        public new DateTime Timestamp { get; set; }
    }
}









