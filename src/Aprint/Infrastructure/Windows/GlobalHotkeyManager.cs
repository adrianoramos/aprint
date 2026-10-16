using System.Runtime.InteropServices;

namespace Aprint.Infrastructure.Windows;

/// <summary>Registra um atalho de teclado global usando as APIs do Windows (RegisterHotKey).</summary>
public sealed class GlobalHotkeyManager : IDisposable
{
    private const int WmHotkey = 0x0312;
    private const int HotkeyId = 0xA100;
    private const int WhKeyboardLl = 13;
    private const int WmKeyDown = 0x0100;
    private const int WmKeyUp = 0x0101;
    private const int WmSysKeyDown = 0x0104;
    private const int WmSysKeyUp = 0x0105;

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

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc callback, IntPtr module, uint threadId);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool UnhookWindowsHookEx(IntPtr hook);

    [DllImport("user32.dll")]
    private static extern IntPtr CallNextHookEx(IntPtr hook, int nCode, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool PostMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);

    [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr GetModuleHandle(string? moduleName);

    private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

    private readonly HotkeyWindow _window = new();
    private readonly LowLevelKeyboardProc _keyboardHookCallback;
    private IntPtr _keyboardHook;
    private bool _isRegistered;
    private bool _isPrintScreenDown;

    public GlobalHotkeyManager()
    {
        _keyboardHookCallback = HandleLowLevelKeyboard;
    }

    public event Action? HotkeyPressed
    {
        add => _window.HotkeyPressed += value;
        remove => _window.HotkeyPressed -= value;
    }

    public bool Register(Keys key, bool ctrl, bool shift, bool alt, bool win)
    {
        Unregister();

        if (key == Keys.PrintScreen && !ctrl && !shift && !alt && !win)
        {
            _keyboardHook = SetWindowsHookEx(WhKeyboardLl, _keyboardHookCallback, GetModuleHandle(null), 0);
            return _keyboardHook != IntPtr.Zero;
        }

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
        if (_keyboardHook != IntPtr.Zero)
        {
            UnhookWindowsHookEx(_keyboardHook);
            _keyboardHook = IntPtr.Zero;
        }

        _isPrintScreenDown = false;

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

    private IntPtr HandleLowLevelKeyboard(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode >= 0 && Marshal.ReadInt32(lParam) == (int)Keys.PrintScreen)
        {
            var message = wParam.ToInt32();
            if (message is WmKeyDown or WmSysKeyDown)
            {
                if (!_isPrintScreenDown)
                {
                    _isPrintScreenDown = true;
                    PostMessage(_window.Handle, WmHotkey, (IntPtr)HotkeyId, IntPtr.Zero);
                }

                return (IntPtr)1;
            }

            if (message is WmKeyUp or WmSysKeyUp)
            {
                _isPrintScreenDown = false;
                return (IntPtr)1;
            }
        }

        return CallNextHookEx(_keyboardHook, nCode, wParam, lParam);
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
