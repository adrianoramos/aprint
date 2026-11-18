using Aprint.Features.Settings;

namespace Aprint.Features.Welcome;

internal sealed class GettingStartedForm : Form
{
    public GettingStartedForm(AppSettings settings, Action openSettings)
    {
        Text = AppLocalization.Translate("Welcome.Title");
        ClientSize = new Size(540, 520);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.CenterScreen;
        Font = new Font("Segoe UI", 9F);
        Icon = Program.AppIcon;
        AcceptButton = CreateCloseButton();
        CancelButton = (Button)AcceptButton;

        var content = new FlowLayoutPanel
        {
            Bounds = new Rectangle(12, 12, 516, 452),
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
        };
        content.Controls.Add(CreateLabel("Welcome.Heading", 22F, FontStyle.Bold, 8));
        content.Controls.Add(CreateLabel("Welcome.Intro", 9F, FontStyle.Regular, 10));
        content.Controls.Add(CreateLabel("Welcome.CapabilitiesHeading", 11F, FontStyle.Bold, 4));
        content.Controls.Add(CreateLabel("Welcome.CapabilityCapture", 9F, FontStyle.Regular, 2));
        content.Controls.Add(CreateLabel("Welcome.CapabilityAnnotate", 9F, FontStyle.Regular, 2));
        content.Controls.Add(CreateLabel("Welcome.CapabilityShare", 9F, FontStyle.Regular, 10));
        content.Controls.Add(CreateLabel("Welcome.TrayHeading", 11F, FontStyle.Bold, 4));
        content.Controls.Add(CreateLabel("Welcome.TrayText", 9F, FontStyle.Regular, 10));
        content.Controls.Add(CreateLabel("Welcome.ShortcutsHeading", 11F, FontStyle.Bold, 4));
        content.Controls.Add(CreateShortcutTable(settings));

        Controls.Add(content);
        Controls.Add(CreateSettingsButton(openSettings));
        Controls.Add((Control)AcceptButton);
    }

    private static Button CreateSettingsButton(Action openSettings)
    {
        var button = new Button
        {
            Bounds = new Rectangle(358, 478, 80, 28),
            Size = new Size(80, 28),
            Anchor = AnchorStyles.Bottom | AnchorStyles.Right,
            Text = AppLocalization.Translate("Tray.Settings"),
            UseVisualStyleBackColor = true,
        };
        button.Click += (_, _) => openSettings();
        return button;
    }

    private static Button CreateCloseButton() => new()
    {
        Bounds = new Rectangle(446, 478, 80, 28),
        Size = new Size(80, 28),
        Anchor = AnchorStyles.Bottom | AnchorStyles.Right,
        DialogResult = DialogResult.OK,
        Text = AppLocalization.Translate("Welcome.Close"),
        UseVisualStyleBackColor = true,
    };

    private static Label CreateLabel(string resourceKey, float fontSize, FontStyle fontStyle, int bottomMargin) => new()
    {
        AutoSize = true,
        Font = new Font("Segoe UI", fontSize, fontStyle),
        Margin = new Padding(0, 0, 0, bottomMargin),
        MaximumSize = new Size(480, 0),
        Text = AppLocalization.Translate(resourceKey),
    };

    private static TableLayoutPanel CreateShortcutTable(AppSettings settings)
    {
        var table = new TableLayoutPanel
        {
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            ColumnCount = 2,
            Margin = new Padding(0, 0, 0, 4),
        };
        table.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        table.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

        var captureKey = settings.ForcePrintScreen
            ? Keys.PrintScreen
            : settings.HotkeyKey;
        var captureShortcut = settings.ForcePrintScreen
            ? FormatKey(captureKey)
            : FormatHotkey(captureKey, settings.HotkeyCtrl, settings.HotkeyShift, settings.HotkeyAlt, settings.HotkeyWin);
        AddShortcutRow(table, "Welcome.ShortcutCapture", captureShortcut);
        AddShortcutRow(table, "Welcome.ShortcutComplete", "Enter");
        AddShortcutRow(table, "Welcome.ShortcutEditor", "Ctrl+Enter");
        AddShortcutRow(table, "Welcome.ShortcutCopy", "Ctrl+C");
        AddShortcutRow(table, "Welcome.ShortcutSave", "Ctrl+S");
        AddShortcutRow(table, "Welcome.ShortcutCancel", "Esc");
        AddShortcutRow(table, "Welcome.ShortcutNewCapture", "Ctrl+N");
        AddShortcutRow(table, "Welcome.ShortcutStartSession", FormatHotkey(settings.StartSessionHotkey));
        AddShortcutRow(table, "Welcome.ShortcutEndSession", FormatHotkey(settings.EndSessionHotkey));
        return table;
    }

    private static void AddShortcutRow(TableLayoutPanel table, string labelKey, string shortcut)
    {
        var row = table.RowCount++;
        table.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        table.Controls.Add(new Label
        {
            AutoSize = true,
            Margin = new Padding(0, 1, 12, 2),
            Text = AppLocalization.Translate(labelKey),
        }, 0, row);
        table.Controls.Add(new Label
        {
            AutoSize = true,
            Font = new Font("Segoe UI", 9F, FontStyle.Bold),
            Margin = new Padding(0, 1, 0, 2),
            Text = shortcut,
        }, 1, row);
    }

    private static string FormatHotkey(HotkeySettings hotkey) =>
        FormatHotkey(hotkey.Key, hotkey.Ctrl, hotkey.Shift, hotkey.Alt, hotkey.Win);

    private static string FormatHotkey(Keys key, bool ctrl, bool shift, bool alt, bool win)
    {
        var parts = new List<string>(5);
        if (ctrl)
        {
            parts.Add("Ctrl");
        }

        if (shift)
        {
            parts.Add("Shift");
        }

        if (alt)
        {
            parts.Add("Alt");
        }

        if (win)
        {
            parts.Add("Win");
        }

        parts.Add(FormatKey(key));
        return string.Join("+", parts);
    }

    private static string FormatKey(Keys key) => key switch
    {
        Keys.PrintScreen => "Print Screen",
        >= Keys.D0 and <= Keys.D9 => ((char)('0' + (int)key - (int)Keys.D0)).ToString(),
        _ => key.ToString(),
    };
}
