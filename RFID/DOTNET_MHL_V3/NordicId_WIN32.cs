using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;

namespace NordicId
{
    /// <summary>
    /// Common WIN32 API functions used by NordicId classes
    /// </summary>
    public class WIN32
    {
        /** MSGQUEUEOPTIONS struct */
        [StructLayout(LayoutKind.Sequential)]
        public struct MSGQUEUEOPTIONS
        {
            internal int dwSize;
            internal int dwFlags;
            internal int dwMaxMessages;
            internal int cbMaxMessage;
            [MarshalAs(UnmanagedType.Bool)]
            internal bool bReadAccess;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct SYSTEM_POWER_STATUS_EX2
        {
            public byte ACLineStatus;
            public byte BatteryFlag;
            public byte BatteryLifePercent;
            public byte Reserved1;
            public int BatteryLifeTime;
            public int BatteryFullLifeTime;
            public byte Reserved2;
            public byte BackupBatteryFlag;
            public byte BackupBatteryLifePercent;
            public byte Reserved3;
            public int BackupBatteryLifeTime;
            public int BackupBatteryFullLifeTime;
            public int BatteryVoltage;
            public int BatteryCurrent;
            public int BatteryAverageCurrent;
            public int BatteryAverageInterval;
            public int BatterymAHourConsumed;
            public int BatteryTemperature;
            public int BackupBatteryVoltage;
            public byte BatteryChemistry;
        }

        public const byte AC_LINE_OFFLINE                 = 0x00;
        public const byte AC_LINE_ONLINE                  = 0x01;
        public const byte AC_LINE_BACKUP_POWER            = 0x02;
        public const byte AC_LINE_UNKNOWN                 = 0xFF;

        public const byte BATTERY_FLAG_HIGH               = 0x01;
        public const byte BATTERY_FLAG_LOW                = 0x02;
        public const byte BATTERY_FLAG_CRITICAL           = 0x04;
        public const byte BATTERY_FLAG_CHARGING           = 0x08;
        public const byte BATTERY_FLAG_NO_BATTERY         = 0x80;
        public const byte BATTERY_FLAG_UNKNOWN            = 0xFF;
        public const byte BATTERY_PERCENTAGE_UNKNOWN      = 0xFF;
        public const uint BATTERY_LIFE_UNKNOWN = 0xFFFFFFFF;

        /** MSGQUEUEOPTIONS constant */
        public const bool ACCESS_WRITE = false;
        /** MSGQUEUEOPTIONS constant */
        public const bool ACCESS_READ = true;
        /** MSGQUEUEOPTIONS constant */
        public const int MSGQUEUE_NOPRECOMMIT = 1;
        /** MSGQUEUEOPTIONS constant */
        public const int MSGQUEUE_ALLOW_BROKEN = 2;

        /** WIN32 constant */
        public const int INFINITE = -1;
        /** WIN32 constant */
        public const int EVENT_RESET = 2;
        /** WIN32 constant */
        public const int EVENT_SET = 3;

        /** WIN32 constant */
        public const int WAIT_OBJECT_0 = 0;
        /** WIN32 constant */
        public const int WAIT_ABANDONED = 0x80;

        /** Invalid handle value constant (0xFFFFFFFF) */
        public const int INVALID_HANDLE_VALUE = -1;

        /** ShowWindow constant */
        public const int SW_SHOW = 5;
        /** ShowWindow constant */
        public const int SW_HIDE = 0;

        /** FormatMessage constant */
        public const int FORMAT_MESSAGE_FROM_SYSTEM = 0x00001000;

        /** SystemParametersInfo4Strings constant */
        public const int SPI_GETPLATFORMTYPE = 257;
        /** SystemParametersInfo4Strings constant */
        public const int SPI_GETOEMINFO = 258;

        /** CreateFile constant */
        public const uint GENERIC_READ = (0x80000000);
        /** CreateFile constant */
        public const uint GENERIC_WRITE = (0x40000000);
        /** CreateFile constant */
        public const uint CREATE_NEW = 1;
        /** CreateFile constant */
        public const uint CREATE_ALWAYS = 2;
        /** CreateFile constant */
        public const uint OPEN_EXISTING = 3;
        /** CreateFile constant */
        public const uint OPEN_ALWAYS = 4;
        /** CreateFile constant */
        public const int TRUNCATE_EXISTING = 5;

        /// <summary>
        /// Creates or opens a named or unnamed event object.
        /// </summary>
        /// <remarks>Do not use, unless you know WIN32 API</remarks>
        [DllImport("coredll.dll", EntryPoint = "CreateEventW", SetLastError = true)]
        static public extern IntPtr CreateEvent(IntPtr sec_attr, bool manual_reset, bool initial_state, String name);
        /// <summary>
        /// Closes an open object handle.
        /// </summary>
        /// <remarks>Do not use, unless you know WIN32 API</remarks>
        [DllImport("coredll.dll", SetLastError = true)]
        static public extern bool CloseHandle(IntPtr handle);
        /// <summary>
        /// Waits until the specified object is in the signaled state or the time-out interval elapses.
        /// </summary>
        /// <remarks>Do not use, unless you know WIN32 API</remarks>
        [DllImport("coredll.dll", SetLastError = true)]
        static public extern uint WaitForSingleObject(IntPtr handle, uint milliseconds);
        /// <summary>
        /// Windows internal function to set event state.
        /// </summary>
        /// <remarks>Do not use, unless you know WIN32 API</remarks>
        [DllImport("coredll.dll", SetLastError = true)]
        static public extern bool EventModify(IntPtr handle, int evt_change);

        /// <summary>
        /// This function creates or opens a user-defined message queue.
        /// </summary>
        /// <remarks>Do not use, unless you know WIN32 API</remarks>
        [DllImport("coredll.dll", SetLastError = true)]
        static public extern IntPtr CreateMsgQueue(string lpszName, ref MSGQUEUEOPTIONS lpOptions);
        /// <summary>
        /// This function closes a currently open message queue.
        /// </summary>
        /// <remarks>Do not use, unless you know WIN32 API</remarks>
        [DllImport("coredll.dll", SetLastError = true)]
        static public extern bool CloseMsgQueue(IntPtr hMsgQ);
        /// <summary>
        /// This function reads a single message from a message queue.
        /// </summary>
        /// <remarks>Do not use, unless you know WIN32 API</remarks>
        [DllImport("coredll.dll", SetLastError = true)]
        static public extern bool ReadMsgQueue(IntPtr hMsgQ, IntPtr lpBuffer, int cbBufferSize, out int lpNumberOfBytesRead, int dwTimeout, out int pdwFlags);

        /// <summary>
        /// Retrieves a handle to the top-level window whose class name and window name match the specified strings. This function does not search child windows. This function does not perform a case-sensitive search.
        /// </summary>
        /// <remarks>Do not use, unless you know WIN32 API</remarks>
        [DllImport("coredll.dll", EntryPoint = "FindWindowW", SetLastError = true)]
        static public extern uint FindWindow(string ClassName, string WindowName);
        /// <summary>
        /// Sets the specified window's show state. 
        /// </summary>
        /// <remarks>Do not use, unless you know WIN32 API</remarks>
        [DllImport("coredll.dll", SetLastError = true)]
        static public extern uint ShowWindow(uint hwnd, int cmd);
        /// <summary>
        /// Determines the visibility state of the specified window. 
        /// </summary>
        /// <remarks>Do not use, unless you know WIN32 API</remarks>
        [DllImport("coredll.dll", SetLastError = true)]
        static public extern bool IsWindowVisible(uint hwnd);

        /// <summary>
        /// Formats a message string. The function requires a message definition as input.
        /// </summary>
        /// <remarks>Do not use, unless you know WIN32 API</remarks>
        [DllImport("coredll.dll", SetLastError = true)]
        public static extern int FormatMessage(int flags, IntPtr source, int messageId, int languageId, StringBuilder
             buffer, int size, IntPtr arguments);

        /// <summary>
        /// Retrieves or sets the value of one of the system-wide parameters. Only for string parameters!
        /// </summary>
        /// <remarks>Do not use, unless you know WIN32 API</remarks>
        [DllImport("Coredll.dll", EntryPoint = "SystemParametersInfoW", CharSet = CharSet.Unicode)]
        public static extern int SystemParametersInfo4Strings(uint uiAction, uint uiParam, StringBuilder pvParam, uint fWinIni);

        /// <summary>
        /// Defines a system-wide hot key.
        /// </summary>
        /// <remarks>Do not use, unless you know WIN32 API</remarks>
        [DllImport("coredll.dll", SetLastError = true)]
        public static extern bool RegisterHotKey(IntPtr hWnd, int id, KeyModifiers Modifiers, int key);

        /// <summary>
        /// Windows CE internal function
        /// </summary>
        /// <remarks>Do not use, unless you know WIN32 API</remarks>
        [DllImport("coredll.dll", SetLastError = true)]
        public static extern bool UnregisterFunc1(KeyModifiers modifiers, int key); 

        /// <summary>
        /// Return platform type string
        /// </summary>
        public static string GetPlatformType()
        {
            StringBuilder platformType = new StringBuilder(128);
            if (SystemParametersInfo4Strings((uint)SPI_GETPLATFORMTYPE,
                (uint)platformType.Capacity, platformType, 0) == 0)
                throw new Exception("Error getting platform type");
            return platformType.ToString();
        }

        /// <summary>
        /// Return OEM info string.
        /// In Merlin this is "Nordic ID Merlin" and in morphic this is "Nordic ID Morphic"
        /// </summary>
        public static string GetOemInfo()
        {
            StringBuilder oemInfo = new StringBuilder(128);
            if (SystemParametersInfo4Strings((uint)SPI_GETOEMINFO,
                (uint)oemInfo.Capacity, oemInfo, 0) == 0)
                throw new Exception("Error getting OEM info");
            return oemInfo.ToString();
        }

        /// <summary>
        /// Sets the specified event object to the signaled state.
        /// </summary>
        /// <remarks>Do not use, unless you know WIN32 API</remarks>
        static public bool SetEvent(IntPtr handle)
        {
            return EventModify(handle, EVENT_SET);
        }

        /// <summary>
        /// Sets the specified event object to the nonsignaled state.
        /// </summary>
        /// <remarks>Do not use, unless you know WIN32 API</remarks>
        static public bool ResetEvent(IntPtr handle)
        {
            return EventModify(handle, EVENT_RESET);
        }

        /// <summary>
        /// Get specific WIN32 error occured as formatted string
        /// </summary>
        /// <param name="err">Speficic error code for message</param>
        static public string GetErrorMessage(int err)
        {
            switch ((MHLError)err)
            {
                case MHLError.MHL_FEATURE_NOT_FOUND: return "MHL Named feature not found";
                case MHLError.MHL_NOT_READABLE: return "MHL Feature is not readable";
                case MHLError.MHL_NOT_WRITABLE: return "MHL Feature is not writable";
                case MHLError.MHL_OUT_OF_RANGE: return "MHL Value is out of feature specific range";
                case MHLError.MHL_INVALID_TYPE: return "MHL Value is different type than the one in driver";
                case MHLError.MHL_INVALID_ARGUMENT: return "MHL Value is invalid";
                case MHLError.MHL_INVALID_HANDLE: return "MHL Handle to driver is invalid";
                case MHLError.MHL_DRIVER_NOT_FOUND: return "MHL Named driver not found";
                case MHLError.MHL_DRIVER_OPEN_FAILED: return "MHL Open driver failed";
                case MHLError.MHL_FEATURE_NOTSUPP: return "MHL Feature not supported";
                case MHLError.MHL_FEATURE_NOTIMP: return "MHL Feature not implemented";
                case MHLError.MHL_DRIVER_NOT_OPEN: return "MHL Trying to close not open driver";
                case MHLError.MHL_PROFILE_NOT_FOUND: return "MHL Trying to load profile that doesn't exists";
                case MHLError.MHL_ERROR_BUFFER_TOO_BIG: return "MHL Too long buffer passed in to the set function";
                case MHLError.MHL_ERROR_BUFFER_TOO_SMALL: return "MHL Too small buffer passed in to get function";
                case MHLError.MHL_IMAGER_NEED_LICENSE_C: return "MHL Need C version of imager license to access these features";
                case MHLError.MHL_SCAN_NO_RESULT: return "MHL No scan result available";
                case MHLError.MHL_SCAN_IN_PROGRESS: return "MHL Scanning in progress";
                case MHLError.MHL_RFID_NO_TAG: return "MHL RFID No active tag";
                case MHLError.MHL_RFID_DEVICE_ERROR: return "MHL RFID Device error";
                case MHLError.MHL_RFID_INVALID_CHIP: return "MHL RFID Current id has no valid tag";
                case MHLError.MHL_RFID_NOT_SUPPORTED: return "MHL RFID No implementation";
                case MHLError.MHL_RFID_INVALID_TAGTYPE: return "MHL RFID For some reason currently active tag type is invalid";
                case MHLError.MHL_RFID_NOT_FOUND: return "MHL RFID NOT FOUND";
                case MHLError.MHL_RFID_INVALID: return "MHL RFID Invalid parameter";
                case MHLError.MHL_RFID_OUTOFRANGE: return "MHL RFID Block pointer, end point etc. out of range";
                case MHLError.MHL_RFID_ICODESEL_ERROR: return "MHL RFID ICode select failed";
                case MHLError.MHL_RFID_ISO15READ_ERROR: return "MHL RFID ISO15693 read failed";
                case MHLError.MHL_RFID_ISO15WRITE_ERROR: return "MHL RFID ISO15693 write failed";
                case MHLError.MHL_RFID_ISO15TAG_ERROR: return "MHL RFID ISO15693 tag reported an error (e.g. command not supported)";
                case MHLError.MHL_RFID_ICODEREAD_ERROR: return "MHL RFID ICode read failed";
                case MHLError.MHL_RFID_ICODEWRITE_ERROR: return "MHL RFID ICode write failed";
                case MHLError.MHL_RFID_ICODE_HALT_ERROR: return "MHL RFID ICode halt failed";
                case MHLError.MHL_RFID_LOW_POWER: return "MHL RFID RF on low power when amplifier needed";
                case MHLError.MHL_RFID_TOO_MANY: return "MHL RFID Too many tags in field when single write";
                case MHLError.MHL_RFID_COLLISION: return "MHL RFID Collision";
                case MHLError.MHL_UHF_WRITE_ERROR: return "MHL RFID UHF write error; Bad address, bank etc.";
                case MHLError.MHL_RFID_PARTIAL_READ: return "MHL RFID Partial read failure";
                case MHLError.MHL_RFID_PARTIAL_WRITE: return "MHL RFID Partial write failure";
                case MHLError.MHL_RFID_ACCESS_ERROR: return "MHL RFID Tag could not be accessed; Invalid password / air error";
                case MHLError.MHL_RFID_LOCK_ERROR: return "MHL RFID Lock operation failed";
                case MHLError.MHL_RFID_CODE_NOT_VALID: return "MHL RFID Scanned code had an invalid first 4 bits i.e. other than 0x3";
                case MHLError.MHL_RFID_CODE_NOT_ENABLED: return "MHL RFID Scanned code was not enabled";
                case MHLError.MHL_RFID_INVALID_PARTITION: return "MHL RFID Decoded code has invalid partition bits";
                case MHLError.MHL_RFID_INVALID_COMPANY: return "MHL RFID Decoded code has invalid company prefix (too big)";
                case MHLError.MHL_RFID_INVALID_PART2: return "MHL RFID Partition part 2 was invalid";
            }

            StringBuilder sbFormatMessage = new StringBuilder(1024);
            FormatMessage(FORMAT_MESSAGE_FROM_SYSTEM, IntPtr.Zero, err, 0, sbFormatMessage, sbFormatMessage.Capacity, IntPtr.Zero);
            return sbFormatMessage.ToString();
        }

        /// <summary>
        /// Get last WIN32 error occured as formatted string
        /// </summary>
        static public string GetLastErrorMessage()
        {
            return GetErrorMessage(GetLastError());
        }

        /// <summary>
        /// Get last WIN32 error occured
        /// </summary>
        static public int GetLastError()
        {
            return Marshal.GetLastWin32Error();
        }

        /// <summary>
        /// This function creates, opens, or truncates a file, COM port, device, service, or console. It returns a handle to access the object.
        /// </summary>
        /// <remarks>Do not use, unless you know WIN32 API</remarks>
        [DllImport("Coredll.dll", EntryPoint = "CreateFile", CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern IntPtr CreateFile(
            String lpFileName,
            uint dwDesiredAccess,
            uint dwShareMode,
            IntPtr lpSecurityAttributes,
            uint dwCreationDisposition,
            uint dwFlagsAndAttributes,
            IntPtr hTemplateFile);

        /// <summary>
        /// This function sends an IOCTL directly to a specified device driver, causing the corresponding device to perform the specified operation.
        /// </summary>
        /// <remarks>Do not use, unless you know WIN32 API</remarks>
        [DllImport("Coredll.dll", EntryPoint = "DeviceIoControl", CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern bool DeviceIoControl(
          IntPtr hDevice,
          uint dwIoControlCode,
          IntPtr lpInBuffer,
          uint nInBufferSize,
          IntPtr lpOutBuffer,
          uint nOutBufferSize,
          IntPtr lpBytesReturned,
          IntPtr lpOverlapped);

        /// <summary>
        /// This function reads data from a file, starting at the position indicated by the file pointer. 
        /// After the read operation has been completed, the file pointer is adjusted by the number of bytes actually read. 
        /// </summary>
        /// <remarks>Do not use, unless you know WIN32 API</remarks>
        [DllImport("Coredll.dll", EntryPoint = "ReadFile", CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern bool ReadFile(
          IntPtr hFile,
          IntPtr lpBuffer,
          uint nNumberOfBytesToRead,
          IntPtr lpNumberOfBytesRead,
          IntPtr lpOverlapped);

        /// <summary>
        /// This function writes data to a file. WriteFile starts writing data to the file at the position indicated by the file pointer. 
        /// After the write operation has been completed, the file pointer is adjusted by the number of bytes actually written. 
        /// </summary>
        /// <remarks>Do not use, unless you know WIN32 API</remarks>
        [DllImport("Coredll.dll", EntryPoint = "WriteFile", CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern bool WriteFile(
          IntPtr hFile,
          IntPtr lpBuffer,
          uint nNumberOfBytesToWrite,
          IntPtr lpNumberOfBytesWritten,
          IntPtr lpOverlapped);

        /// <summary>
        /// This function obtains the following information about the amount of space available on a disk volume: 
        /// the total amount of space, the total amount of free space, and the amount of free space available 
        /// to the user associated with the calling thread.
        /// </summary>
        /// <param name="lpDirectoryName">[in] Pointer to a null-terminated string that specifies a directory on the specified disk. This string can be a Universal Naming Convention (UNC) name.</param>
        /// <param name="lpFreeBytesAvailable">[out] Pointer to a variable to receive the total number of free bytes on the disk that are available to the user associated with the calling thread.</param>
        /// <param name="lpTotalNumberOfBytes">[out] Pointer to a variable to receive the total number of bytes on the disk that are available to the user associated with the calling thread.</param>
        /// <param name="lpTotalNumberOfFreeBytes">[out] Pointer to a variable to receive the total number of free bytes on the disk. This parameter can be NULL.</param>
        /// <returns>Nonzero indicates success. Zero indicates failure. To get extended error information, call GetLastError.</returns>
        [DllImport("coredll.dll", SetLastError = true, CharSet = CharSet.Auto)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool GetDiskFreeSpaceEx(string lpDirectoryName,
                                              out ulong lpFreeBytesAvailable,
                                              out ulong lpTotalNumberOfBytes,
                                              out ulong lpTotalNumberOfFreeBytes);

        [StructLayout(LayoutKind.Sequential)]
        public struct PROCESS_INFORMATION
        {
            public IntPtr hProcess;
            public IntPtr hThread;
            public int dwProcessId;
            public int dwThreadId;
        }
        
        /// <summary>
        /// If this flag is set, the calling process is treated as a debugger, and the new process is a process being debugged. Child processes of the new process are also debugged. 
        /// The system notifies the debugger of all debug events that occur in the process being debugged.
        /// If you create a process with this flag set, only the calling thread (the thread that called CreateProcess) can call the WaitForDebugEvent function.
        /// </summary>
        public const uint DEBUG_PROCESS = 0x00000001;
        /// <summary>
        /// If this flag is set, the calling process is treated as a debugger, and the new process is a process being debugged. 
        /// No child processes of the new process are debugged. 
        /// The system notifies the debugger of all debug events that occur in the process being debugged.
        /// </summary>
        public const uint DEBUG_ONLY_THIS_PROCESS = 0x00000002;
        /// <summary>
        /// The primary thread of the new process is created in a suspended state, and does not run until the ResumeThread function is called.
        /// </summary>
        public const uint CREATE_SUSPENDED = 0x00000004;
        /// <summary>
        /// The new process has a new console, instead of inheriting the parent's console. 
        /// </summary>
        public const uint CREATE_NEW_CONSOLE = 0x00000010;
        /// <summary>
        /// If this flag is set, the new process inherits the priority of the creator process.
        /// </summary>
        public const uint INHERIT_CALLER_PRIORITY = 0x00020000;

        /// <summary>
        /// This function is used to run a new program. It creates a new process and its primary thread. The new process executes the specified executable file. 
        /// </summary>
        /// <param name="lpApplicationName">[in] Pointer to a null-terminated string that specifies the module to execute. 
        /// The string can specify the full path and filename of the module to execute or it can specify a partial path and filename. 
        /// The lpszImageName parameter must be non-NULL and must include the module name. 
        /// </param>
        /// <param name="lpCommandLine">[in, out] Pointer to a null-terminated string that specifies the command line to execute. 
        /// The system adds a null character to the command line, trimming the string if necessary, to indicate which file was used. 
        /// The lpszCmdLine parameter can be NULL. In that case, the function uses the string pointed to by lpszImageName as the command line. 
        /// If lpszImageName and lpszCmdLine are non-NULL, * lpszImageName specifies the module to execute, and * lpszCmdLine specifies the command line. 
        /// C runtime processes can use the argc and argv arguments. 
        /// If the filename does not contain an extension, .EXE is assumed. 
        /// If the filename ends in a period (.) with no extension, or if the filename contains a path, .EXE is not appended. 
        /// </param>
        /// <param name="lpProcessAttributes">[in] Not supported; set to NULL. </param>
        /// <param name="lpThreadAttributes">[in] Not supported; set to NULL.</param>
        /// <param name="bInheritHandles">[in] Not supported; set to FALSE.</param>
        /// <param name="dwCreationFlags">[in] Specifies additional flags that control the priority and the creation of the process. 
        /// </param>
        /// <param name="lpEnvironment">[in] Not supported; set to NULL.</param>
        /// <param name="lpCurrentDirectory">[in] Not supported; set to NULL.</param>
        /// <param name="lpStartupInfo">[in] Not supported; set to NULL.</param>
        /// <param name="lpProcessInformation">[out] Pointer to a PROCESS_INFORMATION structure that receives identification information about the new process. </param>
        /// <returns>Nonzero indicates success. 
        /// Zero indicates failure. 
        /// To get extended error information, call GetLastError.
        /// </returns>
        [DllImport("coredll.dll", SetLastError = true)]
        public static extern uint CreateProcess(string lpApplicationName,
           string lpCommandLine, IntPtr lpProcessAttributes,
           IntPtr lpThreadAttributes, bool bInheritHandles,
           uint dwCreationFlags, IntPtr lpEnvironment, string lpCurrentDirectory,
           [In] IntPtr lpStartupInfo,
           out PROCESS_INFORMATION lpProcessInformation);

        /// <summary>
        /// This function retrieves the termination status of the specified process.
        /// </summary>
        /// <param name="Handle">[in] Handle to the process.</param>
        /// <param name="Wait">[out] Pointer to a 32-bit variable to receive the process termination status.</param>
        /// <returns>Nonzero indicates success. Zero indicates failure. To get extended error information, call GetLastError.</returns>
        [DllImport("CoreDLL.dll", SetLastError = true)]
        public static extern bool GetExitCodeProcess(IntPtr Handle, out uint Wait);

        /// <summary>
        /// This function retrieves the power status of the system. The status indicates whether the system is running on AC or DC power, whether or not the batteries are currently charging, and the remaining life of main and backup batteries.
        /// </summary>
        /// <param name="pStatus">[out] Pointer to the SYSTEM_POWER_STATUS_EX structure receiving the power status information.</param>
        /// <param name="fUpdate">[in] If this Boolean is set to TRUE, GetSystemPowerStatusEx gets the latest information from the device driver, otherwise it retrieves cached information that may be out-of-date by several seconds.</param>
        /// <returns>This function returns TRUE if successful; otherwise, it returns FALSE.</returns>
        [DllImport("coredll.dll", SetLastError = true)]
        public static extern int GetSystemPowerStatusEx2([Out, In] ref SYSTEM_POWER_STATUS_EX2 pSystemPowerStatusEx2, [MarshalAs(UnmanagedType.U4), In] int dwLen, [MarshalAs(UnmanagedType.Bool), In] bool fUpdate);


        public const uint STANDARD_RIGHTS_REQUIRED = 0x000F0000;
        public const uint SYNCHRONIZE = 0x00100000;
        public const uint EVENT_ALL_ACCESS = (STANDARD_RIGHTS_REQUIRED | SYNCHRONIZE | 0x3);
        public const uint EVENT_MODIFY_STATE = 0x0002;
        public const long ERROR_FILE_NOT_FOUND = 2L;

        [DllImport("coredll.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern IntPtr OpenEvent(int desiredAccess, bool inheritHandle, string name);

        [DllImport("coredll.dll")]
        public static extern uint GetFileSize(IntPtr hFile, IntPtr lpFileSizeHigh);

        [DllImport("coredll.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern int GetFileAttributes(string lpFileName);
    }
}
