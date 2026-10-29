namespace Spreadsheet.Core;

// Що повертає граф: Order - у якому порядку рахувати клітинки,
// CycleCells - клітинки в циклі або залежні від нього (отримають #CYCLE!)
public class RecalcPlan
{
    public List<CellAddress> Order { get; } = new();
    public HashSet<CellAddress> CycleCells { get; } = new();
}
