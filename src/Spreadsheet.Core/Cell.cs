using System.Numerics;
using Antlr4.Runtime.Tree;

namespace Spreadsheet.Core;

// Одна клітинка: що ввели, дерево розбору і результат (значення або помилка)
public class Cell
{
    private string expression = "";
    private object? value;            // число, bool або null (порожня)
    private CellError? error;
    private string errorDetails = "";
    private IParseTree? tree;         // null, якщо порожня або синтаксична помилка

    public string GetExpression() => expression;
    public object? GetValue() => value;
    public CellError? GetError() => error;
    public string GetErrorDetails() => errorDetails;
    public IParseTree? GetTree() => tree;

    public void SetExpression(string text, IParseTree? tree)
    {
        expression = text;
        this.tree = tree;
        value = null;
        error = null;
        errorDetails = "";
    }

    public void SetValue(object? value)
    {
        this.value = value;
        error = null;
        errorDetails = "";
    }

    public void SetError(CellError error, string details)
    {
        this.error = error;
        errorDetails = details;
        value = null;
    }

    public string GetDisplayText(ViewMode mode)
    {
        if (mode == ViewMode.Expression)
        {
            return expression;
        }
        if (error is CellError e)
        {
            return CellErrorText.Of(e);
        }
        return value switch
        {
            bool b => b ? "ІСТИНА" : "ХИБНІСТЬ",
            BigInteger n => n.ToString(),
            _ => "",
        };
    }
}
