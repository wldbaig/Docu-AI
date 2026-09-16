namespace DocuChat.Application;

public sealed class ExternalServiceException(string message, Exception? inner = null) : Exception(message, inner);
