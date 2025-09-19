﻿using Microsoft.WindowsAzure.Storage.Table;
using System;
using Microsoft.WindowsAzure.Storage;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common
{
    public class AlterEmailEntity
    {
        // Table: AlertEmails
        // PK = "Alert"
        // RK = emailLower
        public class AlertEmailEntity : TableEntity
        {
            public AlertEmailEntity(string emailLower)
            {
                PartitionKey = "Alert";
                RowKey = emailLower;
            }

            public AlertEmailEntity() { }

        }
    }
}










