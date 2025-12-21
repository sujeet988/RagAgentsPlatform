using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RagAgents.Core.Interfaces
{
    public interface IRagService
    {
        Task<string> AskAsync(string question);
        Task AskWithHistoryAsync( string question,string conversationId,string userId,Func<string, Task> onToken);
    }
}
