using RagAgents.Core.Interfaces;
using System;
using System.IO;
using System.Threading.Tasks;

namespace RagAgents.Core.Agents
{
    /// <summary>
    /// Pdf ingest tool. Expects input to be a data URI (data:application/pdf;base64,...) or
    /// a filename available to the executing process. When given a data URI it will decode
    /// and call IPdfIngestService.IngestAsync with the stream.
    /// </summary>
    public class PdfIngestTool : ITool
    {
        private readonly IPdfIngestService _ingest;
        public string Name => "PdfIngest";

        public PdfIngestTool(IPdfIngestService ingest)
        {
            _ingest = ingest;
        }

        public async Task<string> RunAsync(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return "PdfIngest: input is empty. Provide a data URI (data:application/pdf;base64,...) or a local file path.";

            // Handle data URI
            const string prefix = "data:application/pdf;base64,";
            if (input.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    var b64 = input.Substring(prefix.Length);
                    var bytes = Convert.FromBase64String(b64);
                    using var ms = new MemoryStream(bytes);
                    await _ingest.IngestAsync(ms, "uploaded.pdf");
                    return "PdfIngest: PDF ingested from data URI.";
                }
                catch (Exception ex)
                {
                    return $"PdfIngest: failed to ingest data URI - {ex.Message}";
                }
            }

            // Handle local file path
            if (File.Exists(input))
            {
                try
                {
                    using var fs = File.OpenRead(input);
                    await _ingest.IngestAsync(fs, Path.GetFileName(input));
                    return $"PdfIngest: ingested local file {input}.";
                }
                catch (Exception ex)
                {
                    return $"PdfIngest: failed to ingest file - {ex.Message}";
                }
            }

            return "PdfIngest: unsupported input. Provide data URI or existing local file path.";
        }
    }
}
