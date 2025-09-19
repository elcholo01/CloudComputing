using Microsoft.WindowsAzure.Storage.Table;
using System;
using Microsoft.WindowsAzure.Storage;
namespace AdminToolsConsoleApp
{
    public class UserEntity : TableEntity
    {
        public UserEntity()
        {
            PartitionKey = "User";
        }
        public UserEntity(string email) : this()
        {
            RowKey = email;
        }
        public string FullName { get; set; }
        public string Gender { get; set; }
        public string Country { get; set; }
        public string City { get; set; }
        public string Address { get; set; }
        public string ProfileImageUrl { get; set; }
        public bool IsAuthorVerified { get; set; }
        public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
    }
}









