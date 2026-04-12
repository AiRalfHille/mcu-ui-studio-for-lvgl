namespace Ai.McuUiStudio.App.ViewModels;

public sealed class EditorStateViewModel : ViewModelBase
{
    private const int MaxLogLength = 12000;
    private readonly List<string> _technicalLogEntries = [];
    private readonly List<string> _eventLogEntries = [];
    private string _emptyLogText = string.Empty;
    private string _noSimulatorText = string.Empty;
    private string _previewNotRenderedText = string.Empty;
    private bool _isDirty;
    private bool _isPreviewOutOfDate;
    private bool _isSimulatorConnected;
    private bool _autoRenderEnabled = true;
    private string _eventCallbackLog = string.Empty;
    private string _previewBackend = string.Empty;
    private string _previewLog = string.Empty;
    private string _previewStatus = string.Empty;
    private string _technicalLog = string.Empty;

    public bool IsDirty
    {
        get => _isDirty;
        set => SetProperty(ref _isDirty, value);
    }

    public bool IsPreviewOutOfDate
    {
        get => _isPreviewOutOfDate;
        set => SetProperty(ref _isPreviewOutOfDate, value);
    }

    public bool IsSimulatorConnected
    {
        get => _isSimulatorConnected;
        set => SetProperty(ref _isSimulatorConnected, value);
    }

    public bool AutoRenderEnabled
    {
        get => _autoRenderEnabled;
        set => SetProperty(ref _autoRenderEnabled, value);
    }

    public string PreviewStatus
    {
        get => _previewStatus;
        set => SetProperty(ref _previewStatus, value);
    }

    public string PreviewBackend
    {
        get => _previewBackend;
        set => SetProperty(ref _previewBackend, value);
    }

    public string PreviewLog
    {
        get => _previewLog;
        set => SetProperty(ref _previewLog, value);
    }

    public string TechnicalLog
    {
        get => _technicalLog;
        private set => SetProperty(ref _technicalLog, value);
    }

    public string EventCallbackLog
    {
        get => _eventCallbackLog;
        private set => SetProperty(ref _eventCallbackLog, value);
    }

    public void ConfigureLocalizedTexts(string emptyLogText, string noSimulatorText, string previewNotRenderedText)
    {
        _emptyLogText = string.IsNullOrWhiteSpace(emptyLogText) ? _emptyLogText : emptyLogText;
        _noSimulatorText = string.IsNullOrWhiteSpace(noSimulatorText) ? _noSimulatorText : noSimulatorText;
        _previewNotRenderedText = string.IsNullOrWhiteSpace(previewNotRenderedText) ? _previewNotRenderedText : previewNotRenderedText;

        if (_technicalLogEntries.Count == 0)
        {
            TechnicalLog = _emptyLogText;
        }

        if (_eventLogEntries.Count == 0)
        {
            EventCallbackLog = _emptyLogText;
        }

        if (string.IsNullOrWhiteSpace(PreviewLog) || string.Equals(PreviewLog, _emptyLogText, StringComparison.Ordinal))
        {
            PreviewLog = _emptyLogText;
        }

        if (string.IsNullOrWhiteSpace(PreviewBackend) || string.Equals(PreviewBackend, _noSimulatorText, StringComparison.Ordinal))
        {
            PreviewBackend = _noSimulatorText;
        }

        if (string.IsNullOrWhiteSpace(PreviewStatus) || string.Equals(PreviewStatus, _previewNotRenderedText, StringComparison.Ordinal))
        {
            PreviewStatus = _previewNotRenderedText;
        }
    }

    public void AppendPreviewLog(string line)
    {
        if (string.IsNullOrWhiteSpace(line))
        {
            return;
        }

        var stampedLine = $"[{DateTime.Now:HH:mm:ss}] {line}";
        var isEventCallback = stampedLine.Contains("event callback fired:", StringComparison.OrdinalIgnoreCase);
        var targetEntries = isEventCallback ? _eventLogEntries : _technicalLogEntries;
        targetEntries.Add(stampedLine);

        TrimEntries(targetEntries);

        TechnicalLog = _technicalLogEntries.Count == 0
            ? _emptyLogText
            : string.Join(Environment.NewLine, _technicalLogEntries);

        EventCallbackLog = _eventLogEntries.Count == 0
            ? _emptyLogText
            : string.Join(Environment.NewLine, _eventLogEntries);

        PreviewLog = string.Join(
            Environment.NewLine,
            _technicalLogEntries.Concat(_eventLogEntries).OrderBy(x => x, StringComparer.Ordinal));
    }

    public void ClearPreviewLog()
    {
        _technicalLogEntries.Clear();
        _eventLogEntries.Clear();
        TechnicalLog = _emptyLogText;
        EventCallbackLog = _emptyLogText;
        PreviewLog = _emptyLogText;
    }

    public void SetPreviewStatus(string statusText)
    {
        if (string.IsNullOrWhiteSpace(statusText))
        {
            return;
        }

        var stampedStatus = $"[{DateTime.Now:HH:mm:ss}] {statusText}";
        PreviewStatus = stampedStatus;
        AppendPreviewLog($"[status] {statusText}");
    }

    private static void TrimEntries(List<string> entries)
    {
        if (entries.Count == 0)
        {
            return;
        }

        while (string.Join(Environment.NewLine, entries).Length > MaxLogLength && entries.Count > 1)
        {
            entries.RemoveAt(0);
        }
    }
}
