using System.Runtime.CompilerServices;
using System.Text;
using DocuChat.Domain;

namespace DocuChat.Application;

public sealed class RagService(
    IDocumentRepository documents,
    IChatMessageRepository messages,
    IUnitOfWork unitOfWork,
    IEmbeddingService embeddings,
    IVectorStore vectorStore,
    IChatCompletionService chat) : IRagService
{
    private const string SystemPrompt = """
        You are DocuChat AI. Answer ONLY from the supplied document excerpts.
        If the excerpts do not contain enough information, say: "I couldn't find that in the uploaded documents."
        Never use outside knowledge. Be concise and cite supporting excerpts inline as [Source N].
        """;

    public async Task<RagContext> PrepareAsync(Guid userId, AskRequest request, CancellationToken cancellationToken)
    {
        var topK = Math.Clamp(request.TopK, 1, 10);
        if (!await documents.HasReadyAsync(userId, request.DocumentIds, cancellationToken))
            throw new ValidationException("Upload and index at least one document before asking a question.");

        var questionVector = (await embeddings.CreateAsync([request.Question], cancellationToken))[0];
        var matches = await vectorStore.SearchAsync(userId, questionVector, topK, request.DocumentIds, cancellationToken);
        var sources = matches.Select(x => new SourceDto(x.DocumentId, x.FileName, x.ChunkIndex, x.Content, x.Score)).ToArray();
        if (sources.Length == 0) throw new ValidationException("No matching document chunks were found in the vector store. Reindex the selected documents.");

        var prompt = new StringBuilder().AppendLine("DOCUMENT EXCERPTS:");
        for (var i = 0; i < sources.Length; i++)
            prompt.AppendLine($"[Source {i + 1}] File: {sources[i].FileName}, chunk {sources[i].ChunkIndex + 1}\n{sources[i].Content}\n");
        prompt.AppendLine($"QUESTION:\n{request.Question}");
        return new RagContext(request.Question, sources, prompt.ToString());
    }

    public async IAsyncEnumerable<string> StreamAnswerAsync(Guid userId, RagContext context, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        messages.Add(new ChatMessage { UserId = userId, Role = ChatRole.User, Content = context.Question });
        await unitOfWork.SaveChangesAsync(cancellationToken);
        var answer = new StringBuilder();
        await foreach (var token in chat.StreamAsync(SystemPrompt, context.GroundedPrompt, cancellationToken))
        {
            answer.Append(token);
            yield return token;
        }
        messages.Add(new ChatMessage { UserId = userId, Role = ChatRole.Assistant, Content = answer.ToString() });
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
