namespace Aprint.Features.Editor;

internal static class ToolStripColorPalette
{
    public static readonly Color BarBackground = Color.FromArgb(246, 247, 249);
    public static readonly Color ButtonBackground = BarBackground;
    public static readonly Color ButtonForeground = Color.FromArgb(45, 49, 54);
    public static readonly Color ActiveBackground = Color.FromArgb(232, 239, 244);
    public static readonly Color ActiveBorder = Color.FromArgb(183, 197, 207);
    public static readonly Color PrimaryButtonBackground = Color.FromArgb(0, 103, 192);
    public static readonly Color PrimaryButtonHover = Color.FromArgb(0, 90, 158);
    public static readonly Color PrimaryButtonBorder = Color.FromArgb(0, 70, 130);

    public static ProfessionalColorTable CreateColorTable() => new SharedColorTable();

    private sealed class SharedColorTable : ProfessionalColorTable
    {
        public override Color ToolStripGradientBegin => BarBackground;
        public override Color ToolStripGradientMiddle => BarBackground;
        public override Color ToolStripGradientEnd => BarBackground;
        public override Color ToolStripBorder => ActiveBorder;
        public override Color ButtonSelectedGradientBegin => ActiveBackground;
        public override Color ButtonSelectedGradientMiddle => ActiveBackground;
        public override Color ButtonSelectedGradientEnd => ActiveBackground;
        public override Color ButtonSelectedBorder => ActiveBorder;
        public override Color ButtonCheckedGradientBegin => ActiveBackground;
        public override Color ButtonCheckedGradientMiddle => ActiveBackground;
        public override Color ButtonCheckedGradientEnd => ActiveBackground;
        public override Color ButtonPressedGradientBegin => ActiveBackground;
        public override Color ButtonPressedGradientMiddle => ActiveBackground;
        public override Color ButtonPressedGradientEnd => ActiveBackground;
        public override Color ButtonPressedBorder => ActiveBorder;
    }
}