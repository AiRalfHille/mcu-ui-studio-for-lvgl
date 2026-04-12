using System.Text.RegularExpressions;

namespace Ai.McuUiStudio.App.Services.Project;

public sealed class LvConfFileService
{
    private static readonly Regex DefineRegex = new(
        @"^(?<indent>\s*)#define\s+(?<name>[A-Z0-9_]+)\s+(?<value>.+?)(?<comment>\s*(?://.*|/\*.*\*/)\s*)?$",
        RegexOptions.Compiled);

    private static readonly IReadOnlyDictionary<string, string> KnownDescriptions =
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["LV_COLOR_DEPTH"] = "Farbtiefe des Displays, z. B. 16 fuer RGB565.",
            ["LV_DEF_REFR_PERIOD"] = "Standard-Refresh-Intervall in Millisekunden.",
            ["LV_DPI_DEF"] = "Referenz-DPI fuer Layout und Standardgroessen.",
            ["LV_MEM_SIZE"] = "Groesse des internen LVGL-Speicherpools.",
            ["LV_USE_LOG"] = "Aktiviert das LVGL-Logging.",
            ["LV_LOG_LEVEL"] = "Legt die Detailtiefe der Log-Ausgaben fest.",
            ["LV_USE_OBJ_NAME"] = "Erlaubt Objektnamen fuer Debugging und Editorbezug.",
            ["LV_USE_SDL"] = "Aktiviert SDL als Plattform fuer die Preview.",
            ["LV_SDL_ACCELERATED"] = "Nutzt beschleunigtes Rendering in SDL, wenn verfuegbar.",
            ["LV_SDL_FULLSCREEN"] = "Startet die SDL-Preview im Vollbildmodus.",
            ["LV_USE_THEME_DEFAULT"] = "Aktiviert das Standard-LVGL-Theme.",
            ["LV_THEME_DEFAULT_DARK"] = "Schaltet das Standard-Theme auf dunkle Variante.",
            ["LV_THEME_DEFAULT_GROW"] = "Aktiviert leichte Wachstumsanimationen im Theme.",
            ["LV_FONT_MONTSERRAT_14"] = "Bindet die Montserrat-Schrift in 14 px ein.",
            ["LV_FONT_MONTSERRAT_16"] = "Bindet die Montserrat-Schrift in 16 px ein.",
            ["LV_FONT_MONTSERRAT_20"] = "Bindet die Montserrat-Schrift in 20 px ein.",
            ["LV_USE_LABEL"] = "Aktiviert das Label-Widget.",
            ["LV_USE_BUTTON"] = "Aktiviert das Button-Widget.",
            ["LV_USE_IMAGE"] = "Aktiviert das Image-Widget.",
            ["LV_USE_SWITCH"] = "Aktiviert das Switch-Widget.",
            ["LV_USE_TABLE"] = "Aktiviert das Table-Widget.",
            ["LV_USE_DROPDOWN"] = "Aktiviert das Dropdown-Widget.",
            ["LV_USE_LIST"] = "Aktiviert das List-Widget.",
            ["LV_USE_MENU"] = "Aktiviert das Menu-Widget.",
            ["LV_USE_TABVIEW"] = "Aktiviert das TabView-Widget.",
            ["LV_USE_TILEVIEW"] = "Aktiviert das TileView-Widget.",
            ["LV_USE_WIN"] = "Aktiviert das Window-Widget.",
            ["LV_USE_CHART"] = "Aktiviert das Chart-Widget.",
            ["LV_USE_LED"] = "Aktiviert das LED-Widget."
        };

    public LvConfDocument Load(string filePath)
    {
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException("lv_conf-Datei wurde nicht gefunden.", filePath);
        }

        var lines = File.ReadAllLines(filePath).ToList();
        var entries = new List<LvConfDefineEntry>();

        for (var index = 0; index < lines.Count; index++)
        {
            var line = lines[index];
            var match = DefineRegex.Match(line);
            if (!match.Success)
            {
                continue;
            }

            var name = match.Groups["name"].Value.Trim();
            if (!name.StartsWith("LV_", StringComparison.Ordinal))
            {
                continue;
            }

            var value = match.Groups["value"].Value.Trim();
            var comment = match.Groups["comment"].Success ? match.Groups["comment"].Value : string.Empty;
            entries.Add(new LvConfDefineEntry(
                index,
                name,
                value,
                ResolveDescription(name, comment),
                comment));
        }

        return new LvConfDocument(filePath, lines, entries);
    }

    public void Save(LvConfDocument document, IEnumerable<LvConfOptionState> rows)
    {
        foreach (var row in rows)
        {
            var entry = document.Entries.FirstOrDefault(x => string.Equals(x.Name, row.Name, StringComparison.Ordinal));
            if (entry is null)
            {
                continue;
            }

            var rebuiltLine = $"#define {entry.Name} {row.Value?.Trim() ?? string.Empty}".TrimEnd();
            if (!string.IsNullOrWhiteSpace(entry.CommentSuffix))
            {
                rebuiltLine = $"{rebuiltLine} {entry.CommentSuffix.Trim()}";
            }

            document.Lines[entry.LineIndex] = rebuiltLine;
        }

        File.WriteAllLines(document.FilePath, document.Lines);
    }

    private static string ResolveDescription(string name, string commentSuffix)
    {
        if (KnownDescriptions.TryGetValue(name, out var description))
        {
            return description;
        }

        var cleaned = commentSuffix
            .Replace("/*", string.Empty, StringComparison.Ordinal)
            .Replace("*/", string.Empty, StringComparison.Ordinal)
            .Replace("//", string.Empty, StringComparison.Ordinal)
            .Trim();

        return cleaned;
    }
}

public sealed class LvConfDocument
{
    public LvConfDocument(string filePath, List<string> lines, List<LvConfDefineEntry> entries)
    {
        FilePath = filePath;
        Lines = lines;
        Entries = entries;
    }

    public string FilePath { get; }

    public List<string> Lines { get; }

    public List<LvConfDefineEntry> Entries { get; }
}

public sealed record LvConfDefineEntry(
    int LineIndex,
    string Name,
    string Value,
    string Description,
    string CommentSuffix);

public sealed record LvConfOptionState(string Name, string Value);
