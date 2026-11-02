using Spreadsheet.Core;
using Spreadsheet.Core.Storage;

namespace Spreadsheet.UI;

// Головне вікно: показує таблицю і передає дії користувача контролеру, сама нічого не рахує
public partial class MainPage : ContentPage
{
    // тут лежать збережені таблиці, client_secret.json і токен Google
    private static readonly string FilesFolder =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Spreadsheet74");

    private readonly SpreadsheetController controller =
        new(new LocalFileStorage(), new GoogleDriveStorage(FilesFolder));
    private Entry[,] cellEntries = new Entry[0, 0];
    private int selectedRow;
    private int selectedColumn;

    public MainPage()
    {
        InitializeComponent();
        RefreshGrid();
    }

    public void OnCellUnfocused(string address, string text)
    {
        controller.HandleCellEdit(address, text);
        RefreshGrid();
        var a = CellAddress.Parse(address);
        ShowMessage(controller.GetCellErrorDetails(a.Row, a.Column));
    }

    private void OnCellFocused(int row, int column)
    {
        selectedRow = row;
        selectedColumn = column;
        string expression = controller.GetCellExpression(row, column);
        cellEntries[row, column].Text = expression;   // у клітинці редагуємо вираз, навіть у режимі значень
        cellLabel.Text = new CellAddress(column, row).ToString();
        formulaEntry.Text = expression;
        ShowMessage(controller.GetCellErrorDetails(row, column));
    }

    private void OnFormulaCompleted(object? sender, EventArgs e)
        => OnCellUnfocused(new CellAddress(selectedColumn, selectedRow).ToString(), formulaEntry.Text ?? "");

    public void OnModeButtonClicked(object? sender, EventArgs e)
    {
        controller.HandleToggleMode();
        modeButton.Text = controller.GetMode() == ViewMode.Value ? "Режим: ЗНАЧЕННЯ" : "Режим: ВИРАЗ";
        RefreshGrid();
    }

    public void OnAddRowClicked(object? sender, EventArgs e) => ChangeSize(controller.HandleAddRow);
    public void OnRemoveRowClicked(object? sender, EventArgs e) => ChangeSize(controller.HandleRemoveRow);
    public void OnAddColumnClicked(object? sender, EventArgs e) => ChangeSize(controller.HandleAddColumn);
    public void OnRemoveColumnClicked(object? sender, EventArgs e) => ChangeSize(controller.HandleRemoveColumn);

    public async void OnNewTableClicked(object? sender, EventArgs e)
    {
        if (await ConfirmDiscard())
        {
            controller.HandleNewTable(SpreadsheetController.DefaultRows, SpreadsheetController.DefaultColumns);
            RefreshGrid();
        }
    }

    public async void OnSaveFileClicked(object? sender, EventArgs e)
    {
        string? name = await AskFileName();
        if (name == null) return;
        try
        {
            Directory.CreateDirectory(FilesFolder);
            name = Path.GetFileName(name);   // тільки назва, без шляху - щоб файл не потрапив в іншу папку
            string path = Path.Combine(FilesFolder, name.EndsWith(".json") ? name : name + ".json");
            await controller.SaveToFileAsync(path);
            ShowMessage($"Збережено: {path}");
        }
        catch (Exception ex)
        {
            await DisplayAlert("Помилка збереження", ex.Message, "OK");
        }
    }

    public async void OnOpenFileClicked(object? sender, EventArgs e)
    {
        if (!await ConfirmDiscard()) return;
        var file = await FilePicker.PickAsync(new PickOptions { PickerTitle = "Оберіть таблицю (.json)" });
        if (file == null) return;
        try
        {
            await controller.LoadFromFileAsync(file.FullPath);
            RefreshGrid();
            ShowMessage($"Відкрито: {file.FullPath}");
        }
        catch (Exception ex)   // файл не JSON або в ньому погані дані
        {
            await DisplayAlert("Не вдалося відкрити файл", ex.Message, "OK");
        }
    }

    public async void OnSaveDriveClicked(object? sender, EventArgs e)
    {
        string? name = await AskFileName();
        if (name == null) return;
        try
        {
            ShowMessage("Збереження на Google Drive…");
            await controller.SaveToDriveAsync(name);
            ShowMessage($"Збережено на Google Drive: {name}");
        }
        catch (Exception ex)
        {
            ShowMessage("");
            await DisplayAlert("Помилка Google Drive", ex.Message, "OK");
        }
    }

    public async void OnOpenDriveClicked(object? sender, EventArgs e)
    {
        if (!await ConfirmDiscard()) return;
        string? name = await AskFileName();
        if (name == null) return;
        try
        {
            ShowMessage("Завантаження з Google Drive…");
            await controller.LoadFromDriveAsync(name);
            RefreshGrid();
            ShowMessage($"Відкрито з Google Drive: {name}");
        }
        catch (Exception ex)
        {
            ShowMessage("");
            await DisplayAlert("Помилка Google Drive", ex.Message, "OK");
        }
    }

    public async void OnHelpClicked(object? sender, EventArgs e) => await DisplayAlert("Довідка",
        "Лабораторна робота 1*, варіант 74.\n" +
        "Автор: Бак Олександр, К-25.\n\n" +
        "Операції:\n" +
        "  inc(x), dec(x)\n" +
        "  max(x, y), min(x, y)\n" +
        "  mmax(x1, …, xN), mmin(x1, …, xN)\n" +
        "  =  <  >  <=  >=  <>\n\n" +
        "Програма в клітинці:\n" +
        "  x := вираз;   if умова then … else …;   while умова do …;   { … }\n" +
        "  Імена змінних - лише літери і _ (x1 - це адреса клітинки)", "OK");

    // Викликається з MauiProgram перед закриттям вікна. true - можна закривати
    public Task<bool> OnClosing() => ConfirmDiscard();

    private void ChangeSize(Action action)
    {
        try
        {
            action();
            RefreshGrid();
        }
        catch (InvalidOperationException ex)
        {
            ShowMessage(ex.Message);
        }
    }

    private async Task<bool> ConfirmDiscard()
        => !controller.HasUnsavedChanges()
           || await DisplayAlert("Незбережені зміни", "Таблицю не збережено. Продовжити без збереження?", "Так", "Ні");

    private Task<string?> AskFileName()
        => DisplayPromptAsync("Файл", "Назва файлу:", "OK", "Скасувати", initialValue: "lab74.json");

    private void ShowMessage(string text) => statusLabel.Text = text;

    // Перебудовує сітку, якщо змінився розмір, і оновлює тексти всіх клітинок
    private void RefreshGrid()
    {
        int rows = controller.GetTableRowCount();
        int columns = controller.GetTableColumnCount();
        if (cellEntries.GetLength(0) != rows || cellEntries.GetLength(1) != columns)
        {
            BuildGrid(rows, columns);
        }
        for (int r = 0; r < rows; r++)
        {
            for (int c = 0; c < columns; c++)
            {
                if (!cellEntries[r, c].IsFocused)   // не затираємо те, що користувач зараз редагує
                {
                    cellEntries[r, c].Text = controller.GetCellText(r, c);
                }
            }
        }
    }

    private void BuildGrid(int rows, int columns)
    {
        tableGrid.Children.Clear();
        tableGrid.RowDefinitions.Clear();
        tableGrid.ColumnDefinitions.Clear();

        tableGrid.ColumnDefinitions.Add(new ColumnDefinition(40));
        for (int c = 0; c < columns; c++) tableGrid.ColumnDefinitions.Add(new ColumnDefinition(110));
        for (int r = 0; r <= rows; r++) tableGrid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));

        for (int c = 0; c < columns; c++)
        {
            tableGrid.Add(Header(CellAddress.ColumnName(c)), c + 1, 0);
        }
        cellEntries = new Entry[rows, columns];
        for (int r = 0; r < rows; r++)
        {
            tableGrid.Add(Header((r + 1).ToString()), 0, r + 1);
            for (int c = 0; c < columns; c++)
            {
                int row = r, column = c;   // свої копії, інакше всі лямбди побачать останні r і c
                var entry = new Entry();
                entry.Focused += (_, _) => OnCellFocused(row, column);
                entry.Unfocused += (_, _) => OnCellUnfocused(new CellAddress(column, row).ToString(), entry.Text ?? "");
                cellEntries[r, c] = entry;
                tableGrid.Add(entry, c + 1, r + 1);
            }
        }
    }

    private static Label Header(string text) => new()
    {
        Text = text,
        FontAttributes = FontAttributes.Bold,
        HorizontalTextAlignment = TextAlignment.Center,
        VerticalTextAlignment = TextAlignment.Center,
    };
}
