using Microsoft.WindowsAzure.Storage.Table;
using System;
using Microsoft.WindowsAzure.Storage;
namespace AdminToolsConsoleApp
{
    public class AlertEmailEntity : TableEntity
    {
        public AlertEmailEntity()
        {
            PartitionKey = "AlertEmail";
            RowKey = Guid.NewGuid().ToString();
        }
        public AlertEmailEntity(string email) : this()
        {
            Email = email;
        }
        public string Email { get; set; }
        public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
        public bool IsActive { get; set; } = true;
    }
}









