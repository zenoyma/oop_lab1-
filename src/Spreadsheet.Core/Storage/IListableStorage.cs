namespace Spreadsheet.Core.Storage;

// Сховище, яке може показати список своїх таблиць (Google Drive).
// Окремо від ITableStorage, бо локальним файлам список не потрібен - їх обирають через FilePicker
public interface IListableStorage
{
    Task<List<string>> ListAsync();
}
