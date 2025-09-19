using Microsoft.WindowsAzure.Storage.Table;
using System;
using Microsoft.WindowsAzure.Storage;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common
{
    public class VoteEntity : TableEntity
    {
        public string VoteType { get; set; } // "positive" ili "negative"

        public VoteEntity(string discussionId, string userEmail)
        {
            PartitionKey = discussionId;
            RowKey = userEmail?.ToLowerInvariant();
        }

        public VoteEntity() { } // required by Table SDK
    }
}










