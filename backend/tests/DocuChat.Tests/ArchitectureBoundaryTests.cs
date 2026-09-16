using DocuChat.Application;
using System.Text.RegularExpressions;

namespace DocuChat.Tests;

public sealed class ArchitectureBoundaryTests
{
    [Fact]
    public void Application_DoesNotReferenceOuterFrameworksOrInfrastructure()
    {
        var references = typeof(IAuthService).Assembly.GetReferencedAssemblies().Select(x => x.Name!).ToArray();

        Assert.DoesNotContain("DocuChat.Infrastructure", references);
        Assert.DoesNotContain(references, name => name.StartsWith("Microsoft.AspNetCore", StringComparison.Ordinal));
        Assert.DoesNotContain(references, name => name.StartsWith("Microsoft.EntityFrameworkCore", StringComparison.Ordinal));
    }

    [Fact]
    public void UseCaseImplementations_LiveInApplicationAssembly()
    {
        var applicationAssembly = typeof(IAuthService).Assembly;

        Assert.Equal(applicationAssembly, typeof(AuthService).Assembly);
        Assert.Equal(applicationAssembly, typeof(DocumentService).Assembly);
        Assert.Equal(applicationAssembly, typeof(RagService).Assembly);
    }

    [Fact]
    public void CSharpFiles_DeclareAtMostOneType()
    {
        var repositoryRoot = FindRepositoryRoot();
        var sourceRoots = new[]
        {
            Path.Combine(repositoryRoot, "backend", "src"),
            Path.Combine(repositoryRoot, "backend", "tests")
        };
        var declarationPattern = new Regex(
            @"(?m)^\s*(?:(?:public|internal|private|protected|file)\s+)?(?:(?:sealed|abstract|static|partial|readonly)\s+)*(?:class|interface|enum|record(?:\s+(?:class|struct))?)\s+[A-Za-z_]\w*",
            RegexOptions.CultureInvariant);

        var violations = sourceRoots
            .SelectMany(root => Directory.EnumerateFiles(root, "*.cs", SearchOption.AllDirectories))
            .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase))
            .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase))
            .Select(path => new { Path = path, Count = declarationPattern.Matches(File.ReadAllText(path)).Count })
            .Where(result => result.Count > 1)
            .Select(result => $"{Path.GetRelativePath(repositoryRoot, result.Path)} ({result.Count} declarations)")
            .ToArray();

        Assert.True(
            violations.Length == 0,
            $"Each C# file must declare at most one class, interface, record, or enum.{Environment.NewLine}{string.Join(Environment.NewLine, violations)}");
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "DocuChatAI.slnx")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName
            ?? throw new DirectoryNotFoundException("Could not locate the DocuChat AI repository root.");
    }
}
