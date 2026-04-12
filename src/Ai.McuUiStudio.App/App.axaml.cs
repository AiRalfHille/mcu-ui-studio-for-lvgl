using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Ai.McuUiStudio.App.Services;
using Ai.McuUiStudio.App.Services.Documentation;
using Ai.McuUiStudio.App.Services.Preview;
using Ai.McuUiStudio.App.ViewModels;
using Ai.McuUiStudio.App.Views;

namespace Ai.McuUiStudio.App;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var documentationRoot = AppRuntimePaths.ResolveDocumentationRoot();
            var documentationServer = new DocumentationServerService(documentationRoot);
            documentationServer.TryStart(out _);

            var mainWindowViewModel = new MainWindowViewModel(documentationServer);
            desktop.ShutdownRequested += (_, _) =>
            {
                Task.Run(() => mainWindowViewModel.ShutdownPreviewAsync()).GetAwaiter().GetResult();
                documentationServer.Dispose();
            };

            desktop.MainWindow = new MainWindow
            {
                DataContext = mainWindowViewModel
            };
        }

        base.OnFrameworkInitializationCompleted();
    }
}
