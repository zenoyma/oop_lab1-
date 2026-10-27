using Antlr4.Runtime;

namespace Spreadsheet.Core.Parsing;

// Синтаксична помилка у виразі -> виняток #SYNTAX! з позицією символу
public class ThrowingErrorListener : BaseErrorListener, IAntlrErrorListener<int>
{
    // Неправильний порядок: "max(1,", "1 2", ")"
    public override void SyntaxError(TextWriter output, IRecognizer recognizer, IToken offendingSymbol,
        int line, int charPositionInLine, string msg, RecognitionException e)
    {
        string details = offendingSymbol.Type == TokenConstants.EOF
            ? "неочікуваний кінець виразу"
            : $"неочікуваний символ '{offendingSymbol.Text}'";
        throw new CellErrorException(CellError.Syntax, charPositionInLine, details);
    }

    // Символ, якого немає в граматиці: "1 + 2", "A1#"
    public void SyntaxError(TextWriter output, IRecognizer recognizer, int offendingSymbol,
        int line, int charPositionInLine, string msg, RecognitionException e)
    {
        throw new CellErrorException(CellError.Syntax, charPositionInLine, "недопустимий символ");
    }
}
