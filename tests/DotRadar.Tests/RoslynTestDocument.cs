using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;

namespace DotRadar.Tests;

internal static class RoslynTestDocument
{
    private static readonly IReadOnlyList<MetadataReference>
        FrameworkReferences = CreateFrameworkReferences();

    public static Document Create(
        string source,
        string filePath = "Test.cs")
    {
        var workspace = new AdhocWorkspace();

        var project = workspace
            .AddProject(
                "DotRadar.TestProject",
                LanguageNames.CSharp)
            .WithCompilationOptions(
                new CSharpCompilationOptions(
                    OutputKind.DynamicallyLinkedLibrary))
            .WithParseOptions(
                new CSharpParseOptions(LanguageVersion.Latest))
            .AddMetadataReferences(FrameworkReferences);

        return project.AddDocument(
            Path.GetFileName(filePath),
            SourceText.From(source),
            filePath: filePath);
    }

    private static IReadOnlyList<MetadataReference>
        CreateFrameworkReferences()
    {
        var trustedAssemblies =
            AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")
                as string
            ?? throw new InvalidOperationException(
                "Unable to locate platform assemblies.");

        return trustedAssemblies
            .Split(Path.PathSeparator)
            .Select(path =>
                MetadataReference.CreateFromFile(path))
            .ToArray();
    }
}