namespace Ai.McuUiStudio.App.ViewModels;

public sealed class TextInputDialogViewModel : ViewModelBase
{
    private string _value;

    public TextInputDialogViewModel(string title, string message, string value, string confirmLabel, string cancelLabel)
    {
        Title = title;
        Message = message;
        _value = value;
        ConfirmLabel = confirmLabel;
        CancelLabel = cancelLabel;
    }

    public string Title { get; }

    public string Message { get; }

    public string ConfirmLabel { get; }

    public string CancelLabel { get; }

    public string Value
    {
        get => _value;
        set
        {
            if (SetProperty(ref _value, value))
            {
                RaisePropertyChanged(nameof(CanConfirm));
            }
        }
    }

    public bool CanConfirm => !string.IsNullOrWhiteSpace(Value);
}
