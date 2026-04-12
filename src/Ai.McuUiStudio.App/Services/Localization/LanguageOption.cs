namespace Ai.McuUiStudio.App.Services.Localization;

public sealed record LanguageOption(string Code, string DisplayName)
{
    public override string ToString() => DisplayName;
}
