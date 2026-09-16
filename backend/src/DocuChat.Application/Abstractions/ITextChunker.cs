namespace DocuChat.Application;

public interface ITextChunker
{
    IReadOnlyList<string> Split(string text);
}
