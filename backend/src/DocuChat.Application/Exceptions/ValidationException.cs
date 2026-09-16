namespace DocuChat.Application;

public sealed class ValidationException(string message) : Exception(message);
