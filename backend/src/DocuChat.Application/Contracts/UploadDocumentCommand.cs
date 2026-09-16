namespace DocuChat.Application;

public sealed record UploadDocumentCommand(string FileName, long Length, Stream Content);
