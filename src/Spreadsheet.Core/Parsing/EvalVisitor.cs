using System.Numerics;
using Antlr4.Runtime;
using Spreadsheet.Core.Grammar;

namespace Spreadsheet.Core.Parsing;

// Обчислює дерево розбору. Результат - BigInteger (число) або bool (результат порівняння).
// resolve дає значення іншої клітинки (його передасть таблиця).
public class EvalVisitor : LabCalculatorBaseVisitor<object?>
{
    private readonly Func<CellAddress, object> resolve;

    public EvalVisitor(Func<CellAddress, object> resolve)
    {
        this.resolve = resolve;
    }

    public override object? VisitProgram(LabCalculatorParser.ProgramContext context)
        => Visit(context.expression());

    public override object? VisitNumber(LabCalculatorParser.NumberContext context)
        => BigInteger.Parse(context.GetText());

    public override object? VisitCellRef(LabCalculatorParser.CellRefContext context)
    {
        if (!CellAddress.TryParse(context.GetText(), out CellAddress address))
        {
            throw Error(CellError.Ref, context, $"некоректна адреса {context.GetText()}");
        }
        try
        {
            return resolve(address);
        }
        catch (CellErrorException e)   // помилка в A1 -> та сама помилка, але позиція вказує на A1 у цьому виразі
        {
            throw new CellErrorException(e.GetError(), context.Start.Column, e.GetDetails());
        }
    }

    // Змінних поки немає, тому будь-яке слово дає #NAME?
    public override object? VisitVariable(LabCalculatorParser.VariableContext context)
    {
        throw Error(CellError.Name, context, $"невідоме ім'я '{context.GetText()}'");
    }

    public override object? VisitParens(LabCalculatorParser.ParensContext context)
        => Visit(context.expression());

    public override object? VisitIncDec(LabCalculatorParser.IncDecContext context)
    {
        BigInteger x = Number(context.expression());
        return context.op.Type == LabCalculatorLexer.INC ? x + 1 : x - 1;
    }

    public override object? VisitMaxMin(LabCalculatorParser.MaxMinContext context)
    {
        BigInteger x = Number(context.expression(0));
        BigInteger y = Number(context.expression(1));
        return context.op.Type == LabCalculatorLexer.MAX ? BigInteger.Max(x, y) : BigInteger.Min(x, y);
    }

    public override object? VisitMmaxMmin(LabCalculatorParser.MmaxMminContext context)
    {
        bool isMax = context.op.Type == LabCalculatorLexer.MMAX;
        BigInteger result = Number(context.expression(0));
        foreach (var argument in context.expression().Skip(1))
        {
            BigInteger x = Number(argument);
            result = isMax ? BigInteger.Max(result, x) : BigInteger.Min(result, x);
        }
        return result;
    }

    public override object? VisitCompare(LabCalculatorParser.CompareContext context)
    {
        object? left = Visit(context.expression(0));
        object? right = Visit(context.expression(1));
        int op = context.op.Type;

        // два bool порівнюємо тільки на = і <>: (1<2) = (3<4)
        if (left is bool a && right is bool b)
        {
            if (op == LabCalculatorLexer.EQ) return a == b;
            if (op == LabCalculatorLexer.NEQ) return a != b;
            throw Error(CellError.Value, context, "логічні значення не можна порівнювати на більше/менше");
        }
        if (left is not BigInteger x || right is not BigInteger y)
        {
            throw Error(CellError.Value, context, "не можна порівнювати число з логічним значенням");
        }

        int c = x.CompareTo(y);
        return op switch
        {
            LabCalculatorLexer.EQ => c == 0,
            LabCalculatorLexer.NEQ => c != 0,
            LabCalculatorLexer.LT => c < 0,
            LabCalculatorLexer.GT => c > 0,
            LabCalculatorLexer.LE => c <= 0,
            _ => c >= 0,   // GE
        };
    }

    // Аргумент функції має бути числом, інакше #VALUE!
    private BigInteger Number(LabCalculatorParser.ExpressionContext context)
    {
        object? value = Visit(context);
        if (value is BigInteger number)
        {
            return number;
        }
        throw Error(CellError.Value, context, "очікувалось число, а не логічне значення");
    }

    private static CellErrorException Error(CellError error, ParserRuleContext context, string details)
        => new(error, context.Start.Column, details);
}
