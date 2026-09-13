using RagAgents.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UglyToad.PdfPig;

namespace RagAgents.Core.Services
{
    public class PdfPigDocumentTextExtractor: IDocumentTextExtractor
    {
        public async Task<string> ExtractTextAsync(
       Stream document,
       CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            using var pdf = PdfDocument.Open(document);

            var text = string.Join(
                Environment.NewLine,
                pdf.GetPages()
                   .Select(page => page.Text));

            await Task.CompletedTask;

            return text;
        }
    }
}
