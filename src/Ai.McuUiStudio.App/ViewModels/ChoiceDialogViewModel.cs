namespace Ai.McuUiStudio.App.ViewModels;

public sealed class ChoiceDialogViewModel : ViewModelBase
{
    public ChoiceDialogViewModel(string title, string message, string primaryLabel, string secondaryLabel, string cancelLabel)
    {
        Title = title;
        Message = message;
        PrimaryLabel = primaryLabel;
        SecondaryLabel = secondaryLabel;
        CancelLabel = cancelLabel;
    }

    public string Title { get; }

    public string Message { get; }

    public string PrimaryLabel { get; }

    public string SecondaryLabel { get; }

    public string CancelLabel { get; }

    public bool HasSecondaryLabel => !string.IsNullOrWhiteSpace(SecondaryLabel);

    public bool HasCancelLabel => !string.IsNullOrWhiteSpace(CancelLabel);
}
