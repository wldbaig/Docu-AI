using System.Text;
using DocuChat.Application;
using UglyToad.PdfPig;

namespace DocuChat.Infrastructure;

public sealed class DocumentTextExtractor : ITextExtractor
{
    public async Task<string> ExtractAsync(Stream stream, string extension, CancellationToken cancellationToken)
    {
        if (extension.Equals(".txt", StringComparison.OrdinalIgnoreCase))
        {
            using var reader = new StreamReader(stream, Encoding.UTF8, true, leaveOpen: true);
            return await reader.ReadToEndAsync(cancellationToken);
        }
        if (extension.Equals(".pdf", StringComparison.OrdinalIgnoreCase))
        {
            using var document = PdfDocument.Open(stream);
            var builder = new StringBuilder();
            foreach (var page in document.GetPages())
            {
                cancellationToken.ThrowIfCancellationRequested();
                builder.AppendLine(page.Text);
            }
            return builder.ToString();
        }
        throw new ValidationException("Only PDF and TXT files are supported.");
    }
}
