using Avalonia.Media;
using Avalonia;

namespace Ai.McuUiStudio.App.ViewModels;

public sealed class AttributeEditorViewModel : ViewModelBase
{
    private readonly Action<string, string?> _onChanged;
    private bool _boolValue;
    private string? _editValue;
    private string? _value;
    private readonly string _emptyOptionLabel;
    private readonly string _requiredLabel;
    private readonly string _optionalLabel;
    private readonly IReadOnlyDictionary<string, string> _allowedValueLabels;

    public AttributeEditorViewModel(
        string name,
        string storageName,
        string displayName,
        string? value,
        string typeLabel,
        string category,
        string? tooltip,
        bool isRequired,
        IReadOnlyList<string>? allowedValues,
        bool isSupported,
        string emptyOptionLabel,
        string requiredLabel,
        string optionalLabel,
        IReadOnlyDictionary<string, string>? allowedValueLabels,
        bool isMcuRelated,
        Action<string, string?> onChanged)
    {
        Name = name;
        StorageName = storageName;
        DisplayName = displayName;
        TypeLabel = typeLabel;
        Category = category;
        Tooltip = tooltip;
        IsRequired = isRequired;
        AllowedValues = allowedValues ?? [];
        IsSupported = isSupported;
        _emptyOptionLabel = emptyOptionLabel;
        _requiredLabel = requiredLabel;
        _optionalLabel = optionalLabel;
        _allowedValueLabels = allowedValueLabels ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        IsMcuRelated = isMcuRelated;
        AllowedValueOptions = BuildAllowedValueOptions(TypeLabel, AllowedValues, IsRequired, _emptyOptionLabel, _allowedValueLabels);
        _value = value;
        _editValue = value;
        _boolValue = string.Equals(value, "true", StringComparison.OrdinalIgnoreCase);
        _onChanged = onChanged;
    }

    public string Name { get; }

    public string StorageName { get; }

    public string DisplayName { get; }

    public string TypeLabel { get; }

    public string Category { get; }

    public string? Tooltip { get; }

    public bool IsRequired { get; }

    public IReadOnlyList<string> AllowedValues { get; }

    public bool IsSupported { get; }

    public IReadOnlyList<AllowedValueOption> AllowedValueOptions { get; }

    public bool IsMcuRelated { get; }

    public bool IsBoolean => string.Equals(TypeLabel, "boolean", StringComparison.OrdinalIgnoreCase);

    public bool IsColor => string.Equals(TypeLabel, "color", StringComparison.OrdinalIgnoreCase);

    public bool HasAllowedValues => AllowedValues.Count > 0 && !IsBoolean;

    public bool ShowTextEditor => !IsBoolean && !HasAllowedValues && !IsColor;

    public bool ShowComboEditor => HasAllowedValues && !IsColor;

    public bool ShowBooleanEditor => IsBoolean;

    public bool ShowColorEditor => IsColor;

    public string RequirementLabel => IsRequired ? _requiredLabel : _optionalLabel;

    public bool ShowRequiredMarker => IsRequired;

    public double NameOpacity => IsRequired ? 1.0 : 0.82;

    public IBrush NameBrush => IsSupported
        ? (IsMcuRelated ? new SolidColorBrush(Color.Parse("#2F8F4E")) : Brushes.Black)
        : new SolidColorBrush(Color.Parse("#A14F1A"));

    public FontWeight NameFontWeight => IsMcuRelated ? FontWeight.Bold : FontWeight.Normal;

    public AllowedValueOption? SelectedAllowedValue
    {
        get
        {
            var matched = AllowedValueOptions.FirstOrDefault(x =>
                string.Equals(x.Value, EditValue, StringComparison.OrdinalIgnoreCase));

            if (matched is not null)
            {
                return matched;
            }

            return !IsRequired && string.IsNullOrWhiteSpace(EditValue)
                ? new AllowedValueOption(null, _emptyOptionLabel)
                : null;
        }
        set
        {
            if (value is null)
            {
                return;
            }

            Value = value.Value;
        }
    }

    public static string CreateFallbackDisplayName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return string.Empty;
        }

        return string.Join(
            " ",
            name
                .Split('_', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(part => char.ToUpperInvariant(part[0]) + part[1..]));
    }

    public bool BoolValue
    {
        get => _boolValue;
        set
        {
            if (SetProperty(ref _boolValue, value))
            {
                Value = value ? "true" : "false";
            }
        }
    }

    public string? EditValue
    {
        get => _editValue;
        set
        {
            var normalizedValue = NormalizeValue(value);

            if (SetProperty(ref _editValue, normalizedValue))
            {
                RaisePropertyChanged(nameof(SelectedAllowedValue));
            }
        }
    }

    public string? Value
    {
        get => _value;
        set
        {
            var normalizedValue = NormalizeValue(value);

            if (SetProperty(ref _value, normalizedValue))
            {
                SetProperty(ref _editValue, normalizedValue, nameof(EditValue));
                RaisePropertyChanged(nameof(SelectedAllowedValue));

                if (IsBoolean)
                {
                    var parsed = string.Equals(normalizedValue, "true", StringComparison.OrdinalIgnoreCase);
                    SetProperty(ref _boolValue, parsed, nameof(BoolValue));
                }

                _onChanged(StorageName, normalizedValue);
            }
        }
    }

    public void CommitEditValue()
    {
        Value = EditValue;
    }

    public void SetValueFromModel(string? value)
    {
        var normalizedValue = NormalizeValue(value);
        SetProperty(ref _value, normalizedValue, nameof(Value));
        SetProperty(ref _editValue, normalizedValue, nameof(EditValue));
        RaisePropertyChanged(nameof(SelectedAllowedValue));

        if (IsBoolean)
        {
            var parsed = string.Equals(normalizedValue, "true", StringComparison.OrdinalIgnoreCase);
            SetProperty(ref _boolValue, parsed, nameof(BoolValue));
        }
    }

    private string? NormalizeValue(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var trimmed = value.Trim();

        if (IsColor && trimmed.StartsWith('#'))
        {
            return $"0x{trimmed[1..]}";
        }

        return trimmed;
    }

    private static IReadOnlyList<AllowedValueOption> BuildAllowedValueOptions(
        string typeLabel,
        IReadOnlyList<string> allowedValues,
        bool isRequired,
        string emptyOptionLabel,
        IReadOnlyDictionary<string, string> allowedValueLabels)
    {
        if (allowedValues.Count == 0)
        {
            return [];
        }

        var options = new List<AllowedValueOption>();

        if (!isRequired)
        {
            options.Add(new AllowedValueOption(null, emptyOptionLabel));
        }

        if (string.Equals(typeLabel, "color", StringComparison.OrdinalIgnoreCase))
        {
            options.AddRange(allowedValues
                .Select(value =>
                {
                    var displayText = allowedValueLabels.TryGetValue(value, out var name)
                        ? name
                        : value;

                    return new AllowedValueOption(value, displayText);
                }));

            return options.ToArray();
        }

        options.AddRange(allowedValues.Select(value =>
            new AllowedValueOption(
                value,
                allowedValueLabels.TryGetValue(value, out var label) ? label : value)));
        return options.ToArray();
    }
}

public sealed record AllowedValueOption(string? Value, string DisplayText);
