using System.Text.Json;

namespace Spreadsheet.Core.Storage;

// Таблиця у JSON-файлі на диску
public class LocalFileStorage : ITableStorage
{
    private static readonly JsonSerializerOptions Options = new() { WriteIndented = true };

    public async Task SaveAsync(Table table, string path)
    {
        string json = JsonSerializer.Serialize(TableDto.FromTable(table), Options);
        await File.WriteAllTextAsync(path, json);
    }

    public async Task<Table> LoadAsync(string path)
    {
        string json = await File.ReadAllTextAsync(path);
        var dto = JsonSerializer.Deserialize<TableDto>(json) ?? throw new InvalidDataException("Порожній файл");
        return dto.ToTable();
    }
}
