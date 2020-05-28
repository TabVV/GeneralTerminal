using System;
using System.Windows.Forms;
using System.Runtime.InteropServices;

namespace NordicId
{
    /// <summary>
    /// PROVIDED ONLY FOR BACKWARD COMPATIBILITY. Please use new MHLDriver class.
    /// </summary>
    /// <remarks>PROVIDED ONLY FOR BACKWARD COMPATIBILITY. Please use new MHLDriver class.</remarks>
    public class MHL
    {
        /// <summary>PROVIDED ONLY FOR BACKWARD COMPATIBILITY. Please use new HotkeyHelper class.</summary>
        /// <remarks>PROVIDED ONLY FOR BACKWARD COMPATIBILITY. Please use new MHLDriver class.</remarks>
        public int OpenDrv(String name)
        {
            return MHLSRV._MHL_OpenDrv(name);
        }

        /// <summary>PROVIDED ONLY FOR BACKWARD COMPATIBILITY. Please use new HotkeyHelper class.</summary>
        /// <remarks>PROVIDED ONLY FOR BACKWARD COMPATIBILITY. Please use new MHLDriver class.</remarks>
        public int CloseDrv(int handle)
        {
            return MHLSRV._MHL_CloseDrv(handle);
        }

        /// <summary>PROVIDED ONLY FOR BACKWARD COMPATIBILITY. Please use new HotkeyHelper class.</summary>
        /// <remarks>PROVIDED ONLY FOR BACKWARD COMPATIBILITY. Please use new MHLDriver class.</remarks>
        public int GetLastError()
        {
            return Marshal.GetLastWin32Error();
        }

        /// <summary>PROVIDED ONLY FOR BACKWARD COMPATIBILITY. Please use new HotkeyHelper class.</summary>
        /// <remarks>PROVIDED ONLY FOR BACKWARD COMPATIBILITY. Please use new MHLDriver class.</remarks>
        public int LoadProfile(int handle, String name) 
        {
            return MHLSRV._MHL_LoadProfile(handle, name);
        }

        /// <summary>PROVIDED ONLY FOR BACKWARD COMPATIBILITY. Please use new HotkeyHelper class.</summary>
        /// <remarks>PROVIDED ONLY FOR BACKWARD COMPATIBILITY. Please use new MHLDriver class.</remarks>
        public int SaveProfile(int handle, String name) 
        {
            return MHLSRV._MHL_SaveProfile(handle, name);
        }

        ///////////////////////////////////
        // Set Functions

        /// <summary>PROVIDED ONLY FOR BACKWARD COMPATIBILITY. Please use new HotkeyHelper class.</summary>
        /// <remarks>PROVIDED ONLY FOR BACKWARD COMPATIBILITY. Please use new MHLDriver class.</remarks>
        public int SetDword(int handle, String name, uint val)
        {
            return MHLSRV._MHL_SetDword(handle, name, val);
        }

        /// <summary>PROVIDED ONLY FOR BACKWARD COMPATIBILITY. Please use new HotkeyHelper class.</summary>
        /// <remarks>PROVIDED ONLY FOR BACKWARD COMPATIBILITY. Please use new MHLDriver class.</remarks>
        public int SetInt(int handle, String name, int val)
        {
            return MHLSRV._MHL_SetInt(handle, name, val);
        }

        /// <summary>PROVIDED ONLY FOR BACKWARD COMPATIBILITY. Please use new HotkeyHelper class.</summary>
        /// <remarks>PROVIDED ONLY FOR BACKWARD COMPATIBILITY. Please use new MHLDriver class.</remarks>
        public int SetBool(int handle, String name, bool val)
        {
            return MHLSRV._MHL_SetBool(handle, name, (val == true) ? 1 : 0);
        }

        /// <summary>PROVIDED ONLY FOR BACKWARD COMPATIBILITY. Please use new HotkeyHelper class.</summary>
        /// <remarks>PROVIDED ONLY FOR BACKWARD COMPATIBILITY. Please use new MHLDriver class.</remarks>
        public int SetString(int handle, String name, String val)
        {
            return MHLSRV._MHL_SetString(handle, name, val);
        }

        /// <summary>PROVIDED ONLY FOR BACKWARD COMPATIBILITY. Please use new HotkeyHelper class.</summary>
        /// <remarks>PROVIDED ONLY FOR BACKWARD COMPATIBILITY. Please use new MHLDriver class.</remarks>
        public int SetBin(int handle, String name, byte[] val)
        {
            return MHLSRV._MHL_SetBin(handle, name, val, val.Length);
        }

        /// <summary>PROVIDED ONLY FOR BACKWARD COMPATIBILITY. Please use new HotkeyHelper class.</summary>
        /// <remarks>PROVIDED ONLY FOR BACKWARD COMPATIBILITY. Please use new MHLDriver class.</remarks>
        public int Execute(int handle, String name)
        {
            return MHLSRV._MHL_Execute(handle, name);
        }

        ///////////////////////////////////
        // Get Functions

        /// <summary>PROVIDED ONLY FOR BACKWARD COMPATIBILITY. Please use new HotkeyHelper class.</summary>
        /// <remarks>PROVIDED ONLY FOR BACKWARD COMPATIBILITY. Please use new MHLDriver class.</remarks>
        public uint GetDword(int handle, String name)
        {
            return MHLSRV._MHL_GetDword(handle, name);
        }

        /// <summary>PROVIDED ONLY FOR BACKWARD COMPATIBILITY. Please use new HotkeyHelper class.</summary>
        /// <remarks>PROVIDED ONLY FOR BACKWARD COMPATIBILITY. Please use new MHLDriver class.</remarks>
        public int GetInt(int handle, String name)
        {
            return MHLSRV._MHL_GetInt(handle, name);
        }

        /// <summary>PROVIDED ONLY FOR BACKWARD COMPATIBILITY. Please use new HotkeyHelper class.</summary>
        /// <remarks>PROVIDED ONLY FOR BACKWARD COMPATIBILITY. Please use new MHLDriver class.</remarks>
        public bool GetBool(int handle, String name)
        {
            return (MHLSRV._MHL_GetBool(handle, name) == 1) ? true : false;
        }

        /// <summary>PROVIDED ONLY FOR BACKWARD COMPATIBILITY. Please use new HotkeyHelper class.</summary>
        /// <remarks>PROVIDED ONLY FOR BACKWARD COMPATIBILITY. Please use new MHLDriver class.</remarks>
        public String GetString(int handle, String name)
        {
            int length = MHLSRV._MHL_GetStringLen(handle, name) + 1;

            if (length == 0)
                return "";

            String str = new String(' ', length);

            MHLSRV._MHL_GetString(handle, name, str, length);

			// Remove NULL character, if present
			if (str[str.Length - 1] == '\0')
			{
				return str.Remove(str.Length - 1, 1);
			}
			return str;
        }

        private static System.Array ResizeArray(System.Array oldArray, int newSize)
        {
            int oldSize = oldArray.Length;
            System.Type elementType = oldArray.GetType().GetElementType();
            System.Array newArray = System.Array.CreateInstance(elementType, newSize);
            int preserveLength = System.Math.Min(oldSize, newSize);
            if (preserveLength > 0)
                System.Array.Copy(oldArray, newArray, preserveLength);
            return newArray;
        }

        /// <summary>PROVIDED ONLY FOR BACKWARD COMPATIBILITY. Please use new HotkeyHelper class.</summary>
        /// <remarks>PROVIDED ONLY FOR BACKWARD COMPATIBILITY. Please use new MHLDriver class.</remarks>
        public byte[] GetBin(int handle, String name)
        {
            int retLen;
            uint len = MHLSRV._MHL_GetBinLength(handle, name);
            byte[] buf = new byte[len];

            retLen = (int)MHLSRV._MHL_GetBin(handle, name, buf, (int)len);
            buf = (byte[])ResizeArray(buf, retLen);

            return buf;
        }

        /// <summary>PROVIDED ONLY FOR BACKWARD COMPATIBILITY. Please use new HotkeyHelper class.</summary>
        /// <remarks>PROVIDED ONLY FOR BACKWARD COMPATIBILITY. Please use new MHLDriver class.</remarks>
        public byte[] GetBin(int handle, String name, int length)
        {
            int retLen;
            byte[] buf = new byte[length];

            retLen = (int)MHLSRV._MHL_GetBin(handle, name, buf, (int)length);
            buf = (byte[])ResizeArray(buf, retLen);

            return buf;
        }
    }
}
