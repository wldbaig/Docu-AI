using System.Text.RegularExpressions;

namespace DocuChat.Application;

public sealed class TextChunker(int chunkSize = 1200, int overlap = 200) : ITextChunker
{
    public IReadOnlyList<string> Split(string text)
    {
        var normalized = Regex.Replace(text, @"\s+", " ").Trim();
        if (normalized.Length == 0) return [];
        var chunks = new List<string>();
        var start = 0;
        while (start < normalized.Length)
        {
            var length = Math.Min(chunkSize, normalized.Length - start);
            var end = start + length;
            if (end < normalized.Length)
            {
                var boundary = normalized.LastIndexOfAny(['.', '!', '?'], end - 1, length);
                if (boundary > start + chunkSize / 2) end = boundary + 1;
            }
            chunks.Add(normalized[start..end].Trim());
            if (end >= normalized.Length) break;
            start = Math.Max(start + 1, end - overlap);
        }
        return chunks.Where(x => x.Length > 0).ToArray();
    }
}
