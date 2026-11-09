using System.Runtime.InteropServices;

namespace Aprint.Infrastructure.Windows;

/// <summary>Registra um atalho de teclado global usando as APIs do Windows (RegisterHotKey).</summary>
public sealed class GlobalHotkeyManager : IDisposable
{
    private const int WmHotkey = 0x0312;
    private const int FirstHotkeyId = 0xA100;
    private const int WhKeyboardLl = 13;
    private const int WmKeyDown = 0x0100;
    private const int WmKeyUp = 0x0101;
    private const int WmSysKeyDown = 0x0104;
    private const int WmSysKeyUp = 0x0105;
    private const uint WmQuit = 0x0012;

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

    [DllImport("kernel32.dll")]
    private static extern uint GetCurrentThreadId();

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool PostThreadMessage(uint threadId, uint message, UIntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll")]
    private static extern int GetMessage(out NativeMessage message, IntPtr window, uint minimum, uint maximum);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool PeekMessage(out NativeMessage message, IntPtr window, uint minimum, uint maximum, uint remove);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool TranslateMessage(in NativeMessage message);

    [DllImport("user32.dll")]
    private static extern IntPtr DispatchMessage(in NativeMessage message);

    [StructLayout(LayoutKind.Sequential)]
    private struct NativeMessage
    {
        public IntPtr Window;
        public uint Message;
        public UIntPtr WParam;
        public IntPtr LParam;
        public uint Time;
        public int PointX;
        public int PointY;
        public uint Private;
    }

    private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

    private readonly HotkeyWindow _window = new();
    private readonly Dictionary<int, GlobalHotkeyCommand> _commandsById = [];
    private readonly HashSet<int> _registeredHotkeyIds = [];
    private KeyboardHookThread? _keyboardHookThread;
    private int? _printScreenHotkeyId;
    private int _nextHotkeyId;

    public GlobalHotkeyManager() => _window.HotkeyPressed += DispatchHotkey;

    public event Action? HotkeyPressed;

    public event Action<GlobalHotkeyCommand>? CommandPressed;

    public bool Register(Keys key, bool ctrl, bool shift, bool alt, bool win)
    {
        return RegisterMany([new GlobalHotkeyBinding(GlobalHotkeyCommand.Capture, key, ctrl, shift, alt, win)]).Count == 0;
    }

    public IReadOnlyList<GlobalHotkeyCommand> RegisterMany(IEnumerable<GlobalHotkeyBinding> bindings)
    {
        ArgumentNullException.ThrowIfNull(bindings);
        Unregister();
        var failedCommands = new List<GlobalHotkeyCommand>();
        foreach (var binding in bindings)
        {
            var id = FirstHotkeyId + _nextHotkeyId++;
            if (binding.Key == Keys.PrintScreen)
            {
                if (binding.Ctrl || binding.Shift || binding.Alt || binding.Win || _keyboardHookThread is not null)
                {
                    failedCommands.Add(binding.Command);
                    continue;
                }

                var hookThread = new KeyboardHookThread(_window.Handle, id);
                if (!hookThread.Start())
                {
                    hookThread.Dispose();
                    failedCommands.Add(binding.Command);
                    continue;
                }

                _keyboardHookThread = hookThread;
                _printScreenHotkeyId = id;
                _commandsById[id] = binding.Command;
                continue;
            }

            var modifiers = Modifiers.NoRepeat;
            if (binding.Ctrl) modifiers |= Modifiers.Control;
            if (binding.Shift) modifiers |= Modifiers.Shift;
            if (binding.Alt) modifiers |= Modifiers.Alt;
            if (binding.Win) modifiers |= Modifiers.Win;

            if (RegisterHotKey(_window.Handle, id, (uint)modifiers, (uint)binding.Key))
            {
                _registeredHotkeyIds.Add(id);
                _commandsById[id] = binding.Command;
            }
            else
            {
                failedCommands.Add(binding.Command);
            }
        }

        return failedCommands;
    }

    private void DispatchHotkey(int id)
    {
        if (!_commandsById.TryGetValue(id, out var command))
        {
            return;
        }

        CommandPressed?.Invoke(command);
        if (command == GlobalHotkeyCommand.Capture)
        {
            HotkeyPressed?.Invoke();
        }
    }

    public void Unregister()
    {
        _keyboardHookThread?.Dispose();
        _keyboardHookThread = null;

        foreach (var id in _registeredHotkeyIds)
        {
            UnregisterHotKey(_window.Handle, id);
        }

        _registeredHotkeyIds.Clear();
        _commandsById.Clear();
        _printScreenHotkeyId = null;
        _nextHotkeyId = 0;
    }

    public void Dispose()
    {
        Unregister();
        _window.Dispose();
    }

    private sealed class KeyboardHookThread : IDisposable
    {
        private readonly IntPtr _targetWindow;
        private readonly int _hotkeyId;
        private readonly LowLevelKeyboardProc _callback;
        private readonly Thread _thread;
        private readonly ManualResetEventSlim _started = new();
        private uint _threadId;
        private IntPtr _hook;
        private bool _isPrintScreenDown;

        public KeyboardHookThread(IntPtr targetWindow, int hotkeyId)
        {
            _targetWindow = targetWindow;
            _hotkeyId = hotkeyId;
            _callback = HandleLowLevelKeyboard;
            _thread = new Thread(Run)
            {
                IsBackground = true,
                Name = "Aprint.PrintScreenHook",
            };
        }

        public bool Start()
        {
            _thread.Start();
            _started.Wait();
            return _hook != IntPtr.Zero;
        }

        private void Run()
        {
            try
            {
                _threadId = GetCurrentThreadId();
                PeekMessage(out _, IntPtr.Zero, 0, 0, 0);
                _hook = SetWindowsHookEx(WhKeyboardLl, _callback, GetModuleHandle(null), 0);
                _started.Set();
                if (_hook == IntPtr.Zero)
                {
                    return;
                }

                while (GetMessage(out var message, IntPtr.Zero, 0, 0) > 0)
                {
                    TranslateMessage(in message);
                    DispatchMessage(in message);
                }
            }
            finally
            {
                if (_hook != IntPtr.Zero)
                {
                    UnhookWindowsHookEx(_hook);
                    _hook = IntPtr.Zero;
                }

                _started.Set();
            }
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
                        PostMessage(_targetWindow, WmHotkey, (IntPtr)_hotkeyId, IntPtr.Zero);
                    }

                    return (IntPtr)1;
                }

                if (message is WmKeyUp or WmSysKeyUp)
                {
                    _isPrintScreenDown = false;
                    return (IntPtr)1;
                }
            }

            return CallNextHookEx(_hook, nCode, wParam, lParam);
        }

        public void Dispose()
        {
            if (_thread.IsAlive)
            {
                if (!PostThreadMessage(_threadId, WmQuit, UIntPtr.Zero, IntPtr.Zero) && _thread.IsAlive)
                {
                    throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error());
                }

                _thread.Join();
            }

            _started.Dispose();
        }
    }

    private sealed class HotkeyWindow : NativeWindow, IDisposable
    {
        public event Action<int>? HotkeyPressed;

        public HotkeyWindow() => CreateHandle(new CreateParams());

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == WmHotkey)
            {
                HotkeyPressed?.Invoke(m.WParam.ToInt32());
            }

            base.WndProc(ref m);
        }

        public void Dispose() => DestroyHandle();
    }
}

public enum GlobalHotkeyCommand
{
    Capture,
    StartSession,
    EndSession,
}

public sealed record GlobalHotkeyBinding(
    GlobalHotkeyCommand Command,
    Keys Key,
    bool Ctrl,
    bool Shift,
    bool Alt,
    bool Win);
