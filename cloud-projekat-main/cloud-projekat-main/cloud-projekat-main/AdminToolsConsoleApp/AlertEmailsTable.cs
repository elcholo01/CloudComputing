using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AdminToolsConsoleApp
{
    public class AlertEmailsTable
    {
        private readonly CloudTable _table;
        
        public AlertEmailsTable()
        {
            var storageAccount = CloudStorageAccount.Parse("UseDevelopmentStorage=true");
            var tableClient = storageAccount.CreateCloudTableClient();
            _table = tableClient.GetTableReference("AlertEmails");
            _table.CreateIfNotExists();
        }
        
        public void Add(string emailLower)
        {
            _table.Execute(TableOperation.InsertOrReplace(new AlertEmailEntity(emailLower)));
        }
        
        public void Remove(string emailLower)
        {
            var res = _table.Execute(TableOperation.Retrieve<AlertEmailEntity>("AlertEmails", emailLower));
            if (res.Result is AlertEmailEntity e)
            {
                // Postavi ETag na "*" za brisanje
                e.ETag = "*";
                _table.Execute(TableOperation.Delete(e));
            }
        }
    }
}










