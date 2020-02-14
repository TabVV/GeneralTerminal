using System;
using Microsoft.WindowsCE.Forms;
using System.Runtime.InteropServices;

namespace NordicId
{
    /// <summary> HotkeyWindow Callback function type </summary>
    /// <remarks>PROVIDED ONLY FOR BACKWARD COMPATIBILITY. Please use new HotkeyHelper class.</remarks>
    public delegate void HotkeyCallbackFunc(int vk);
    
    /// <summary>
    /// PROVIDED ONLY FOR BACKWARD COMPATIBILITY. Please use new HotkeyHelper class.
    /// </summary>
    /// <remarks>PROVIDED ONLY FOR BACKWARD COMPATIBILITY. Please use new HotkeyHelper class.</remarks>
    public class HotkeyWindow : MessageWindow
    {
        /// <summary> PROVIDED ONLY FOR BACKWARD COMPATIBILITY. Please use new HotkeyHelper class. </summary>
        /// <remarks>PROVIDED ONLY FOR BACKWARD COMPATIBILITY. Please use new HotkeyHelper class.</remarks>
        public const int WM_HOTKEY = 0x0312;

        /// <summary> PROVIDED ONLY FOR BACKWARD COMPATIBILITY. Please use new HotkeyHelper class. </summary>
        /// <remarks>PROVIDED ONLY FOR BACKWARD COMPATIBILITY. Please use new HotkeyHelper class.</remarks>
        public HotkeyCallbackFunc callback;

        /// <summary> PROVIDED ONLY FOR BACKWARD COMPATIBILITY. Please use new HotkeyHelper class. </summary>        
        /// <remarks>PROVIDED ONLY FOR BACKWARD COMPATIBILITY. Please use new HotkeyHelper class.</remarks>
        protected override void WndProc(ref Message msg)
        {            
            switch(msg.Msg) 
            { 
                case WM_HOTKEY:
                    callback(((int)msg.LParam>>16));                    
                    break;
            } 
            base.WndProc(ref msg);
        }

        /// <summary> PROVIDED ONLY FOR BACKWARD COMPATIBILITY. Please use new HotkeyHelper class. </summary>
        /// <remarks>PROVIDED ONLY FOR BACKWARD COMPATIBILITY. Please use new HotkeyHelper class.</remarks>
        public bool RegisterKey(int vk, KeyModifiers mod) 
        {
            WIN32.UnregisterFunc1(mod, vk);
            return WIN32.RegisterHotKey(this.Hwnd, (int)(vk + 0x1000), mod, vk);
        }

        /// <summary> PROVIDED ONLY FOR BACKWARD COMPATIBILITY. Please use new HotkeyHelper class. </summary>
        /// <remarks>PROVIDED ONLY FOR BACKWARD COMPATIBILITY. Please use new HotkeyHelper class.</remarks>
        public bool RegisterKey(VK vk, KeyModifiers mod) 
        {
            return RegisterKey((int)vk, mod);
        }

        /// <summary> PROVIDED ONLY FOR BACKWARD COMPATIBILITY. Please use new HotkeyHelper class. </summary>
        /// <remarks>PROVIDED ONLY FOR BACKWARD COMPATIBILITY. Please use new HotkeyHelper class.</remarks>
        public bool UnregisterKey(int vk, KeyModifiers mod) 
        {
            return WIN32.UnregisterFunc1(mod, vk);
        }

        /// <summary> PROVIDED ONLY FOR BACKWARD COMPATIBILITY. Please use new HotkeyHelper class. </summary>
        /// <remarks>PROVIDED ONLY FOR BACKWARD COMPATIBILITY. Please use new HotkeyHelper class.</remarks>
        public bool UnregisterKey(VK vk, KeyModifiers mod) 
        {
            return UnregisterKey((int)vk, mod);
        }
    }
}

