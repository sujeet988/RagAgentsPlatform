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
            if (string.IsNullOrEmpty(text))
                yield break;

            if (size <= 0)
                throw new ArgumentOutOfRangeException(nameof(size), "Chunk size must be greater than zero.");

            if (overlap < 0 || overlap >= size)
                throw new ArgumentOutOfRangeException(nameof(overlap), "Chunk overlap must be greater than or equal to zero and less than chunk size.");

            for (int i = 0; i < text.Length; i += size - overlap)
                yield return text.Substring(i, Math.Min(size, text.Length - i));
        }

    }

}
