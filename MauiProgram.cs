using Microsoft.Extensions.Logging;
using UniversalSearch.Services;

namespace UniversalSearch;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder.UseMauiApp<App>();
#if DEBUG
        builder.Logging.AddDebug();
#endif
        builder.Services.AddSingleton<ISearchService, LocalSearchService>();
        builder.Services.AddSingleton<MainPage>();
        return builder.Build();
    }
}