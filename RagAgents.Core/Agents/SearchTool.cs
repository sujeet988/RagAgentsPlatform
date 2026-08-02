using RagAgents.Core.Interfaces;

namespace RagAgents.Core.Agents
{
    public class SearchTool : ITool
    {
        private readonly IAzureSearchService _search;
        private readonly IAzureOpenAIService _openAI;
        public string Name => "Search";

        public SearchTool(IAzureSearchService search, IAzureOpenAIService openAI)
        {
            _search = search;
            _openAI = openAI;
        }

        public async Task<string> RunAsync(string input)
        {
            // Create embedding from the input text using the OpenAI service
            var embedding = await _openAI.CreateEmbeddingAsync(input);

            // Perform vector search using the generated embedding
            var chunks = await _search.VectorSearchAsync(embedding);

            return string.Join("\n", chunks);
        }
    }
}
