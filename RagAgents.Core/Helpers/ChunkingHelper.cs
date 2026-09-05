using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RagAgents.Core.Helpers
{
    public static class ChunkingHelper
    {

        // Helper: split long text into chunks with overlap
        public static IEnumerable<string> SplitTextByOverLap(string text, int size, int overlap)
        {
            for (int i = 0; i < text.Length; i += size - overlap)
                yield return text.Substring(i, Math.Min(size, text.Length - i));
        }

    }

}
