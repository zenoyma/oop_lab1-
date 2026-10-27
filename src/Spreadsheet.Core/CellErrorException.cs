namespace Spreadsheet.Core;

// Яка помилка, де вона (номер символу в тексті клітинки, з 0) і пояснення для рядка стану
public class CellErrorException : Exception
{
    private readonly CellError error;
    private readonly int position;
    private readonly string details;

    public CellErrorException(CellError error, int position, string details)
        : base($"{CellErrorText.Of(error)} (позиція {position + 1}): {details}")
    {
        this.error = error;
        this.position = position;
        this.details = details;
    }

    public CellError GetError() => error;
    public int GetPosition() => position;
    public string GetDetails() => details;
}
