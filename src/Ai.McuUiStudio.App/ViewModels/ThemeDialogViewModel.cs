using Ai.McuUiStudio.App.Services.Localization;
using Ai.McuUiStudio.App.Services.Project;

namespace Ai.McuUiStudio.App.ViewModels;

public sealed class ThemeDialogViewModel : ViewModelBase
{
    private readonly LocalizationCatalog _localizationCatalog;
    private readonly ThemeProjectDocument _document;
    private readonly ThemeFileService _service;
    private string _primaryColor;
    private string _secondaryColor;
    private bool _darkModeEnabled;
    private string _fontName;
    private string _statusMessage = string.Empty;

    public ThemeDialogViewModel(
        LocalizationCatalog localizationCatalog,
        string title,
        string fileLabel,
        string propertyHeader,
        string valueHeader,
        string descriptionHeader,
        string saveLabel,
        string cancelLabel,
        string saveSuccessFormat,
        string saveFailedFormat,
        ThemeProjectDocument document,
        ThemeFileService service)
    {
        _localizationCatalog = localizationCatalog;
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
        _primaryColor = document.PrimaryColor;
        _secondaryColor = document.SecondaryColor;
        _darkModeEnabled = string.Equals(document.DarkMode, "true", StringComparison.OrdinalIgnoreCase);
        _fontName = document.FontName;
    }

    private string Ui(string key) => _localizationCatalog.GetUiString(key);

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

    public string PrimaryColorLabel => Ui("dialog.theme.option.primary.name");

    public string PrimaryColorDescription => Ui("dialog.theme.option.primary.description");

    public string SecondaryColorLabel => Ui("dialog.theme.option.secondary.name");

    public string SecondaryColorDescription => Ui("dialog.theme.option.secondary.description");

    public string DarkModeLabel => Ui("dialog.theme.option.dark.name");

    public string DarkModeDescription => Ui("dialog.theme.option.dark.description");

    public string FontLabel => Ui("dialog.theme.option.font.name");

    public string FontDescription => Ui("dialog.theme.option.font.description");

    public string PrimaryColor
    {
        get => _primaryColor;
        set => SetProperty(ref _primaryColor, value);
    }

    public string SecondaryColor
    {
        get => _secondaryColor;
        set => SetProperty(ref _secondaryColor, value);
    }

    public bool DarkModeEnabled
    {
        get => _darkModeEnabled;
        set => SetProperty(ref _darkModeEnabled, value);
    }

    public string FontName
    {
        get => _fontName;
        set => SetProperty(ref _fontName, value);
    }

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
            _service.Save(_document, PrimaryColor, SecondaryColor, DarkModeEnabled ? "true" : "false", FontName);
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
