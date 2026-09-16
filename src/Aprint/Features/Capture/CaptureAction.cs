namespace Aprint.Features.Capture;

/// <summary>Ação a ser executada após a captura de um print.</summary>
public enum CaptureAction
{
    ClipboardOnly,
    SaveToDiskOnly,
    Both,
}
