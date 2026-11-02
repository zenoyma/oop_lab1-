using System.Numerics;
using Antlr4.Runtime;
using Spreadsheet.Core.Grammar;

namespace Spreadsheet.Core.Parsing;

// Обчислює дерево розбору. Результат - BigInteger (число) або bool (результат порівняння).
// resolve дає значення іншої клітинки (його передає таблиця).
// null - "немає значення" (while або if без else); у клітинці це порожньо.
public class EvalVisitor : LabCalculatorBaseVisitor<object?>
{
    public const int MaxLoopIterations = 100_000;

    private readonly Func<CellAddress, object> resolve;
    private readonly Dictionary<string, object?> variables = new();   // змінні програми в клітинці
    private int loopIterations;                                       // спільний лічильник для всіх while у клітинці

    public EvalVisitor(Func<CellAddress, object> resolve)
    {
        this.resolve = resolve;
    }

    public override object? VisitProgram(LabCalculatorParser.ProgramContext context)
        => Visit(context.statementList());

    // оператори (бонус):
    // Виконує оператори по черзі; результат - значення останнього
    public override object? VisitStatementList(LabCalculatorParser.StatementListContext context)
    {
        object? last = null;
        foreach (var statement in context.statement())
        {
            last = Visit(statement);
        }
        return last;
    }

    public override object? VisitAssign(LabCalculatorParser.AssignContext context)
    {
        object? value = Visit(context.expression());
        variables[context.IDENT().GetText().ToLowerInvariant()] = value;
        return value;
    }

    public override object? VisitIf(LabCalculatorParser.IfContext context)
    {
        if (Condition(context.expression()))
        {
            return Visit(context.statement(0));
        }
        return context.ELSE() != null ? Visit(context.statement(1)) : null;
    }

    public override object? VisitWhile(LabCalculatorParser.WhileContext context)
    {
        while (Condition(context.expression()))
        {
            if (++loopIterations > MaxLoopIterations)
            {
                throw Error(CellError.Loop, context, $"цикл виконався понад {MaxLoopIterations} разів");
            }
            Visit(context.statement());
        }
        return null;
    }

    public override object? VisitBlock(LabCalculatorParser.BlockContext context)
        => Visit(context.statementList());

    public override object? VisitExpressionStatement(LabCalculatorParser.ExpressionStatementContext context)
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

    // Змінна програми; якщо їй ще нічого не присвоїли - #NAME?
    public override object? VisitVariable(LabCalculatorParser.VariableContext context)
    {
        if (variables.TryGetValue(context.GetText().ToLowerInvariant(), out object? value))
        {
            return value;
        }
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

    // Умова if/while має бути логічною; інакше #VALUE!
    private bool Condition(LabCalculatorParser.ExpressionContext context)
    {
        if (Visit(context) is bool b)
        {
            return b;
        }
        throw Error(CellError.Value, context, "умова має бути логічною (наприклад i < 10)");
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
