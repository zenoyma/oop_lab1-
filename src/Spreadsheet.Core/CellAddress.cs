namespace Spreadsheet.Core;

// Адресу рахуємо з нуля: "A1" = (0, 0), "AB12" = (27, 11)
public readonly record struct CellAddress(int Column, int Row)
{
    public static bool TryParse(string text, out CellAddress address)
    {
        address = default;
        text = text.Trim().ToUpperInvariant();

        int i = 0;
        int column = 0;
        while (i < text.Length && text[i] >= 'A' && text[i] <= 'Z')
        {
            column = column * 26 + (text[i] - 'A' + 1);   // літери читаємо як число: A=1, Z=26, AA=27
            i++;
        }
        // має бути хоч одна літера, а після неї номер рядка від 1
        if (i == 0 || i == text.Length || !int.TryParse(text[i..], out int row) || row < 1)
        {
            return false;
        }
        address = new CellAddress(column - 1, row - 1);
        return true;
    }

    public static CellAddress Parse(string text)
    {
        if (!TryParse(text, out CellAddress address))
        {
            throw new FormatException($"Некоректна адреса клітинки: {text}");
        }
        return address;
    }

    // Назва стовпця за номером: 0 -> "A", 25 -> "Z", 26 -> "AA"
    public static string ColumnName(int column)
    {
        string name = "";
        for (int n = column + 1; n > 0; n = (n - 1) / 26)
        {
            name = (char)('A' + (n - 1) % 26) + name;
        }
        return name;
    }

    public override string ToString() => ColumnName(Column) + (Row + 1);
}
