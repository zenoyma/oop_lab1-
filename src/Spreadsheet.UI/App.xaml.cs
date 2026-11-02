namespace Spreadsheet.UI;

public partial class App : Application
{
	public App()
	{
		InitializeComponent();

		MainPage = new NavigationPage(new MainPage());   // без NavigationPage меню на Windows не видно
	}
}
