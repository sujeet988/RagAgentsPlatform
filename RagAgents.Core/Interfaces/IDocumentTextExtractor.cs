using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RagAgents.Core.Interfaces
{
    public interface IDocumentTextExtractor
    {
        Task<string> ExtractTextAsync(Stream document,CancellationToken cancellationToken = default);
    }
}
