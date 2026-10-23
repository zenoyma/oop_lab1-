using Spreadsheet.Core;

namespace Spreadsheet.UI;

// Головне вікно. Поки що макет: сітка і кнопки, обчислень ще немає
public partial class MainPage : ContentPage
{
    private const int DefaultRows = 10;
    private const int DefaultColumns = 10;

    private ViewMode mode = ViewMode.Value;

    public MainPage()
    {
        InitializeComponent();
        BuildGrid(DefaultRows, DefaultColumns);
    }

    private void OnCellFocused(int row, int column, Entry entry)
    {
        cellLabel.Text = new CellAddress(column, row).ToString();
        formulaEntry.Text = entry.Text;
    }

    private void OnFormulaCompleted(object? sender, EventArgs e) => NotReady();

    public void OnModeButtonClicked(object? sender, EventArgs e)
    {
        mode = mode == ViewMode.Value ? ViewMode.Expression : ViewMode.Value;
        modeButton.Text = mode == ViewMode.Value ? "Режим: ЗНАЧЕННЯ" : "Режим: ВИРАЗ";
    }

    public void OnNewTableClicked(object? sender, EventArgs e) => NotReady();
    public void OnOpenFileClicked(object? sender, EventArgs e) => NotReady();
    public void OnSaveFileClicked(object? sender, EventArgs e) => NotReady();
    public void OnOpenDriveClicked(object? sender, EventArgs e) => NotReady();
    public void OnSaveDriveClicked(object? sender, EventArgs e) => NotReady();
    public void OnAddRowClicked(object? sender, EventArgs e) => NotReady();
    public void OnRemoveRowClicked(object? sender, EventArgs e) => NotReady();
    public void OnAddColumnClicked(object? sender, EventArgs e) => NotReady();
    public void OnRemoveColumnClicked(object? sender, EventArgs e) => NotReady();

    public async void OnHelpClicked(object? sender, EventArgs e) => await DisplayAlert("Довідка",
        "Лабораторна робота 1*, варіант 74.\n\n" +
        "Операції:\n" +
        "  inc(x), dec(x)\n" +
        "  max(x, y), min(x, y)\n" +
        "  mmax(x1, …, xN), mmin(x1, …, xN)\n" +
        "  =  <  >  <=  >=  <>", "OK");

    private void NotReady() => statusLabel.Text = "Ще не реалізовано (макет інтерфейсу)";

    private void BuildGrid(int rows, int columns)
    {
        tableGrid.ColumnDefinitions.Add(new ColumnDefinition(40));
        for (int c = 0; c < columns; c++) tableGrid.ColumnDefinitions.Add(new ColumnDefinition(110));
        for (int r = 0; r <= rows; r++) tableGrid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));

        for (int c = 0; c < columns; c++)
        {
            tableGrid.Add(Header(CellAddress.ColumnName(c)), c + 1, 0);
        }
        for (int r = 0; r < rows; r++)
        {
            tableGrid.Add(Header((r + 1).ToString()), 0, r + 1);
            for (int c = 0; c < columns; c++)
            {
                int row = r, column = c;   // свої копії, інакше всі лямбди побачать останні r і c
                var entry = new Entry();
                entry.Focused += (_, _) => OnCellFocused(row, column, entry);
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
