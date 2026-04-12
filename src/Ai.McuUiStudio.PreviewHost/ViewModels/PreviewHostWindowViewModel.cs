namespace Ai.McuUiStudio.PreviewHost.ViewModels;

public sealed class PreviewHostWindowViewModel : ViewModelBase
{
    private string _documentName = "Warte auf C-Preview";
    private string _status = "Noch keine Daten empfangen.";
    private string _content = string.Empty;
    private DateTimeOffset? _lastUpdatedAt;

    public string DocumentName
    {
        get => _documentName;
        set => SetProperty(ref _documentName, value);
    }

    public string Status
    {
        get => _status;
        set => SetProperty(ref _status, value);
    }

    public string Content
    {
        get => _content;
        set => SetProperty(ref _content, value);
    }

    public DateTimeOffset? LastUpdatedAt
    {
        get => _lastUpdatedAt;
        set
        {
            if (SetProperty(ref _lastUpdatedAt, value))
            {
                RaisePropertyChanged(nameof(LastUpdatedLabel));
            }
        }
    }

    public string LastUpdatedLabel =>
        LastUpdatedAt.HasValue
            ? $"Letztes Update: {LastUpdatedAt:HH:mm:ss}"
            : "Noch kein Update";
}
