namespace Ai.McuUiStudio.App.ViewModels;

public sealed class ThemeOptionViewModel : ViewModelBase
{
    private string _value;

    public ThemeOptionViewModel(string name, string value, string description)
    {
        Name = name;
        _value = value;
        Description = description;
    }

    public string Name { get; }

    public string Value
    {
        get => _value;
        set => SetProperty(ref _value, value);
    }

    public string Description { get; }
}
