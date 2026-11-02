using System.Text;
using System.Text.Json;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Drive.v3;
using Google.Apis.Services;
using Google.Apis.Util.Store;
using DriveFile = Google.Apis.Drive.v3.Data.File;

namespace Spreadsheet.Core.Storage;

// Збереження таблиці у Google Drive (той самий JSON, що й у файлі).
// Потрібен client_secret.json (OAuth-клієнт типу "Desktop app" з Google Cloud Console) у папці appFolder.
// Право DriveFile: програма бачить лише ті файли, які сама створила.
public class GoogleDriveStorage : ITableStorage
{
    private const string JsonMime = "application/json";

    private readonly string appFolder;
    private DriveService? service;

    public GoogleDriveStorage(string appFolder)
    {
        this.appFolder = appFolder;
    }

    public async Task SaveAsync(Table table, string name)
    {
        DriveService drive = await AuthorizeAsync();
        string json = JsonSerializer.Serialize(TableDto.FromTable(table));
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));

        string? id = await FindFileId(drive, name);
        if (id == null)
        {
            var create = drive.Files.Create(new DriveFile { Name = name, MimeType = JsonMime }, stream, JsonMime);
            await UploadOrThrow(create);
        }
        else
        {
            var update = drive.Files.Update(new DriveFile(), id, stream, JsonMime);   // files.update
            await UploadOrThrow(update);
        }
    }

    public async Task<Table> LoadAsync(string name)
    {
        DriveService drive = await AuthorizeAsync();
        string id = await FindFileId(drive, name)
                    ?? throw new FileNotFoundException($"На Google Drive немає файлу «{name}»");

        using var stream = new MemoryStream();
        await drive.Files.Get(id).DownloadAsync(stream);                                // files.get
        stream.Position = 0;
        var dto = await JsonSerializer.DeserializeAsync<TableDto>(stream)
                  ?? throw new InvalidDataException("Порожній файл");
        return dto.ToTable();
    }

    // OAuth: перший раз відкривається браузер для входу, далі токен береться з appFolder\token
    private async Task<DriveService> AuthorizeAsync()
    {
        if (service != null)
        {
            return service;
        }
        string secretPath = Path.Combine(appFolder, "client_secret.json");
        if (!File.Exists(secretPath))
        {
            throw new FileNotFoundException(
                $"Не знайдено {secretPath}. Створіть OAuth-клієнт (Desktop app) у Google Cloud Console, " +
                "увімкніть Google Drive API і покладіть завантажений JSON сюди під назвою client_secret.json.");
        }

        using var secretStream = File.OpenRead(secretPath);
        UserCredential credential = await GoogleWebAuthorizationBroker.AuthorizeAsync(
            GoogleClientSecrets.FromStream(secretStream).Secrets,
            new[] { DriveService.Scope.DriveFile },
            "user",
            CancellationToken.None,
            new FileDataStore(Path.Combine(appFolder, "token"), fullPath: true));

        service = new DriveService(new BaseClientService.Initializer
        {
            HttpClientInitializer = credential,
            ApplicationName = "Spreadsheet74",
        });
        return service;
    }

    // Шукає файл за назвою; null - файлу немає
    private static async Task<string?> FindFileId(DriveService drive, string name)
    {
        var request = drive.Files.List();
        request.Q = $"name = '{name.Replace("\\", "\\\\").Replace("'", "\\'")}' and trashed = false";
        request.Fields = "files(id)";
        var result = await request.ExecuteAsync();
        return result.Files.FirstOrDefault()?.Id;
    }

    private static async Task UploadOrThrow(Google.Apis.Upload.ResumableUpload upload)
    {
        var progress = await upload.UploadAsync();
        if (progress.Exception != null)
        {
            throw progress.Exception;
        }
    }
}
