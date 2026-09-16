namespace DocuChat.Application;

public sealed record RagContext(
    string Question,
    IReadOnlyList<SourceDto> Sources,
    string GroundedPrompt);
