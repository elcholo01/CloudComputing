using Microsoft.WindowsAzure.Storage.Table;
using System;
using Microsoft.WindowsAzure.Storage;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common
{
    public class NotificationLogEntity : TableEntity
    {
        public int SentCount { get; set; }

        public NotificationLogEntity(string commentId, DateTime timestamp, int sentCount)
        {
            PartitionKey = commentId;
            RowKey = timestamp.ToString("yyyyMMddHHmmss");
            SentCount = sentCount;
            Timestamp = timestamp;
        }

        public NotificationLogEntity() { } // required by Table SDK
    }
}










