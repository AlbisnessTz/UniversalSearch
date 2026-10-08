namespace UniversalSearch;

public partial class App : Application
{
    public App(MainPage mainPage)
    {
        InitializeComponent();
        MainPage = new NavigationPage(mainPage)
        {
            BarBackgroundColor = Color.FromArgb("#0B0E14"),
            BarTextColor = Color.FromArgb("#F3F6FA")
        };
    }
}