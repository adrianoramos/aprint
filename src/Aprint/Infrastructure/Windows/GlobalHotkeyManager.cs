using System.Runtime.InteropServices;

namespace Aprint.Infrastructure.Windows;

/// <summary>Registra um atalho de teclado global usando as APIs do Windows (RegisterHotKey).</summary>
public sealed class GlobalHotkeyManager : IDisposable
{
    private const int WmHotkey = 0x0312;
    private const int HotkeyId = 0xA100;

    [Flags]
    private enum Modifiers : uint
    {
        None = 0x0000,
        Alt = 0x0001,
        Control = 0x0002,
        Shift = 0x0004,
        Win = 0x0008,
        NoRepeat = 0x4000,
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

    private readonly HotkeyWindow _window = new();
    private bool _isRegistered;

    public event Action? HotkeyPressed
    {
        add => _window.HotkeyPressed += value;
        remove => _window.HotkeyPressed -= value;
    }

    public bool Register(Keys key, bool ctrl, bool shift, bool alt, bool win)
    {
        Unregister();

        var modifiers = Modifiers.NoRepeat;
        if (ctrl) modifiers |= Modifiers.Control;
        if (shift) modifiers |= Modifiers.Shift;
        if (alt) modifiers |= Modifiers.Alt;
        if (win) modifiers |= Modifiers.Win;

        _isRegistered = RegisterHotKey(_window.Handle, HotkeyId, (uint)modifiers, (uint)key);
        return _isRegistered;
    }

    public void Unregister()
    {
        if (_isRegistered)
        {
            UnregisterHotKey(_window.Handle, HotkeyId);
            _isRegistered = false;
        }
    }

    public void Dispose()
    {
        Unregister();
        _window.Dispose();
    }

    private sealed class HotkeyWindow : NativeWindow, IDisposable
    {
        public event Action? HotkeyPressed;

        public HotkeyWindow() => CreateHandle(new CreateParams());

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == WmHotkey && m.WParam.ToInt32() == HotkeyId)
            {
                HotkeyPressed?.Invoke();
            }

            base.WndProc(ref m);
        }

        public void Dispose() => DestroyHandle();
    }
}
