namespace Ai.McuUiStudio.App.ViewModels;

public sealed class ConfirmDialogViewModel : ViewModelBase
{
    public ConfirmDialogViewModel(string title, string message, string yesLabel, string noLabel)
    {
        Title = title;
        Message = message;
        YesLabel = yesLabel;
        NoLabel = noLabel;
    }

    public string Title { get; }

    public string Message { get; }

    public string YesLabel { get; }

    public string NoLabel { get; }
}
