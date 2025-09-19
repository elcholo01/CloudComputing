

using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.WindowsAzure.Storage.Queue;
using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common
{
    public static class Storage
    {
        // Table names
        public const string USERS_TABLE = "users";
        public const string DISCUSSIONS_TABLE = "discussions";
        public const string COMMENTS_TABLE = "comments";
        public const string VOTES_TABLE = "votes";
        public const string FOLLOWS_TABLE = "follows";
        public const string HEALTH_CHECK_TABLE = "healthcheck";
        public const string ALERT_EMAILS_TABLE = "alertemails";
        public const string NOTIFICATION_LOG_TABLE = "notificationlog";

        // Queue names
        public const string NOTIFICATIONS_QUEUE = "notifications";

        // Container names
        public const string IMAGES_CONTAINER = "images";
        private static string ResolveConnString()
        {
            // 1) ConfigurationManager (radi i u web.config i u .cscfg)
            var cs = ConfigurationManager.AppSettings["DataConnectionString"];

            // 2) Web.config appSettings fallback
            if (string.IsNullOrWhiteSpace(cs))
                cs = "UseDevelopmentStorage=true";

            // 3) Env var (ako je koristi)
            if (string.IsNullOrWhiteSpace(cs))
                cs = Environment.GetEnvironmentVariable("AZURE_STORAGE_CONNECTION_STRING");

            // 4) Safe default za lokalni rad (Azurite / Storage Emulator)
            if (string.IsNullOrWhiteSpace(cs))
                cs = "UseDevelopmentStorage=true";

            return cs;
        }

        public static CloudStorageAccount Account =>
            CloudStorageAccount.Parse(ResolveConnString());

        public static CloudTable GetTable(string name)
        {
            var client = Account.CreateCloudTableClient();
            var t = client.GetTableReference(name);
            t.CreateIfNotExists();
            return t;
        }

        public static CloudQueue GetQueue(string name)
        {
            var client = Account.CreateCloudQueueClient();
            var q = client.GetQueueReference(name);
            q.CreateIfNotExists();
            return q;
        }

        public static CloudBlobContainer GetContainer(string name)
        {
            var client = Account.CreateCloudBlobClient();
            var container = client.GetContainerReference(name);

            if (container.CreateIfNotExists())
            {
                var permissions = new BlobContainerPermissions
                {
                    PublicAccess = BlobContainerPublicAccessType.Blob
                };
                container.SetPermissions(permissions);
            }

            return container;
        }
    }
}












