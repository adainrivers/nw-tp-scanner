﻿using System.Runtime.InteropServices;

namespace TPS.Desktop;

/// <summary>
/// Event Args for the event that is fired after the hot key has been pressed.
/// </summary>
public class KeyPressedEventArgs : EventArgs
{
    internal KeyPressedEventArgs(ModifierKeys modifier, Keys key)
    {
        Modifier = modifier;
        Key = key;
    }

    public ModifierKeys Modifier { get; }

    public Keys Key { get; }
}

/// <summary>
/// The enumeration of possible modifiers.
/// </summary>
[Flags]
public enum ModifierKeys : uint
{
    None = 0,
    Alt = 1,
    Control = 2,
    Shift = 4,
    Win = 8
}

public sealed  class KeyboardHook : IDisposable
{
    // Registers a hot key with Windows.
    [DllImport("user32.dll")]
    private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);
    // Unregisters the hot key with Windows.
    [DllImport("user32.dll")]
    private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

    /// <summary>
    /// Represents the window that is used internally to get the messages.
    /// </summary>
    private sealed class Window : NativeWindow, IDisposable
    {
        private static readonly int WM_HOTKEY = 0x0312;

        public Window()
        {
            // create the handle for the window.
            CreateHandle(new CreateParams());
        }

        /// <summary>
        /// Overridden to get the notifications.
        /// </summary>
        /// <param name="m"></param>
        protected override void WndProc(ref Message m)
        {
            base.WndProc(ref m);

            // check if we got a hot key pressed.
            if (m.Msg != WM_HOTKEY) return;
            // get the keys.
            var key = (Keys)(((int)m.LParam >> 16) & 0xFFFF);
            var modifier = (ModifierKeys)((int)m.LParam & 0xFFFF);

            // invoke the event to notify the parent.
            KeyPressed?.Invoke(this, new KeyPressedEventArgs(modifier, key));
        }

        public event EventHandler<KeyPressedEventArgs> KeyPressed;

        #region IDisposable Members

        public void Dispose()
        {
            DestroyHandle();
        }

        #endregion
    }

    private readonly Window _window = new();
    private int _currentId;

    public KeyboardHook()
    {
        // register the event of the inner native window.
        _window.KeyPressed += delegate(object sender, KeyPressedEventArgs args)
        {
            KeyPressed?.Invoke(this, args);
        };
    }

    /// <summary>
    /// Registers a hot key in the system.
    /// </summary>
    /// <param name="modifier">The modifiers that are associated with the hot key.</param>
    /// <param name="key">The key itself that is associated with the hot key.</param>
    public void RegisterHotKey(ModifierKeys modifier, Keys key)
    {
        // increment the counter.
        _currentId += 1;

        // register the hot key.
        if (!RegisterHotKey(_window.Handle, _currentId, (uint)modifier, (uint)key))
            throw new InvalidOperationException("Couldn’t register the hot key.");
    }

    /// <summary>
    /// A hot key has been pressed.
    /// </summary>
    public event EventHandler<KeyPressedEventArgs> KeyPressed;

    #region IDisposable Members

    public void Dispose()
    {
        // unregister all the registered hot keys.
        for (var i = _currentId; i > 0; i--)
        {
            UnregisterHotKey(_window.Handle, i);
        }

        // dispose the inner native window.
        _window.Dispose();
    }

    #endregion
}