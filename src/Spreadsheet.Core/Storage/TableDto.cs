namespace Spreadsheet.Core.Storage;

// Що пишемо у JSON: розмір і тексти непорожніх клітинок ("B1": "inc(A1)").
// Значення не зберігаємо: після відкриття таблиця перераховує їх сама
public class TableDto
{
    public int Rows { get; set; }
    public int Columns { get; set; }
    public Dictionary<string, string> Cells { get; set; } = new();

    public static TableDto FromTable(Table table)
    {
        var dto = new TableDto { Rows = table.GetRowCount(), Columns = table.GetColumnCount() };
        foreach (var address in table.GetFilledAddresses())
        {
            dto.Cells[address.ToString()] = table.GetCell(address).GetExpression();
        }
        return dto;
    }

    public Table ToTable()
    {
        if (Rows < 1 || Columns < 1)
        {
            throw new InvalidDataException("Некоректний розмір таблиці у файлі");
        }
        var table = new Table(Rows, Columns);
        foreach (var (key, text) in Cells)
        {
            if (!CellAddress.TryParse(key, out var address))
            {
                throw new InvalidDataException($"Некоректна адреса у файлі: {key}");
            }
            table.UpdateCell(address, text);
        }
        return table;
    }
}
