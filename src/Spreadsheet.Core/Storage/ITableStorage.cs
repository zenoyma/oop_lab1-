namespace Spreadsheet.Core.Storage;

// Збереження і відкриття таблиці. Контролер знає тільки цей інтерфейс,
// тому інший спосіб збереження (напр. Google Drive) додається без змін у контролері
public interface ITableStorage
{
    Task SaveAsync(Table table, string name);
    Task<Table> LoadAsync(string name);
}
