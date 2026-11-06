using System.Reflection;
using System.Runtime.InteropServices;
using Aprint.Infrastructure.Windows;

namespace Aprint.Tests.Capture;

public sealed class GlobalHotkeyManagerTests
{
    [Fact]
    public void PrintScreenHookRunsOnDedicatedThreadAndStopsOnUnregister()
    {
        using var manager = new GlobalHotkeyManager();
        Assert.True(manager.Register(Keys.PrintScreen, false, false, false, false));
        var hook = GetHook(manager);
        var thread = GetThread(hook);

        Assert.True(thread.IsAlive);
        Assert.NotEqual(Environment.CurrentManagedThreadId, thread.ManagedThreadId);

        manager.Unregister();

        Assert.False(thread.IsAlive);
        Assert.Null(typeof(GlobalHotkeyManager)
            .GetField("_keyboardHookThread", BindingFlags.Instance | BindingFlags.NonPublic)!
            .GetValue(manager));
    }

    [Fact]
    public void RegisteringAgainStopsPreviousHookThread()
    {
        using var manager = new GlobalHotkeyManager();
        Assert.True(manager.Register(Keys.PrintScreen, false, false, false, false));
        var previousThread = GetThread(GetHook(manager));

        Assert.True(manager.Register(Keys.PrintScreen, false, false, false, false));
        var currentThread = GetThread(GetHook(manager));

        Assert.False(previousThread.IsAlive);
        Assert.True(currentThread.IsAlive);
        Assert.NotSame(previousThread, currentThread);
    }

    [Fact]
    public void HookPostsToUiAndSuppressesRepeatedKeyDown()
    {
        using var manager = new GlobalHotkeyManager();
        Assert.True(manager.Register(Keys.PrintScreen, false, false, false, false));
        var hook = GetHook(manager);
        var callback = hook.GetType().GetMethod("HandleLowLevelKeyboard", BindingFlags.Instance | BindingFlags.NonPublic)!;
        var uiThreadId = Environment.CurrentManagedThreadId;
        var notifications = 0;
        manager.HotkeyPressed += () =>
        {
            Assert.Equal(uiThreadId, Environment.CurrentManagedThreadId);
            notifications++;
        };

        var keyData = Marshal.AllocHGlobal(sizeof(int));
        try
        {
            Marshal.WriteInt32(keyData, (int)Keys.PrintScreen);
            Exception? callbackError = null;
            var callbackThread = new Thread(() =>
            {
                try
                {
                    Assert.Equal((IntPtr)1, callback.Invoke(hook, [0, (IntPtr)0x0100, keyData]));
                    Assert.Equal((IntPtr)1, callback.Invoke(hook, [0, (IntPtr)0x0100, keyData]));
                    Assert.Equal((IntPtr)1, callback.Invoke(hook, [0, (IntPtr)0x0101, keyData]));
                    Assert.Equal((IntPtr)1, callback.Invoke(hook, [0, (IntPtr)0x0100, keyData]));
                    Assert.Equal((IntPtr)1, callback.Invoke(hook, [0, (IntPtr)0x0101, keyData]));
                }
                catch (Exception exception)
                {
                    callbackError = exception;
                }
            });
            callbackThread.Start();
            callbackThread.Join();

            Assert.Null(callbackError);
            Assert.Equal(0, notifications);
            System.Windows.Forms.Application.DoEvents();
            Assert.Equal(2, notifications);
        }
        finally
        {
            Marshal.FreeHGlobal(keyData);
        }
    }

    private static object GetHook(GlobalHotkeyManager manager) => typeof(GlobalHotkeyManager)
        .GetField("_keyboardHookThread", BindingFlags.Instance | BindingFlags.NonPublic)!
        .GetValue(manager)!;

    private static Thread GetThread(object hook) => (Thread)hook.GetType()
        .GetField("_thread", BindingFlags.Instance | BindingFlags.NonPublic)!
        .GetValue(hook)!;
}