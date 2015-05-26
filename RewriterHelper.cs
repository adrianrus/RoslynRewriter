using Roslyn.Compilers.CSharp;
using Roslyn.Services;
using System;
using System.IO;

namespace RoslynRewriter
{
    internal class RewriterHelper
    {
        public static void ProcessProject<TRewriter>(string projectPath) where TRewriter : SyntaxRewriter, IActionDone, new()
        {
            var workspace = Workspace.LoadStandAloneProject(projectPath);

            foreach (var project in workspace.CurrentSolution.Projects)
            {
                foreach (var document in project.Documents)
                {
                    string filePath = document.FilePath;
                    var node = document.GetSyntaxTree().GetRoot() as CompilationUnitSyntax;

                    Console.WriteLine("Processing file -> {0}", filePath);

                    if (node != null)
                    {
                        var reorderUsingsRewriter = new TRewriter();
                        SyntaxNode newRoot = reorderUsingsRewriter.Visit(node);

                        if (reorderUsingsRewriter.ActionDone)
                            File.WriteAllText(filePath, newRoot.GetText().ToString());
                    }
                }
            }
        }

        public static void ProcessFile<TRewriter>(string filePath, TextWriter writer) where TRewriter : SyntaxRewriter, IActionDone, new()
        {
            var tree = Syntax.ParseCompilationUnit(File.ReadAllText(filePath));

            SyntaxNode newRoot = new TRewriter().Visit(tree);
            newRoot.WriteTo(writer);
        }
    }
}
