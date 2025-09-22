using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage.Table;

namespace AdminToolsConsoleApp
{
    public class AuthorVerifier
    {
        private readonly UsersTable _users = new UsersTable();

        public bool Verify(string email)
        {
            var key = email?.ToLowerInvariant();
            var u = _users.GetByEmail(key);
            if (u == null) return false;

            // SIGURNA VERIFIKACIJA - koristi Replace umesto Upsert
            u.IsAuthorVerified = true;
            
            // Koristi direktno Replace umesto Upsert da se osiguraš da se ne gube podaci
            var storage = Storage.GetTable("Users");
            var updateOperation = TableOperation.Replace(u);
            storage.Execute(updateOperation);
            
            return true;
        }
    }
}









