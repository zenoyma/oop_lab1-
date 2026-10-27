using Antlr4.Runtime;
using Antlr4.Runtime.Tree;
using Spreadsheet.Core.Grammar;

namespace Spreadsheet.Core.Parsing;

// Єдине місце, через яке решта програми працює з ANTLR
public class ExpressionEvaluator
{
    // Текст -> дерево розбору; при помилці кидає #SYNTAX! з позицією
    public IParseTree Parse(string text)
    {
        var listener = new ThrowingErrorListener();

        var lexer = new LabCalculatorLexer(new AntlrInputStream(text));
        lexer.RemoveErrorListeners();
        lexer.AddErrorListener(listener);

        var parser = new LabCalculatorParser(new CommonTokenStream(lexer));
        parser.RemoveErrorListeners();
        parser.AddErrorListener(listener);

        return parser.program();
    }

    public ISet<CellAddress> CollectReferences(IParseTree tree)
    {
        var collector = new ReferenceCollector();
        collector.Visit(tree);
        return collector.GetReferences();
    }

    public object? Evaluate(IParseTree tree, Func<CellAddress, object> resolve)
        => new EvalVisitor(resolve).Visit(tree);
}
