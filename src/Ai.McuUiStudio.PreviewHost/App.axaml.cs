using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Ai.McuUiStudio.PreviewHost.Services;
using Ai.McuUiStudio.PreviewHost.ViewModels;
using Ai.McuUiStudio.PreviewHost.Views;

namespace Ai.McuUiStudio.PreviewHost;

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
            var port = PreviewHostArguments.ParsePort(desktop.Args ?? []);
            var viewModel = new PreviewHostWindowViewModel();
            var server = new PreviewTcpServer(viewModel, desktop, port);

            desktop.MainWindow = new PreviewHostWindow
            {
                DataContext = viewModel
            };

            _ = server.StartAsync();
        }

        base.OnFrameworkInitializationCompleted();
    }
}
