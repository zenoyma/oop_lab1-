using System.Numerics;
using Spreadsheet.Core.Parsing;

namespace Spreadsheet.Core;

// Таблиця: зберігає тільки непорожні клітинки і перераховує залежні після змін
public class Table
{
    private int rowCount;
    private int columnCount;
    private readonly Dictionary<CellAddress, Cell> cells = new();
    private readonly DependencyGraph graph = new();
    private readonly ExpressionEvaluator evaluator = new();

    public Table(int rows, int columns)
    {
        rowCount = rows;
        columnCount = columns;
    }

    public int GetRowCount() => rowCount;
    public int GetColumnCount() => columnCount;

    public Cell GetCell(CellAddress address) => cells.TryGetValue(address, out var cell) ? cell : new Cell();

    // Ввели текст: розбираємо -> оновлюємо граф -> перераховуємо клітинку і залежні
    public void UpdateCell(CellAddress address, string text)
    {
        text = text.Trim();
        if (text.Length == 0)
        {
            cells.Remove(address);
            graph.Remove(address);
            Recalculate(new[] { address });
            return;
        }

        if (!cells.TryGetValue(address, out var cell))
        {
            cells[address] = cell = new Cell();
        }
        try
        {
            var tree = evaluator.Parse(text);
            cell.SetExpression(text, tree);
            graph.SetPrecedents(address, evaluator.CollectReferences(tree));
        }
        catch (CellErrorException e)   // #SYNTAX!: дерева немає, тому й посилань немає
        {
            cell.SetExpression(text, null);
            cell.SetError(e.GetError(), Describe(e));
            graph.Remove(address);
        }
        Recalculate(new[] { address });
    }

    // Рядки і стовпці додаються й видаляються тільки в кінці.
    // Після цього все перераховуємо: посилання могли вийти за межі (#REF!) або повернутись
    public void AddRow() { rowCount++; RecalculateAll(); }
    public void AddColumn() { columnCount++; RecalculateAll(); }

    public void RemoveRow()
    {
        if (rowCount <= 1) throw new InvalidOperationException("Таблиця має містити хоча б один рядок");
        rowCount--;
        RemoveOutside();
    }

    public void RemoveColumn()
    {
        if (columnCount <= 1) throw new InvalidOperationException("Таблиця має містити хоча б один стовпець");
        columnCount--;
        RemoveOutside();
    }

    public void RecalculateAll() => Recalculate(cells.Keys.ToList());

    private void RemoveOutside()
    {
        foreach (var address in cells.Keys.Where(a => !IsInside(a)).ToList())
        {
            cells.Remove(address);
            graph.Remove(address);
        }
        RecalculateAll();
    }

    private bool IsInside(CellAddress a) => a.Row < rowCount && a.Column < columnCount;

    private void Recalculate(IEnumerable<CellAddress> changed)
    {
        RecalcPlan plan = graph.GetRecalcOrder(changed);

        string cycle = string.Join(", ", plan.CycleCells.OrderBy(a => a.Column).ThenBy(a => a.Row));
        foreach (var address in plan.CycleCells)
        {
            if (cells.TryGetValue(address, out var cell))
            {
                cell.SetError(CellError.Cycle, $"циклічне посилання: {cycle}");
            }
        }

        foreach (var address in plan.Order)
        {
            if (!cells.TryGetValue(address, out var cell) || cell.GetTree() is not { } tree)
            {
                continue;   // порожня або з #SYNTAX! - рахувати нічого
            }
            try
            {
                cell.SetValue(evaluator.Evaluate(tree, ResolveValue));
            }
            catch (CellErrorException e)
            {
                cell.SetError(e.GetError(), Describe(e));
            }
        }
    }

    // Значення клітинки для виразу (це resolve з EvalVisitor). Порожня = 0
    // Помилка передається далі: якщо A1 = #VALUE!, то й inc(A1) дає #VALUE!
    private object ResolveValue(CellAddress address)
    {
        if (!IsInside(address))
        {
            throw new CellErrorException(CellError.Ref, 0, $"клітинка {address} поза межами таблиці");
        }
        Cell cell = GetCell(address);
        if (cell.GetError() is CellError error)
        {
            throw new CellErrorException(error, 0, $"клітинка {address} містить помилку {CellErrorText.Of(error)}");
        }
        return cell.GetValue() ?? BigInteger.Zero;
    }

    private static string Describe(CellErrorException e) => $"позиція {e.GetPosition() + 1}: {e.GetDetails()}";
}
