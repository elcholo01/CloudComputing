using Microsoft.WindowsAzure.Storage.Table;

namespace Common
{
    public class AlertEmailEntity : TableEntity
    {
        public string Email { get; set; }

        public AlertEmailEntity(string email)
        {
            PartitionKey = "AlertEmails";
            RowKey = email.ToLowerInvariant();
            Email = email;
        }

        public AlertEmailEntity() { }
    }
}