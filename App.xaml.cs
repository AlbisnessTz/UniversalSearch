namespace UniversalSearch;

public partial class App : Application
{
    private readonly MainPage _mainPage;

    public App(MainPage mainPage)
    {
        InitializeComponent();
        _mainPage = mainPage;
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        return new Window(new NavigationPage(_mainPage)
        {
            BarBackgroundColor = Color.FromArgb("#0B0E14"),
            BarTextColor = Color.FromArgb("#F3F6FA")
        });
    }
}
