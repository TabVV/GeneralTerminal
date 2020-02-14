using System;
using System.Windows.Forms;

namespace NordicId
{
    /// <summary>
    /// Encapsulates MHL service calls for single driver instance.
    /// </summary>
    /// <example>
    /// <code>
    /// class MyClass
    /// {
    ///     MHLDriver hScan = new MHLDriver();
    ///     MHLDriver hKeyb = new MHLDriver();
    /// 
    ///     // Must be called at init time
    ///     public void InitMHL() 
    ///     {
    ///         try
    ///         {
    ///             hScan.Open("Scanner");
    ///         }
    ///         catch (Exception ex)
    ///         {
    ///             MessageBox.Show(ex.ToString(), "Scanner open failure");
    ///             // Handle Error here ..
    ///         }
    /// 
    ///         try
    ///         {
    ///             hKeyb.Open("Keyboard");
    ///         }
    ///         catch (Exception ex)
    ///         {
    ///             MessageBox.Show(ex.ToString(), "Keyboard open failure");
    ///             // Handle Error here ..
    ///         }
    ///     }
    /// 
    ///     // ...... Rest of the code ......
    /// }
    /// </code>
    /// </example>
    public class MHLDriver
    {
        private string loadedDrvName = "";
        private int loadedDrvHandle = -1;

        /// <summary>
        /// If true, this MHL Driver object will throw exception on error
        /// </summary>
        /// <remarks>Defaults to true.</remarks>
        public bool throwsException = true;

        /// <summary>
        /// std constructor.
        /// </summary>
        public MHLDriver()
        {

        }

        /// <summary>
        /// Open driver on constructor.
        /// </summary>
        /// <param name="drvName">Name of the MHL driver to open.</param>
        public MHLDriver(string drvName)
        {
            Open(drvName);
        }

        /// <summary>
        /// Open driver on constructor.
        /// </summary>
        /// <param name="drvName">Name of the MHL driver to open.</param>
        /// <param name="throwsException">Set true to make this MHL Driver object to throw exceptions on error.</param>
        public MHLDriver(string drvName, bool throwsException)
        {
            this.throwsException = throwsException;
            Open(drvName);
        }

        /// <summary>
        /// std destructor.
        /// </summary>
        ~MHLDriver()
        {
            Close();
        }

        /// <summary>
        /// Get open state.
        /// </summary>
        /// <returns>Returns true if driver is opened successfully.</returns>
        public bool IsOpen()
        {
            return (loadedDrvHandle != WIN32.INVALID_HANDLE_VALUE);
        }

        /// <summary>
        /// Get currently open driver name.
        /// </summary>
        /// <returns>Returns name of the currently open driver. If driver is not open, empty string is returned.</returns>
        public string GetName()
        {
            return loadedDrvName;
        }

        /// <summary>
        /// Get low-level driver handle to open driver. 
        /// </summary>
        /// <returns>Returns low-level handle to opened driver. If driver is not open, -1 is returned.</returns>
        public int GetHandle()
        {
            return loadedDrvHandle;
        }

        /// <summary>
        /// This function opens an MHL feature driver for use. 
        /// Call this function to select which MHL driver you wish the object instance to use, if you have not already done so in the constructor. 
        /// Once a MHL feature driver has been opened you can call the Get/Set functions to communicate / command the driver. 
        /// See the specific drivers documentation for information about available functionality. 
        /// 
        /// If this instance of object have already open MHL driver, it will be internally closed first.
        /// </summary>
        /// <param name="name">Name of the MHL driver to open.</param>
        /// <exception cref="System.ArgumentException">If exceptions are enabled, on failure exception is thrown with valid error message.</exception>
        /// <returns>true on success, false on failure.</returns>
        /// <remarks>On failure, use GetLastError() ot GetLastErrorMessage() for more information.</remarks>
        public bool Open(string name)
        {
            if (this.IsOpen())
            {
                this.Close();
            }
            loadedDrvHandle = MHLSRV._MHL_OpenDrv(name);

            if (throwsException && loadedDrvHandle == WIN32.INVALID_HANDLE_VALUE)
            {
                throw new System.ArgumentException("Cannot open driver " + name, "Error " + this.GetLastError().ToString() + "; " + this.GetLastErrorMessage());
            }

            if (this.IsOpen())
            {
                loadedDrvName = name;
                return true;
            }
            return false;
        }

        /// <summary>
        /// When you are done with a feature and driver you must call this function to free up resources. 
        /// If your application is likely to be run exclusively on the unit, it is recommendable that you open and close an MHL feature handle only once during the lifetime of the application. 
        /// Do not make code that opens and closes the feature rapidly and very often, it is extremely inefficient. 
        /// </summary>
        /// <exception cref="System.ArgumentException">If exceptions are enabled, on failure exception is thrown with valid error message.</exception>
        /// <returns>true on success, false on failure.</returns>
        /// <remarks>On failure, use GetLastError() ot GetLastErrorMessage() for more information.</remarks>
        public bool Close()
        {
            if (IsOpen())
            {
                if (MHLSRV._MHL_CloseDrv(loadedDrvHandle) == 0)
                {
                    if (throwsException) throw new System.ArgumentException("Cannot close driver " + loadedDrvName, "Error " + this.GetLastError().ToString() + "; " + this.GetLastErrorMessage());
                }
                loadedDrvHandle = -1;
                loadedDrvName = "";
                return true;
            }
            return false;
        }

        /// <summary>
        /// Get last system error code.
        /// </summary>
        /// <returns>Returns last error code occured.</returns>
        public int GetLastError()
        {
            return WIN32.GetLastError();
        }

        /// <summary>
        /// Get last system error message.
        /// </summary>
        /// <returns>Returns last error message occured.</returns>
        public string GetLastErrorMessage()
        {
            return WIN32.GetLastErrorMessage();
        }

        /// <summary>
        /// This function is used to load a previously stored configuration of the loaded MHL driver. 
        /// The idea behind this is that you can store the existing setting when your application starts and then radically alter the settings of the MHL driver to suit your needs. 
        /// This may include capturing the Scan button so that it sends a virtual key code your program can intercept instead of automatically triggering the scanner. 
        /// Another typical use is to set up the barcode scanners parameters as you need them to be. 
        /// Then when your program exits you can load the initial profile and ensure that the device works as a factory default unit when your application is no longer running. 
        /// You can also use this function to restore factory default settings unless you have previously overwritten the SystemDefault profile. 
        /// </summary>
        /// <param name="name">The name of the profile you wish to load. The name must have been used earlier with SaveProfile to store a profile or it must be the SystemDefault profile.</param>
        /// <exception cref="System.ArgumentException">If exceptions are enabled, on failure exception is thrown with valid error message.</exception>
        /// <returns>true on success, false on failure.</returns>
        /// <remarks>On failure, use GetLastError() ot GetLastErrorMessage() for more information.</remarks>
        public bool LoadProfile(string name) 
        {
            if (MHLSRV._MHL_LoadProfile(loadedDrvHandle, name) == 0)
            {
                if (throwsException) throw new System.ArgumentException("Cannot load profile " + name, "Error " + this.GetLastError().ToString() + "; " + this.GetLastErrorMessage());
                return false;
            }
            return true;
        }

        /// <summary>
        /// This function is used to store the current configuration of the loaded MHL driver to named profile. 
        /// The idea behind this is that you can store the existing configuration when your application starts and then radically alter the settings of the MHL driver to suit your needs. 
        /// This may include capturing the Scan button so that it sends a virtual key code your program can intercept instead of automatically triggering the scanner. 
        /// Another typical use is to set up the barcode scanners parameters as you need them to be. 
        /// Then when your program exits you can load the initial profile and ensure that the device works as a factory default unit when your application is no longer running.
        /// </summary>
        /// <param name="name">The name of the profile you wish to store. Use this same name with LoadProfile when you wish to load the previously stored profile. Use the name SystemDefault to override the systems default configuration for this MHL driver.</param>
        /// <exception cref="System.ArgumentException">If exceptions are enabled, on failure exception is thrown with valid error message.</exception>
        /// <returns>true on success, false on failure.</returns>
        /// <remarks>On failure, use GetLastError() ot GetLastErrorMessage() for more information.</remarks>
        public bool SaveProfile(string name) 
        {
            if (MHLSRV._MHL_SaveProfile(loadedDrvHandle, name) == 0)
            {
                if (throwsException) throw new System.ArgumentException("Cannot save profile " + name, "Error " + this.GetLastError().ToString() + "; " + this.GetLastErrorMessage());
                return false;
            }
            return true;
        }

        ///////////////////////////////////
        // Set Functions

        /// <summary>
        /// This function is used to write a DWORD value to MHL features that are of DWORD type. 
        /// The DWORD data type is defined as a 32 bit unsigned integer in MHL.
        /// </summary>
        /// <param name="name">Name of the feature to set</param>
        /// <param name="val">DWORD value to set</param>
        /// <exception cref="System.ArgumentException">If exceptions are enabled, on failure exception is thrown with valid error message.</exception>
        /// <returns>true on success, false on failure.</returns>
        /// <remarks>On failure, use GetLastError() ot GetLastErrorMessage() for more information.</remarks>
        public bool SetDword(string name, uint val)
        {
            if (MHLSRV._MHL_SetDword(loadedDrvHandle, name, val) == 0)
            {
                if (throwsException) throw new System.ArgumentException("Cannot set DWORD " + name, "Error " + this.GetLastError().ToString() + "; " + this.GetLastErrorMessage());
                return false;
            }
            return true;
        }

        /// <summary>
        /// This function is used to write a DWORD value to MHL features that are of DWORD type. 
        /// The DWORD data type is defined as a 32 bit unsigned integer in MHL.
        /// </summary>
        /// <param name="name">Name of the feature to set</param>
        /// <param name="val">INT value to set as DWORD</param>
        /// <exception cref="System.ArgumentException">If exceptions are enabled, on failure exception is thrown with valid error message.</exception>
        /// <returns>true on success, false on failure.</returns>
        /// <remarks>On failure, use GetLastError() ot GetLastErrorMessage() for more information.</remarks>
        public bool SetDword(string name, int val)
        {
            return this.SetDword(name, (uint)val);
        }

        /// <summary>
        /// This function is used to write a double value to MHL features that are of double type. 
        /// The double data type is defined as a 32 bit unsigned integer in MHL.
        /// </summary>
        /// <param name="name">Name of the feature to set</param>
        /// <param name="val">double value to set</param>
        /// <exception cref="System.ArgumentException">If exceptions are enabled, on failure exception is thrown with valid error message.</exception>
        /// <returns>true on success, false on failure.</returns>
        /// <remarks>On failure, use GetLastError() ot GetLastErrorMessage() for more information.</remarks>
        public bool SetDouble(string name, double val)
        {
            if (MHLSRV._MHL_SetDouble(loadedDrvHandle, name, ref val) == 0)
            {
                if (throwsException) throw new System.ArgumentException("Cannot set double " + name, "Error " + this.GetLastError().ToString() + "; " + this.GetLastErrorMessage());
                return false;
            }
            return true;
        }

        /// <summary>
        /// This function is used to write a integer value to MHL features that are of integer type. 
        /// The INT data type is defined as a 32 bit signed integer in MHL.
        /// </summary>
        /// <param name="name">Name of the feature to set</param>
        /// <param name="val">Int value to set</param>
        /// <exception cref="System.ArgumentException">If exceptions are enabled, on failure exception is thrown with valid error message.</exception>
        /// <returns>true on success, false on failure.</returns>
        /// <remarks>On failure, use GetLastError() ot GetLastErrorMessage() for more information.</remarks>
        public bool SetInt(string name, int val)
        {
            if (MHLSRV._MHL_SetInt(loadedDrvHandle, name, val) == 0)
            {
                if (throwsException) throw new System.ArgumentException("Cannot set INT " + name, "Error " + this.GetLastError().ToString() + "; " + this.GetLastErrorMessage());
                return false;
            }
            return true;
        }

        /// <summary>
        /// This function is used to write a boolean value to MHL features that are of boolean type. 
        /// </summary>
        /// <param name="name">Name of the feature to set</param>
        /// <param name="val">bool value to set</param>
        /// <exception cref="System.ArgumentException">If exceptions are enabled, on failure exception is thrown with valid error message.</exception>
        /// <returns>true on success, false on failure.</returns>
        /// <remarks>On failure, use GetLastError() ot GetLastErrorMessage() for more information.</remarks>
        public bool SetBool(string name, bool val)
        {
            if (MHLSRV._MHL_SetBool(loadedDrvHandle, name, val ? 1 : 0) == 0)
            {
                if (throwsException) throw new System.ArgumentException("Cannot set BOOL " + name, "Error " + this.GetLastError().ToString() + "; " + this.GetLastErrorMessage());
                return false;
            }
            return true;
        }

        /// <summary>
        /// This function is used to write a string to MHL features that are of string type.
        /// </summary>
        /// <param name="name">Name of the feature to set</param>
        /// <param name="val">String value to set</param>
        /// <exception cref="System.ArgumentException">If exceptions are enabled, on failure exception is thrown with valid error message.</exception>
        /// <returns>true on success, false on failure.</returns>
        /// <remarks>On failure, use GetLastError() ot GetLastErrorMessage() for more information.</remarks>
        public bool SetString(string name, string val)
        {
            if (MHLSRV._MHL_SetString(loadedDrvHandle, name, val) == 0)
            {
                if (throwsException) throw new System.ArgumentException("Cannot set STRING " + name, "Error " + this.GetLastError().ToString() + "; " + this.GetLastErrorMessage());
                return false;
            }
            return true;
        }

        /// <summary>
        /// This function is used to write binary data to MHL features that are of binary type.
        /// </summary>
        /// <param name="name">Name of the feature to set</param>
        /// <param name="val">Byte array to write</param>
        /// <exception cref="System.ArgumentException">If exceptions are enabled, on failure exception is thrown with valid error message.</exception>
        /// <returns>true on success, false on failure.</returns>
        /// <remarks>On failure, use GetLastError() ot GetLastErrorMessage() for more information.</remarks>
        public bool SetBin(string name, byte[] val)
        {
            if (MHLSRV._MHL_SetBin(loadedDrvHandle, name, val, val.Length) == 0)
            {
                if (throwsException) throw new System.ArgumentException("Cannot set BIN " + name, "Error " + this.GetLastError().ToString() + "; " + this.GetLastErrorMessage());
                return false;
            }
            return true;
        }

        /// <summary>
        /// This function is used for MHL feature functions that are of the Executable type. 
        /// They typically trigger some kind of process or function in hardware. 
        /// Internally this call is the same as calling the same function with SetBool with a true parameter.
        /// </summary>
        /// <param name="name">Name of the feature to set</param>
        /// <exception cref="System.ArgumentException">If exceptions are enabled, on failure exception is thrown with valid error message.</exception>
        /// <returns>true on success, false on failure.</returns>
        /// <remarks>On failure, use GetLastError() ot GetLastErrorMessage() for more information.</remarks>
        public bool Execute(string name)
        {
            if (MHLSRV._MHL_Execute(loadedDrvHandle, name) == 0)
            {
                if (throwsException) throw new System.ArgumentException("Cannot EXECUTE " + name, "Error " + this.GetLastError().ToString() + "; " + this.GetLastErrorMessage());
                return false;
            }
            return true;
        }

        ///////////////////////////////////
        // Get Functions
        
        /// <summary>
        /// This function is used to read the value of MHL feature functions of DWORD type. MHL Dwords are 32 bit unsigned values.
        /// </summary>
        /// <param name="name">Name of the requested feature</param>
        /// <exception cref="System.ArgumentException">If exceptions are enabled, on failure exception is thrown with valid error message.</exception>
        /// <returns>DWORD value for requested feature. -1 on failure, if exceptions are not enabled.</returns>
        /// <remarks>On failure, use GetLastError() ot GetLastErrorMessage() for more information.</remarks>
        public uint GetDword(string name)
        {
            uint ret = MHLSRV._MHL_GetDword(loadedDrvHandle, name);
            if (throwsException && this.GetLastError() != 0)
            {
                throw new System.ArgumentException("Get DWORD Failed " + name, "Error " + this.GetLastError().ToString() + "; " + this.GetLastErrorMessage());
            }
            return ret;
        }

        /// <summary>
        /// This function is used to read the value of MHL feature functions of double type. MHL doubles are 64 bit floating point values.
        /// </summary>
        /// <param name="name">Name of the requested feature</param>
        /// <exception cref="System.ArgumentException">If exceptions are enabled, on failure exception is thrown with valid error message.</exception>
        /// <returns>double value for requested feature. NaN constant on failure (0xFFFFFFFF in hexadecimal), if exceptions are not enabled.</returns>
        /// <remarks>On failure, use GetLastError() ot GetLastErrorMessage() for more information.</remarks>
        public double GetDouble(string name)
        {
            double ret = 0;
            
            MHLSRV._MHL_GetDouble(loadedDrvHandle, name, ref ret);

            if (throwsException && this.GetLastError() != 0)
            {
                throw new System.ArgumentException("Get double Failed " + name, "Error " + this.GetLastError().ToString() + "; " + this.GetLastErrorMessage());
            }
            return ret;
        }

        /// <summary>
        /// This function is used to read the value of MHL feature functions of INT type. MHL Integers are 32 bit signed values.
        /// </summary>
        /// <param name="name">Name of the requested feature</param>
        /// <exception cref="System.ArgumentException">If exceptions are enabled, on failure exception is thrown with valid error message.</exception>
        /// <returns>INT value for requested feature. -1 on failure, if exceptions are not enabled.</returns>
        /// <remarks>On failure, use GetLastError() ot GetLastErrorMessage() for more information.</remarks>
        public int GetInt(string name)
        {
            int ret = MHLSRV._MHL_GetInt(loadedDrvHandle, name);
            if (throwsException && this.GetLastError() != 0)
            {
                throw new System.ArgumentException("Get INT Failed " + name, "Error " + this.GetLastError().ToString() + "; " + this.GetLastErrorMessage());
            }
            return ret;
        }

        /// <summary>
        /// This function is used to read the value of MHL feature functions of BOOL type.
        /// </summary>
        /// <param name="name">Name of the requested feature</param>
        /// <exception cref="System.ArgumentException">If exceptions are enabled, on failure exception is thrown with valid error message.</exception>
        /// <returns>BOOL value for requested feature. Returns false on failure, use GetLastError() to verify errors if exceptions are not enabled.</returns>
        /// <remarks>On failure, use GetLastError() ot GetLastErrorMessage() for more information.</remarks>
        public bool GetBool(string name)
        {
            bool ret = MHLSRV._MHL_GetBool(loadedDrvHandle, name) == 1;
            if (throwsException && this.GetLastError() != 0)
            {
                throw new System.ArgumentException("Get BOOL Failed " + name, "Error " + this.GetLastError().ToString() + "; " + this.GetLastErrorMessage());
            }
            return ret;
        }

        /// <summary>
        /// This function is used to read the value of MHL feature functions of String type.
        /// </summary>
        /// <param name="name">Name of the requested feature</param>
        /// <exception cref="System.ArgumentException">If exceptions are enabled, on failure exception is thrown with valid error message.</exception>
        /// <returns>String value for requested feature. Empty string on failure, if exceptions are not enabled.</returns>
        /// <remarks>On failure, use GetLastError() ot GetLastErrorMessage() for more information.</remarks>
        public string GetString(string name)
        {
            int length = MHLSRV._MHL_GetStringLen(loadedDrvHandle, name) + 1;
            if (throwsException && this.GetLastError() != 0)
            {
                throw new System.ArgumentException("Get STRING Failed " + name, "Error " + this.GetLastError().ToString() + "; " + this.GetLastErrorMessage());
            }
            if (length == 0)
                return "";

            string str = new string(' ', length);

            MHLSRV._MHL_GetString(loadedDrvHandle, name, str, length);
            if (throwsException && this.GetLastError() != 0)
            {
                throw new System.ArgumentException("Get STRING Failed " + name, "Error " + this.GetLastError().ToString() + "; " + this.GetLastErrorMessage());
            }

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

        /// <summary>
        /// This function is used to read the value of MHL feature functions of BINARY type.
        /// </summary>
        /// <param name="name">Name of the requested feature</param>
        /// <exception cref="System.ArgumentException">If exceptions are enabled, on failure exception is thrown with valid error message.</exception>
        /// <returns>Binary array value for requested feature. Null on failure, if exceptions are not enabled.</returns>
        /// <remarks>On failure, use GetLastError() ot GetLastErrorMessage() for more information.</remarks>
        public byte[] GetBin(string name)
        {
            int retLen;
            uint len = MHLSRV._MHL_GetBinLength(loadedDrvHandle, name);
            if (this.GetLastError() != 0)
            {
                if (throwsException)
                    throw new System.ArgumentException("Get BIN Failed " + name, "Error " + this.GetLastError().ToString() + "; " + this.GetLastErrorMessage());
                return null;
            }

            byte[] buf = new byte[len];

            retLen = (int)MHLSRV._MHL_GetBin(loadedDrvHandle, name, buf, (int)len);
            if (this.GetLastError() != 0)
            {
                if (throwsException)
                    throw new System.ArgumentException("Get BIN Failed " + name, "Error " + this.GetLastError().ToString() + "; " + this.GetLastErrorMessage());
                return null;
            }

            buf = (byte[])ResizeArray(buf, retLen);

            return buf;
        }

        /// <summary>
        /// This function is used to read the value of MHL feature functions of BINARY type with specific length.
        /// </summary>
        /// <param name="name">Name of the requested feature</param>
        /// <param name="length">Length in bytes to read</param>
        /// <exception cref="System.ArgumentException">If exceptions are enabled, on failure exception is thrown with valid error message.</exception>
        /// <returns>Binary array value for requested feature. Null on failure, if exceptions are not enabled.</returns>
        /// <remarks>On failure, use GetLastError() ot GetLastErrorMessage() for more information.</remarks>
        public byte[] GetBin(string name, int length)
        {
            int retLen;
            byte[] buf = new byte[length];

            retLen = (int)MHLSRV._MHL_GetBin(loadedDrvHandle, name, buf, (int)length);
            if (this.GetLastError() != 0)
            {
                if (throwsException)
                    throw new System.ArgumentException("Get BIN Failed " + name, "Error " + this.GetLastError().ToString() + "; " + this.GetLastErrorMessage());
                return null;
            }

            buf = (byte[])ResizeArray(buf, retLen);

            return buf;
        }

        /// <summary>
        /// This function is used to read the value of MHL feature functions of BINARY type formatted as HEX string.
        /// </summary>
        /// <param name="name">Name of the requested feature</param>
        /// <param name="delim">Delimeter used for each byte</param>
        /// <exception cref="System.ArgumentException">If exceptions are enabled, on failure exception is thrown with valid error message.</exception>
        /// <returns>String formatted as HEX values for requested feature. Null on failure, if exceptions are not enabled.</returns>
        /// <remarks>On failure, use GetLastError() ot GetLastErrorMessage() for more information.</remarks>
        public string GetBinAsHexString(string name, string delim)
        {
            byte[] buf = this.GetBin(name);
            string ret = "";
            bool first = true;
            for (int i = 0; i != buf.Length; i++)
            {
                if (!first && delim.Length > 0)
                    ret += delim;
                ret += buf[i].ToString("X2");
                first = false;
            }
            return ret;
        }

        /// <summary>
        /// This function is used to read the value of MHL feature functions of BINARY type formatted as ASCII string.
        /// </summary>
        /// <param name="name">Name of the requested feature</param>
        /// <param name="delim">Delimeter used for each byte</param>
        /// <exception cref="System.ArgumentException">If exceptions are enabled, on failure exception is thrown with valid error message.</exception>
        /// <returns>String formatted as ASCII values for requested feature. Null on failure, if exceptions are not enabled.</returns>
        /// <remarks>On failure, use GetLastError() ot GetLastErrorMessage() for more information.</remarks>
        public string GetBinAsAsciiString(string name, string delim)
        {
            byte[] buf = this.GetBin(name);
            string ret = "";
            bool first = true;
            for (int i = 0; i != buf.Length; i++)
            {
                if (!first && delim.Length > 0)
                    ret += delim;
                ret += ((char)buf[i]).ToString();
                first = false;
            }
            return ret;
        }
    }
}
