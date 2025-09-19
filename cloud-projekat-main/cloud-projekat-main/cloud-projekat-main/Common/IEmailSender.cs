using System.Collections.Generic;
using System.Threading.Tasks;

namespace Common
{
    public interface IEmailSender
    {
        Task SendAsync(IEnumerable<string> recipients, string subject, string body);
        Task SendAsync(string recipient, string subject, string body);
    }
}









