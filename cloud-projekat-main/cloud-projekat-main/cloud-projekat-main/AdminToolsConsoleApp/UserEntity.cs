using Microsoft.WindowsAzure.Storage.Table;
using System;

namespace AdminToolsConsoleApp
{
    public class UserEntity : TableEntity
    {
        public string FullName { get; set; }
        public string Gender { get; set; }      // optional: "M","F","Other"
        public string Country { get; set; }
        public string City { get; set; }
        public string Address { get; set; }
        public string PasswordHash { get; set; }
        public string PhotoUrl { get; set; }
        public bool IsAuthorVerified { get; set; }

        public UserEntity(string email)
        {
            PartitionKey = "User";
            RowKey = email?.ToLowerInvariant();
        }

        public UserEntity() { } // required by Table SDK
    }
}
