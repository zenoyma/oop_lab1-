using Spreadsheet.Core.Storage;

namespace Spreadsheet.Core;

// Посередник між вікном і таблицею: кнопки викликають тільки його методи, сама логіка тут
public class SpreadsheetController
{
    public const int DefaultRows = 10;
    public const int DefaultColumns = 10;

    private Table table = new(DefaultRows, DefaultColumns);
    private ViewMode mode = ViewMode.Value;
    private bool hasUnsavedChanges;
    private readonly ITableStorage fileStorage;
    private readonly ITableStorage? driveStorage;   // null, якщо Drive не підключено

    public SpreadsheetController(ITableStorage fileStorage, ITableStorage? driveStorage = null)
    {
        this.fileStorage = fileStorage;
        this.driveStorage = driveStorage;
    }

    public void HandleCellEdit(string address, string text)
    {
        var a = CellAddress.Parse(address);
        if (table.GetCell(a).GetExpression() == text.Trim())
        {
            return;   // нічого не змінилося
        }
        table.UpdateCell(a, text);
        hasUnsavedChanges = true;
    }

    public string GetCellText(int row, int column) => GetCell(row, column).GetDisplayText(mode);
    public string GetCellExpression(int row, int column) => GetCell(row, column).GetExpression();
    public string GetCellErrorDetails(int row, int column) => GetCell(row, column).GetErrorDetails();

    public ViewMode GetMode() => mode;
    public void HandleToggleMode() => mode = mode == ViewMode.Value ? ViewMode.Expression : ViewMode.Value;

    public void HandleAddRow() { table.AddRow(); hasUnsavedChanges = true; }
    public void HandleAddColumn() { table.AddColumn(); hasUnsavedChanges = true; }
    public void HandleRemoveRow() { table.RemoveRow(); hasUnsavedChanges = true; }
    public void HandleRemoveColumn() { table.RemoveColumn(); hasUnsavedChanges = true; }

    public void HandleNewTable(int rows, int columns)
    {
        table = new Table(rows, columns);
        hasUnsavedChanges = false;
    }

    public async Task SaveToFileAsync(string path)
    {
        await fileStorage.SaveAsync(table, path);
        hasUnsavedChanges = false;
    }

    public async Task LoadFromFileAsync(string path)
    {
        table = await fileStorage.LoadAsync(path);
        hasUnsavedChanges = false;
    }

    public async Task SaveToDriveAsync(string name)
    {
        await RequireDrive().SaveAsync(table, name);
        hasUnsavedChanges = false;
    }

    public async Task LoadFromDriveAsync(string name)
    {
        table = await RequireDrive().LoadAsync(name);
        hasUnsavedChanges = false;
    }

    public int GetTableRowCount() => table.GetRowCount();
    public int GetTableColumnCount() => table.GetColumnCount();
    public bool HasUnsavedChanges() => hasUnsavedChanges;

    private Cell GetCell(int row, int column) => table.GetCell(new CellAddress(column, row));

    private ITableStorage RequireDrive()
        => driveStorage ?? throw new InvalidOperationException("Google Drive не налаштовано");
}
