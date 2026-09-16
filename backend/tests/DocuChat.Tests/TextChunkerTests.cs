using DocuChat.Application;

namespace DocuChat.Tests;

public sealed class TextChunkerTests
{
    [Fact]
    public void Split_UsesOverlapAndPreservesContent()
    {
        var text = string.Join(' ', Enumerable.Repeat("grounded", 80));
        var chunks = new TextChunker(100, 20).Split(text);
        Assert.True(chunks.Count > 1);
        Assert.All(chunks, chunk => Assert.InRange(chunk.Length, 1, 100));
        Assert.All(chunks, chunk => Assert.Contains("grounded", chunk));
    }

    [Fact]
    public void Split_ReturnsNothingForWhitespace() => Assert.Empty(new TextChunker().Split(" \r\n "));
}
