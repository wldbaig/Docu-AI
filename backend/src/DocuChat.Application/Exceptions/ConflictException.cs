namespace DocuChat.Application;

public sealed class ConflictException(string message) : Exception(message);
