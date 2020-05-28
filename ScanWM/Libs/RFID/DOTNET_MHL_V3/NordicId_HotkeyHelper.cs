using System;
using Microsoft.WindowsCE.Forms;
using System.Runtime.InteropServices;

namespace NordicId
{
    /// <summary>
    /// Register/Catch WM_HOTKEY messages.
    /// This class allows you to globally catch keypress events.
    /// </summary>
    /// <example>
    /// <code>
    /// class MyClass
    /// {
    ///     HotkeyHelper hotkeyHelper = new HotkeyHelper(); 
    /// 
    ///     public void HandleHotkey(int vk) 
    ///     {
    ///         if (vk == (int)VK.SCAN) 
    ///         {
    ///             MessageBox.Show("You pressed Scan button", "HOTKEY");            
    ///         }
    ///     }
    /// 
    ///     // Must be called at init time
    ///     public void InitHotKey() 
    ///     {
    ///         // Attach callback
    ///         hotkeyHelper.SetCallbackDelegate(new HotkeyHelper.HotkeyCallback(HandleHotkey));
    ///         // Register scan button keypress
    ///         hotkeyHelper.RegisterKey(VK.SCAN, KeyModifiers.None);
    ///     }
    /// 
    ///     // ...... Rest of the code ......
    /// }
    /// </code>
    /// </example>
    public class HotkeyHelper : MessageWindow
    {
        /// <summary> Delegate prototype for HotkeyHelper Callback </summary>
        /// <param name="vk">Virtual keycode</param>
        public delegate void HotkeyCallback(int vk);

        private const int WM_HOTKEY = 0x0312;
        private HotkeyCallback callbackDelegate = null;

        /// <summary>
        /// Window proc
        /// </summary>        
        protected override void WndProc(ref Message msg)
        {            
            switch(msg.Msg) 
            { 
                case WM_HOTKEY:
                    if (callbackDelegate != null)
                    {
                        callbackDelegate(((int)msg.LParam >> 16));
                    }
                    break;
            } 
            base.WndProc(ref msg);
        }

        /// <summary>
        /// std contructor.
        /// </summary>
        public HotkeyHelper()
        {

        }

        /// <summary>
        /// Connstructor with delegate function to receive hotkey callbacks
        /// </summary>
        /// <param name="callbackDelegate">delegate function</param>
        public HotkeyHelper(HotkeyCallback callbackDelegate)
        {
            this.callbackDelegate = callbackDelegate;
        }

        /// <summary>
        /// Set delegate function to receive hotkey callbacks.
        /// </summary>
        /// <param name="callbackDelegate">delegate function</param>
        public void SetCallbackDelegate(HotkeyCallback callbackDelegate)
        {
            this.callbackDelegate = callbackDelegate;
        }

        /// <summary> Register Hotkey with modifiers </summary>
        /// <param name="vk">Virtual keycode to register</param>
        /// <param name="mod">Keyboard modifier for virtual keycode</param>
        public bool RegisterKey(int vk, KeyModifiers mod) 
        {
            WIN32.UnregisterFunc1(mod, vk);
            return WIN32.RegisterHotKey(this.Hwnd, (int)(vk + 0x1000), mod, vk);
        }

        /// <summary> Register Hotkey with modifiers </summary>
        /// <param name="vk">Virtual keycode to register</param>
        /// <param name="mod">Keyboard modifier for virtual keycode</param>
        public bool RegisterKey(VK vk, KeyModifiers mod) 
        {
            return RegisterKey((int)vk, mod);
        }

        /// <summary> Unregister Hotkey with modifiers </summary>
        /// <param name="vk">Virtual keycode to register</param>
        /// <param name="mod">Keyboard modifier for virtual keycode</param>
        public bool UnregisterKey(int vk, KeyModifiers mod) 
        {
            return WIN32.UnregisterFunc1(mod, vk);
        }

        /// <summary> Unregister Hotkey with modifiers </summary>
        /// <param name="vk">Virtual keycode to register</param>
        /// <param name="mod">Keyboard modifier for virtual keycode</param>
        public bool UnregisterKey(VK vk, KeyModifiers mod) 
        {
            return UnregisterKey((int)vk, mod);
        }
    }
}

