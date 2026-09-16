# DocuChat AI

A production-minded RAG proof of concept for asking natural-language questions of PDF and TXT files. It combines a .NET 8 clean-architecture API, Qdrant vector search, configurable AI providers, and a responsive React/TypeScript frontend. Answers stream to the browser and include their exact source excerpts.

## Architecture

```text
React + Vite                   ASP.NET Core API
┌──────────────────┐          ┌──────────────────────────────────────┐
│ JWT auth         │──HTTP───▶│ API: controllers, JWT, SSE          │
│ Upload + status  │          │ Application: use cases and ports    │
│ Streaming chat   │◀─SSE────│ Infrastructure: providers + storage │
│ Source drawers   │          │ Domain: relational entities         │
└──────────────────┘          └──────────────┬───────────────────────┘
                                      ┌──────┴──────┐
                                   SQLite        Qdrant
                                users/status   vectors/payload
```

The dependency direction is `API → Application ← Infrastructure`, with Domain at the center. Application owns the authentication, document-indexing, and RAG use cases. It has no ASP.NET Core, EF Core, Qdrant, or vendor SDK dependency; Infrastructure implements the ports Application defines.

Every C# class, interface, record, and enum lives in its own file. The architecture test suite enforces this convention together with the Application-layer dependency boundary.

The application separates three AI/data concerns:

- `IEmbeddingService` selects the configured embedding provider.
- `IVectorStore` persists and queries embeddings. `QdrantVectorStore` is the current implementation.
- `IChatCompletionService` selects a named `IChatModelProvider` and streams generated text.

Concrete chat providers are included for OpenAI Chat Completions, OpenAI Codex through the Responses API, xAI Grok, Anthropic Claude, Google Gemini, and Zhipu GLM. OpenAI and Gemini embedding implementations are included. Chat and embedding providers are selected independently because not every chat vendor offers embeddings.

## RAG data flow

1. **Chunk:** PDF/TXT text is normalized and split into ~1,200-character chunks with 200 characters of overlap.
2. **Embed:** chunks are embedded in batches through the configured `IEmbeddingModelProvider`.
3. **Store:** vectors and source payloads are upserted into Qdrant. SQLite keeps relational ownership, status, and chat records—not vectors.
4. **Retrieve:** the question is embedded and Qdrant performs cosine search with mandatory user and optional document filters.
5. **Generate:** retrieved excerpts form a grounded prompt sent to the configured `IChatModelProvider`.
6. **Trace:** source metadata is emitted first, followed by answer tokens and a terminal SSE event.

## Repository layout

```text
backend/src/DocuChat.Api                    HTTP, JWT, Swagger, SSE, errors
backend/src/DocuChat.Application            use cases, DTOs, and outbound ports
backend/src/DocuChat.Domain                 User, Document, Chunk, ChatMessage
backend/src/DocuChat.Infrastructure/AI      model and embedding providers
backend/src/DocuChat.Infrastructure/Vector  Qdrant implementation
backend/src/DocuChat.Infrastructure/Persistence  EF repository adapters
backend/src/DocuChat.Infrastructure/Security     JWT and password adapters
backend/tests/DocuChat.Tests                unit and Qdrant integration tests
frontend                                    React, TypeScript, Tailwind
docker-compose.yml                          local Qdrant service
samples                                     document to try
```

## Supported providers

| Configuration name | API style | Chat | Embeddings |
|---|---|---:|---:|
| `OpenAI` | Chat Completions / Embeddings | Yes | Yes |
| `Codex` | OpenAI Responses | Yes | No; select OpenAI or Gemini |
| `Grok` | xAI OpenAI-compatible Chat Completions | Yes | No |
| `Claude` | Anthropic Messages | Yes | No |
| `Gemini` | Google `streamGenerateContent` / `batchEmbedContents` | Yes | Yes |
| `GLM` | Zhipu OpenAI-compatible Chat Completions | Yes | No |

The model IDs in `appsettings.json` are examples and remain fully configurable. Availability depends on the vendor account. When the embedding model or its output dimension changes, use a new Qdrant collection name and re-upload documents.

## Prerequisites

- .NET 8 SDK or newer
- Node.js 20+
- Docker Desktop or another reachable Qdrant instance
- An API key for the selected chat provider
- An API key for the selected embedding provider

> Windows note: if `dotnet --version` produces no output because a zero-byte `C:\Windows\System32\dotnet` shim takes precedence, invoke `C:\Program Files\dotnet\dotnet.exe` explicitly or correct the `PATH` order.

## Configure and run

Start Qdrant:

```powershell
docker compose up -d qdrant
```

Choose providers in `backend/src/DocuChat.Api/appsettings.json`:

```json
"AI": {
  "ChatProvider": "Claude",
  "EmbeddingProvider": "OpenAI"
}
```

Valid chat values are `OpenAI`, `Codex`, `Grok`, `Claude`, `Gemini`, and `GLM`. Valid embedding values are `OpenAI` and `Gemini`.

Store keys with .NET user-secrets. Examples:

```powershell
dotnet user-secrets set "Jwt:Key" "replace-with-a-random-secret-of-at-least-32-characters" --project backend/src/DocuChat.Api
dotnet user-secrets set "AI:Providers:OpenAI:ApiKey" "sk-..." --project backend/src/DocuChat.Api
dotnet user-secrets set "AI:Providers:Claude:ApiKey" "..." --project backend/src/DocuChat.Api
dotnet user-secrets set "AI:Providers:Gemini:ApiKey" "..." --project backend/src/DocuChat.Api
```

The same hierarchy works with environment variables, for example `AI__ChatProvider=Grok` and `AI__Providers__Grok__ApiKey=...`. Qdrant Cloud can be configured with `VectorStore__BaseUrl` and `VectorStore__ApiKey`. Never put real keys in source or frontend environment files.

Start the API; EF migrations apply automatically:

```powershell
dotnet restore DocuChatAI.slnx --configfile NuGet.Config
dotnet run --project backend/src/DocuChat.Api --launch-profile http
```

The API is at `http://localhost:5080`; Swagger is at `http://localhost:5080/swagger`.

In another terminal:

```powershell
cd frontend
npm install
npm run dev
```

Open `http://localhost:5173`, register, and upload [`samples/company-handbook.txt`](samples/company-handbook.txt). Try asking:

- How many paid leave days do employees receive?
- What can the learning allowance be used for?
- What should I do if I suspect a security incident?
- What is the parental leave policy? *(The correct result is a grounded refusal.)*

## API

| Method | Route | Purpose |
|---|---|---|
| `POST` | `/api/auth/register` | Create an account and return a JWT |
| `POST` | `/api/auth/login` | Return a JWT |
| `GET` | `/api/documents` | List the current user's documents |
| `POST` | `/api/documents` | Extract, embed, and index a PDF/TXT file |
| `DELETE` | `/api/documents/{id}` | Delete relational chunks and Qdrant points |
| `POST` | `/api/chat/ask` | Stream `sources`, `token`, `done`, and `error` events |

## Verification

```powershell
dotnet build DocuChatAI.slnx
dotnet test backend/tests/DocuChat.Tests
$env:RUN_QDRANT_TESTS="1"
dotnet test backend/tests/DocuChat.Tests
cd frontend
npm run build
```

The Qdrant integration test creates a temporary collection, verifies tenant-filtered retrieval and deletion, and removes the collection afterward. For production, move indexing to a durable queue, store source files in object storage, pin container/model versions, add retry/circuit-breaker policies and rate limits, and persist chat/source relationships for historical replay.
