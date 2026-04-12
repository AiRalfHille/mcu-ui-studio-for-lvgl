using System.Reflection;
using System.Text.Json;

namespace Ai.McuUiStudio.App.Services.Localization;

public sealed class LocalizationCatalog
{
    private const string ResourcePrefix = "Ai.McuUiStudio.App.Localization.";
    private readonly Dictionary<string, LanguageBundle> _bundles;
    private string _currentLanguageCode;

    private LocalizationCatalog(Dictionary<string, LanguageBundle> bundles, string currentLanguageCode)
    {
        _bundles = bundles;
        _currentLanguageCode = currentLanguageCode;
    }

    public IReadOnlyList<LanguageOption> AvailableLanguages =>
        _bundles.Values
            .OrderBy(x => x.Language.DisplayName, StringComparer.OrdinalIgnoreCase)
            .Select(x => x.Language)
            .ToArray();

    public string CurrentLanguageCode => _currentLanguageCode;

    public static LocalizationCatalog LoadEmbedded(string lvglVersion)
    {
        var assembly = Assembly.GetExecutingAssembly();
        var resourceNames = assembly.GetManifestResourceNames();
        var bundles = new Dictionary<string, LanguageBundle>(StringComparer.OrdinalIgnoreCase);

        foreach (var uiResource in resourceNames.Where(x => x.StartsWith($"{ResourcePrefix}ui.", StringComparison.OrdinalIgnoreCase) &&
                                                            x.EndsWith(".json", StringComparison.OrdinalIgnoreCase)))
        {
            var languageCode = ExtractLanguageCode(uiResource, "ui.");
            if (languageCode is null)
            {
                continue;
            }

            var uiDocument = ReadJson<UiLocalizationDocument>(assembly, uiResource);
            var lvglResource = $"{ResourcePrefix}lvgl-{lvglVersion}.{languageCode}.json";
            var lvglDocument = resourceNames.Contains(lvglResource, StringComparer.OrdinalIgnoreCase)
                ? ReadJson<LvglLocalizationDocument>(assembly, lvglResource)
                : null;

            var option = new LanguageOption(
                languageCode,
                uiDocument.Meta?.DisplayName ?? languageCode.ToUpperInvariant());

            bundles[languageCode] = new LanguageBundle(
                option,
                uiDocument.Strings ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase),
                lvglDocument?.Elements ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase),
                lvglDocument?.Attributes ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase),
                lvglDocument?.AttributeLabels ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase));
        }

        if (bundles.Count == 0)
        {
            var fallback = new LanguageOption("de", "Deutsch");
            bundles["de"] = new LanguageBundle(
                fallback,
                new Dictionary<string, string>(),
                new Dictionary<string, string>(),
                new Dictionary<string, string>(),
                new Dictionary<string, string>());
        }

        var defaultLanguage = bundles.ContainsKey("de")
            ? "de"
            : bundles.Keys.OrderBy(x => x, StringComparer.OrdinalIgnoreCase).First();

        return new LocalizationCatalog(bundles, defaultLanguage);
    }

    public void SetLanguage(string? languageCode)
    {
        if (string.IsNullOrWhiteSpace(languageCode) || !_bundles.ContainsKey(languageCode))
        {
            return;
        }

        _currentLanguageCode = languageCode;
    }

    public string GetUiString(string key)
    {
        if (_bundles.TryGetValue(_currentLanguageCode, out var bundle) &&
            bundle.UiStrings.TryGetValue(key, out var value) &&
            !string.IsNullOrWhiteSpace(value))
        {
            return value;
        }

        if (_bundles.TryGetValue("de", out var germanBundle) &&
            germanBundle.UiStrings.TryGetValue(key, out var germanValue) &&
            !string.IsNullOrWhiteSpace(germanValue))
        {
            return germanValue;
        }

        if (_bundles.TryGetValue("en", out var englishBundle) &&
            englishBundle.UiStrings.TryGetValue(key, out var englishValue) &&
            !string.IsNullOrWhiteSpace(englishValue))
        {
            return englishValue;
        }

        return key;
    }

    public string? GetElementDescription(string elementName)
    {
        if (_bundles.TryGetValue(_currentLanguageCode, out var bundle) &&
            bundle.ElementDescriptions.TryGetValue(elementName, out var value) &&
            !string.IsNullOrWhiteSpace(value))
        {
            return value;
        }

        return null;
    }

    public string? GetAttributeDescription(string attributeName)
    {
        if (_bundles.TryGetValue(_currentLanguageCode, out var bundle) &&
            bundle.AttributeDescriptions.TryGetValue(attributeName, out var value) &&
            !string.IsNullOrWhiteSpace(value))
        {
            return value;
        }

        return null;
    }

    public string? GetAttributeLabel(string attributeName)
    {
        if (_bundles.TryGetValue(_currentLanguageCode, out var bundle) &&
            bundle.AttributeLabels.TryGetValue(attributeName, out var value) &&
            !string.IsNullOrWhiteSpace(value))
        {
            return value;
        }

        return null;
    }

    private static T ReadJson<T>(Assembly assembly, string resourceName) where T : new()
    {
        using var stream = assembly.GetManifestResourceStream(resourceName)
                           ?? throw new InvalidOperationException($"Localization resource '{resourceName}' not found.");
        using var reader = new StreamReader(stream);
        var json = reader.ReadToEnd();
        return JsonSerializer.Deserialize<T>(json, new JsonSerializerOptions
               {
                   PropertyNameCaseInsensitive = true
               }) ?? new T();
    }

    private static string? ExtractLanguageCode(string resourceName, string marker)
    {
        var startIndex = resourceName.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
        if (startIndex < 0)
        {
            return null;
        }

        var suffix = resourceName[(startIndex + marker.Length)..];
        var endIndex = suffix.LastIndexOf(".json", StringComparison.OrdinalIgnoreCase);
        return endIndex > 0 ? suffix[..endIndex] : null;
    }

    private sealed record LanguageBundle(
        LanguageOption Language,
        Dictionary<string, string> UiStrings,
        Dictionary<string, string> ElementDescriptions,
        Dictionary<string, string> AttributeDescriptions,
        Dictionary<string, string> AttributeLabels);

    private sealed class UiLocalizationDocument
    {
        public UiLocalizationMeta? Meta { get; init; }

        public Dictionary<string, string>? Strings { get; init; }
    }

    private sealed class UiLocalizationMeta
    {
        public string? DisplayName { get; init; }
    }

    private sealed class LvglLocalizationDocument
    {
        public Dictionary<string, string>? Elements { get; init; }

        public Dictionary<string, string>? Attributes { get; init; }

        public Dictionary<string, string>? AttributeLabels { get; init; }
    }
}
