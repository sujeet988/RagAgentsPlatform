using System.Threading.Tasks;

namespace RagAgents.Core.Agents
{
    public interface IAgent
    {
        Task<string> RunAsync(string goal, string conversationId = null);
    }
}
