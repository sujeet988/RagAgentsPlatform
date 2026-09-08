using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RagAgents.Core.Models;

namespace RagAgents.Core.Interfaces
{
    public interface IDocumentIngestService
    {
        // Ingest PDF -> extract text -> split -> generate embeddings -> index
        Task<DocumentIngestResult> IngestAsync(DocumentIngestRequest request, CancellationToken cancellationToken = default);

        Task<DocumentIngestResult> IngestAsync(Stream pdf, string fileName, CancellationToken cancellationToken = default)
            => IngestAsync(new DocumentIngestRequest(pdf, fileName), cancellationToken);
    }
}
