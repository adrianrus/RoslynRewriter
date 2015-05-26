using Roslyn.Compilers.CSharp;
using System.Collections.Generic;
using System.Linq;

namespace RoslynRewriter
{
    internal class ReorderUsingsRewriter : SyntaxRewriter, IActionDone
    {
        private bool _actionDone;
        private readonly List<UsingDirectiveSyntax> _usings = new List<UsingDirectiveSyntax>();

        public bool ActionDone
        {
            get { return _actionDone || _usings.Any(); }
        }

        public override SyntaxNode VisitCompilationUnit(CompilationUnitSyntax node)
        {
            var newNode = RemoveNamespaceUsings(node);
            return newNode.AddUsings(_usings.ToArray());
        }

        private CompilationUnitSyntax RemoveNamespaceUsings(CompilationUnitSyntax node)
        {
            var removeUsingsRewriter = new RemoveUsingsFromNamespaceRewriter();
            var result = removeUsingsRewriter.Visit(node);
            _usings.AddRange(removeUsingsRewriter.Usings);

            return (CompilationUnitSyntax)result;
        }

        public override SyntaxNode VisitNamespaceDeclaration(NamespaceDeclarationSyntax node)
        {
            var parent = (CompilationUnitSyntax)node.Parent;
            bool shouldAddNewLineBeforeNamespace = parent.Usings.Any() && !node.GetLeadingTrivia().Any(SyntaxKind.EndOfLineTrivia);
            if (shouldAddNewLineBeforeNamespace)
            {
                _actionDone = true;
                return node.WithLeadingTrivia(Syntax.CarriageReturnLineFeed);
            }

            return node;
        }

        internal class RemoveUsingsFromNamespaceRewriter : SyntaxRewriter
        {
            private readonly List<UsingDirectiveSyntax> _usings = new List<UsingDirectiveSyntax>();

            public List<UsingDirectiveSyntax> Usings
            {
                get { return _usings; }
            }

            public override SyntaxNode VisitUsingDirective(UsingDirectiveSyntax node)
            {
                if (node.Parent is CompilationUnitSyntax)
                    return node;

                var newNode = node.ReplaceTrivia(node.GetLeadingTrivia(), (trivia, syntaxTrivia) => new SyntaxTriviaList());
                Usings.Add(newNode);

                return null;
            }
        }
    }

    internal interface IActionDone
    {
        bool ActionDone { get; }
    }
}
