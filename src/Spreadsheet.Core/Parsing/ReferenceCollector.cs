using Spreadsheet.Core.Grammar;

namespace Spreadsheet.Core.Parsing;

// Збирає всі адреси клітинок з виразу (знадобиться для графа залежностей)
public class ReferenceCollector : LabCalculatorBaseVisitor<object?>
{
    private readonly HashSet<CellAddress> refs = new();

    public override object? VisitCellRef(LabCalculatorParser.CellRefContext context)
    {
        // некоректну адресу (A0) пропускаємо: EvalVisitor видасть для неї #REF!
        if (CellAddress.TryParse(context.GetText(), out CellAddress address))
        {
            refs.Add(address);
        }
        return null;
    }

    public ISet<CellAddress> GetReferences() => refs;
}
