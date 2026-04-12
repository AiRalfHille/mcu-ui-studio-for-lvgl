using System.Collections.ObjectModel;
using Ai.McuUiStudio.App.Services.Project;

namespace Ai.McuUiStudio.App.ViewModels;

public sealed class LvConfDialogViewModel : ViewModelBase
{
    private readonly LvConfDocument _document;
    private readonly LvConfFileService _service;
    private string _statusMessage = string.Empty;

    public LvConfDialogViewModel(
        string title,
        string fileLabel,
        string propertyHeader,
        string valueHeader,
        string descriptionHeader,
        string saveLabel,
        string cancelLabel,
        string saveSuccessFormat,
        string saveFailedFormat,
        LvConfDocument document,
        LvConfFileService service)
    {
        Title = title;
        FileLabel = fileLabel;
        PropertyHeader = propertyHeader;
        ValueHeader = valueHeader;
        DescriptionHeader = descriptionHeader;
        SaveLabel = saveLabel;
        CancelLabel = cancelLabel;
        SaveSuccessFormat = saveSuccessFormat;
        SaveFailedFormat = saveFailedFormat;
        _document = document;
        _service = service;
        FilePath = document.FilePath;

        Options = new ObservableCollection<LvConfOptionViewModel>(
            document.Entries.Select(x => new LvConfOptionViewModel(x.Name, x.Value, x.Description)));
    }

    public string Title { get; }

    public string FileLabel { get; }

    public string FilePath { get; }

    public string PropertyHeader { get; }

    public string ValueHeader { get; }

    public string DescriptionHeader { get; }

    public string SaveLabel { get; }

    public string CancelLabel { get; }

    public string SaveSuccessFormat { get; }

    public string SaveFailedFormat { get; }

    public ObservableCollection<LvConfOptionViewModel> Options { get; }

    public string StatusMessage
    {
        get => _statusMessage;
        set => SetProperty(ref _statusMessage, value);
    }

    public bool TrySave(out string? errorMessage)
    {
        errorMessage = null;

        try
        {
            _service.Save(
                _document,
                Options.Select(x => new LvConfOptionState(x.Name, x.Value)));

            StatusMessage = string.Format(SaveSuccessFormat, Path.GetFileName(FilePath));
            return true;
        }
        catch (Exception ex)
        {
            errorMessage = ex.Message;
            StatusMessage = string.Format(SaveFailedFormat, ex.Message);
            return false;
        }
    }
}
