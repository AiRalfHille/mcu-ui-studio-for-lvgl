namespace Ai.McuUiStudio.App.ViewModels;

public sealed class InfoDialogViewModel : ViewModelBase
{
    public InfoDialogViewModel(string title, string message, string closeLabel)
    {
        Title = title;
        Message = message;
        CloseLabel = closeLabel;
    }

    public string Title { get; }

    public string Message { get; }

    public string CloseLabel { get; }
}
