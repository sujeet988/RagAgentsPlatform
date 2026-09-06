using RagAgents.Core.Interfaces;

namespace RagAgents.Core.Agents
{
    public class SearchTool : ITool
    {
        private readonly ISearchIndexer _search;
        private readonly IOpenAIEmbeddingService _openAIEmbeddingService;
        public string Name => "Search";

        public SearchTool(ISearchIndexer search, IOpenAIEmbeddingService openAIEmbeddingService)
        {
            _search = search;
            _openAIEmbeddingService = openAIEmbeddingService;
        }

        public async Task<string> RunAsync(string input)
        {
            // Create embedding from the input text using the OpenAI service
            var embedding = await _openAIEmbeddingService.CreateEmbeddingAsync(input);

            // Perform vector search using the generated embedding
            var chunks = await _search.VectorSearchAsync(embedding);

            return string.Join("\n", chunks);
        }
    }
}
