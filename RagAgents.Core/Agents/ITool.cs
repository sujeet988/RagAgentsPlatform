using System.Threading.Tasks;

namespace RagAgents.Core.Agents
{
    public interface ITool
    {
        string Name { get; }
        Task<string> RunAsync(string input);
    }
}
