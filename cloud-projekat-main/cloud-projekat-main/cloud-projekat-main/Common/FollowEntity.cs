using Microsoft.WindowsAzure.Storage.Table;
using System;
using Microsoft.WindowsAzure.Storage;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common
{
    public class FollowEntity : TableEntity
    {
        public string UserEmail { get; set; }

        public FollowEntity(string discussionId, string userEmail)
        {
            PartitionKey = discussionId;
            RowKey = userEmail?.ToLowerInvariant();
            UserEmail = userEmail?.ToLowerInvariant();
        }

        public FollowEntity() { } // required by Table SDK
    }
}










