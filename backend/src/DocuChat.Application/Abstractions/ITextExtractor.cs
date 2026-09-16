namespace DocuChat.Application;

public interface ITextExtractor
{
    Task<string> ExtractAsync(Stream stream, string extension, CancellationToken cancellationToken);
}
