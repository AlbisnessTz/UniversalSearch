using Microsoft.UI.Xaml;

namespace UniversalSearch.WinUI;

public partial class App : MauiWinUIApplication
{
    public App()
    {
        InitializeComponent();
    }

    protected override MauiApp CreateMauiApp() => UniversalSearch.MauiProgram.CreateMauiApp();
}