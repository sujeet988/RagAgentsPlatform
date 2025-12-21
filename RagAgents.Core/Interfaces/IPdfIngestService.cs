using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RagAgents.Core.Interfaces
{
    public interface IPdfIngestService
    {
        // Ingest PDF -> extract text -> split -> generate embeddings -> index
        Task IngestAsync(Stream pdf, string fileName);
    }
}
