using Microsoft.Extensions.Logging;
using Microsoft.Maui.LifecycleEvents;

namespace Spreadsheet.UI;

public static class MauiProgram
{
	public static MauiApp CreateMauiApp()
	{
		var builder = MauiApp.CreateBuilder();
		builder
			.UseMauiApp<App>()
			.ConfigureFonts(fonts =>
			{
				fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
				fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
			});

#if WINDOWS
		// Перед закриттям вікна питаємо про незбережені зміни (MainPage.OnClosing)
		builder.ConfigureLifecycleEvents(events => events.AddWindows(windows => windows.OnWindowCreated(window =>
		{
			bool confirmed = false;
			window.AppWindow.Closing += async (_, e) =>
			{
				if (confirmed || Application.Current?.MainPage is not MainPage page) return;
				e.Cancel = true;
				if (await page.OnClosing())
				{
					confirmed = true;
					window.Close();
				}
			};
		})));
#endif

#if DEBUG
		builder.Logging.AddDebug();
#endif

		return builder.Build();
	}
}
