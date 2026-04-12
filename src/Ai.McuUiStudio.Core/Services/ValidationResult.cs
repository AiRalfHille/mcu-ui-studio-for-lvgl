namespace Ai.McuUiStudio.Core.Services;

public sealed class ValidationResult
{
    public List<string> Errors { get; } = [];

    public bool IsValid => Errors.Count == 0;
}
