using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;

namespace NordicId
{
    /// <summary>
    /// MHL common service interface and constants
    /// </summary>
    public enum MHLError
    {
        /** MHL Errors starting number */
        MHL_ERROR_BASE = 20000,

        /** Named feature not found */
        MHL_FEATURE_NOT_FOUND,
        /** Feature is not readable */
        MHL_NOT_READABLE,
        /** Feature is not writable */
        MHL_NOT_WRITABLE,
        /** Value is out of feature specific range */
        MHL_OUT_OF_RANGE,
        /** Value is different type than the one in driver */
        MHL_INVALID_TYPE,
        /** Value is invalid */
        MHL_INVALID_ARGUMENT,
        /** Handle to driver is invalid */
        MHL_INVALID_HANDLE,
        /** Named driver not found */
        MHL_DRIVER_NOT_FOUND,
        /** Open driver failed */
        MHL_DRIVER_OPEN_FAILED,
        /** Feature not supported */
        MHL_FEATURE_NOTSUPP,
        /** Feature not implemented */
        MHL_FEATURE_NOTIMP,
        /** Trying to close not open driver */
        MHL_DRIVER_NOT_OPEN,
        /** Trying to load profile that doesn't exists */
        MHL_PROFILE_NOT_FOUND,
        /** Too long buffer passed in to the set function */
        MHL_ERROR_BUFFER_TOO_BIG,
        /** Too small buffer passed in to get function */
        MHL_ERROR_BUFFER_TOO_SMALL,
        /** Need C version of imager license to access these features */
        MHL_IMAGER_NEED_LICENSE_C,

        /** Scanner related errors */
        MHL_SCAN_ERRBASE = (MHL_ERROR_BASE + 1000),
        /** No scan result available */
        MHL_SCAN_NO_RESULT = (MHL_SCAN_ERRBASE),
        /** Scanning in progress */
        MHL_SCAN_IN_PROGRESS,

        /** RFID related errors */
        MHL_RFID_ERRBASE = (MHL_ERROR_BASE + 10000),
        /** No active tag */
        MHL_RFID_NO_TAG = (MHL_RFID_ERRBASE),
        /** device (Medio etc.) error */
        MHL_RFID_DEVICE_ERROR,
        /** current id has no valid tag */
        MHL_RFID_INVALID_CHIP,
        /** no implementation */
        MHL_RFID_NOT_SUPPORTED,
        /** for some strange reason currently active tag type is invalid */
        MHL_RFID_INVALID_TAGTYPE,
        /** Not used */
        MHL_RFID_NOT_FOUND,
        /** block count=0, etc. */
        MHL_RFID_INVALID,
        /** block pointer, end point etc. out of range */
        MHL_RFID_OUTOFRANGE,
        /** ICode select failed */
        MHL_RFID_ICODESEL_ERROR,
        /** ISO15693 read failed */
        MHL_RFID_ISO15READ_ERROR,
        /** ISO15693 write failed */
        MHL_RFID_ISO15WRITE_ERROR,
        /** ISO15693 tag reported an error (e.g. command not supported etc.)*/
        MHL_RFID_ISO15TAG_ERROR,
        /** ICode read failed */
        MHL_RFID_ICODEREAD_ERROR,
        /** ICode write failed */
        MHL_RFID_ICODEWRITE_ERROR,
        /** ICode halt failed */
        MHL_RFID_ICODE_HALT_ERROR,
        /** RF on low power when amplifier needed */
        MHL_RFID_LOW_POWER,
        /** E.g. too many ICode tags in field when single write */
        MHL_RFID_TOO_MANY,
        /** E.g. too many ICode tags in field when single write */
        MHL_RFID_COLLISION,
        /** UHF specific; Bad address, bank etc. */
        MHL_UHF_WRITE_ERROR,
        /** ISO15694 direct write; Partial read failure */
        MHL_RFID_PARTIAL_READ,
        /** ISO15694 direct write; Partial write failure */
        MHL_RFID_PARTIAL_WRITE,
        /** Read error */
        MHL_RFID_READ_ERROR = (MHL_RFID_ERRBASE + 21),
        /** Tag could not be accessed; Invalid password / air error */
        MHL_RFID_ACCESS_ERROR = (MHL_RFID_ERRBASE + 21),

        /* R/W area was locked and correct access is missing */
        /** Lock operation failed */
        MHL_RFID_LOCK_ERROR,

        /** Scanned code had an invalid first 4 bits i.e. other than 0x3 */
        MHL_RFID_CODE_NOT_VALID = (MHL_RFID_ERRBASE + 50),
        /** Scanned code was not enabled */
        MHL_RFID_CODE_NOT_ENABLED,
        /** Decoded code has invalid partition bits */
        MHL_RFID_INVALID_PARTITION,
        /** Decoded code has invalid company prefix (too big) */
        MHL_RFID_INVALID_COMPANY,
        /** Partition part 2 was invalid */
        MHL_RFID_INVALID_PART2,

    }

    /// <summary>
    /// MHL common constants
    /// </summary>
    public enum MHLConst
    {
        /** Other RFID related stuff */
        MHL_RFID_MAXTAGID = 16,
        /** Other RFID related stuff */
        ISO15693_UID_LEN = 8,
        /** Other RFID related stuff */
        ICODE_BLOCK_LENGTH = 4,
        /** UHF RFID protocol */
        RFID_PROTO_ISO18_6B = 0,
        /** UHF RFID protocol */
        RFID_PROTO_EPC_C1G1 = 1,
        /** UHF RFID protocol */
        RFID_PROTO_EM = 2,

        /** Medio ISO15693 related error. */
        MED_ISO15_NO_ERROR = 0x00,
        /** Medio ISO15693 related error. */
        MED_ISO15_NO_TAG = 0x01,
        /** Medio ISO15693 related error. */
        MED_ISO15_COLLISION = 0x02,
        /** Medio ISO15693 related error. */
        MED_ISO15_BAD_FRAME = 0x04,
        /** Medio ISO15693 related error. */
        MED_ISO15_BAD_CRC = 0x08,
        /** Medio ISO15693 related error. */
        MED_ISO15_BUFFER_FULL = 0x0F,

        /** Tag type enumeration/definition */
        RFID_TAGTYPE_ISO15693 = 1,
        /** Tag type enumeration/definition */
        RFID_TAGTYPE_ICODE = 2,
        /** Tag type enumeration/definition */
        RFID_TAGTYPE_TAGIT = 3,
        /** Tag type enumeration/definition */
        RFID_TAGTYPE_ISO18000 = 4,       /* UHF */
        /** Tag type enumeration/definition */
        RFID_TAGTYPE_ISO14443A = 5,
        /** Tag type enumeration/definition */
        RFID_TAGTYPE_ISO14443B = 6
    }

    /// <summary> Nordic ID custom virtual key codes </summary>
    public enum VK
    {
        /// <summary> Base for custom virtual keys </summary>
        VK_NORDICID_BASE = 0xE8,

        /// <summary> Scan to text field </summary>        
        SCAN = (VK_NORDICID_BASE + 1),

        /// <summary> Software input panel </summary>
        SIP_TOGGLE = (VK_NORDICID_BASE + 2),
        /// <summary> Software input panel </summary>
        SIP_ON = (VK_NORDICID_BASE + 3),
        /// <summary> Software input panel </summary>
        SIP_OFF = (VK_NORDICID_BASE + 4),

        /// <summary> Keylock state </summary>
        KEYLOCK_TOGGLE = (VK_NORDICID_BASE + 5),
        /// <summary> Keylock state </summary>
        KEYLOCK_ON = (VK_NORDICID_BASE + 6),
        /// <summary> Keylock state </summary>
        KEYLOCK_OFF = (VK_NORDICID_BASE + 7),

        /// <summary> Function state </summary>
        FUNCTION_TOGGLE = (VK_NORDICID_BASE + 8),
        /// <summary> Function state </summary>
        FUNCTION_ON = (VK_NORDICID_BASE + 9),
        /// <summary> Function state </summary>
        FUNCTION_OFF = (VK_NORDICID_BASE + 10),

        /// <summary> Input mode state </summary>
        INPUT_TOGGLE = (VK_NORDICID_BASE + 11),
        /// <summary> Input mode state </summary>
        INPUT_123 = (VK_NORDICID_BASE + 12),
        /// <summary> Input mode state </summary>
        INPUT_abc = (VK_NORDICID_BASE + 13),
        /// <summary> Input mode state </summary>
        INPUT_ABC = (VK_NORDICID_BASE + 14)
    }

    /// <summary> Key Modifiers for HotkeyHelper </summary>
    public enum KeyModifiers
    {
        /// <summary> None </summary>
        None = 0,
        /// <summary> Alt </summary>
        Alt = 1,
        /// <summary> Control </summary>
        Control = 2,
        /// <summary> Shift </summary>
        Shift = 4,
        /// <summary> Windows </summary>
        Windows = 8,
        /// <summary> Modkeyup </summary>
        Modkeyup = 0x1000,
    }

    /// <summary>
    /// MHL service interface; FOR INTERNAL USE ONLY
    /// </summary>
    public class MHLSRV
    {
        /// <summary>Internal usage only</summary>
        [DllImport("mhl_srv.dll", SetLastError = true)]
        public static extern int _MHL_OpenDrv([In] string name);
        /// <summary>Internal usage only</summary>
        [DllImport("mhl_srv.dll", SetLastError = true)]
        public static extern int _MHL_CloseDrv(int handle);
        /// <summary>Internal usage only</summary>
        [DllImport("mhl_srv.dll", SetLastError = true)]
        public static extern uint _MHL_GetLastError();
        /// <summary>Internal usage only</summary>
        [DllImport("mhl_srv.dll", SetLastError = true)]
        public static extern int _MHL_SetDword(int handle, [In] string name, uint val);

        /// <summary>Internal usage only</summary>
        [DllImport("mhl_srv.dll", SetLastError = true)]
        public static extern int _MHL_SetDouble(int handle, [In] string name, ref double val);
        
        /// <summary>Internal usage only</summary>
        [DllImport("mhl_srv.dll", SetLastError = true)]
        public static extern int _MHL_SetInt(int handle, [In] string name, int val);
        /// <summary>Internal usage only</summary>
        [DllImport("mhl_srv.dll", SetLastError = true)]
        public static extern int _MHL_SetBool(int handle, [In] string name, int val);
        /// <summary>Internal usage only</summary>
        [DllImport("mhl_srv.dll", SetLastError = true)]
        public static extern int _MHL_SetString(int handle, [In] string name, string val);
        /// <summary>Internal usage only</summary>
        [DllImport("mhl_srv.dll", SetLastError = true)]
        public static extern int _MHL_SetBin(int handle, [In] string name, byte[] val, int length);
        /// <summary>Internal usage only</summary>
        [DllImport("mhl_srv.dll", SetLastError = true)]
        public static extern int _MHL_Execute(int handle, [In] string name);
        /// <summary>Internal usage only</summary>
        [DllImport("mhl_srv.dll", SetLastError = true)]
        public static extern uint _MHL_GetDword(int handle, [In] string name);

        /// <summary>Internal usage only</summary>
        [DllImport("mhl_srv.dll", SetLastError = true)]
        public static extern bool _MHL_GetDouble(int handle, [In] string name, ref double val);

        /// <summary>Internal usage only</summary>
        [DllImport("mhl_srv.dll", SetLastError = true)]
        public static extern int _MHL_GetInt(int handle, [In] string name);
        /// <summary>Internal usage only</summary>
        [DllImport("mhl_srv.dll", SetLastError = true)]
        public static extern int _MHL_GetBool(int handle, [In] string name);
        /// <summary>Internal usage only</summary>
        [DllImport("mhl_srv.dll", SetLastError = true)]
        public static extern uint _MHL_GetString(int handle, [In] string name, [In, Out] string str, int len);
        /// <summary>Internal usage only</summary>
        [DllImport("mhl_srv.dll", SetLastError = true)]
        public static extern int _MHL_GetStringLen(int handle, [In] string name);
        /// <summary>Internal usage only</summary>
        [DllImport("mhl_srv.dll", SetLastError = true)]        
        public static extern uint _MHL_GetBin(int handle, [In] string name, [In, Out] byte[] buf, int len);
        /// <summary>Internal usage only</summary>
        [DllImport("mhl_srv.dll", SetLastError = true)]
        public static extern uint _MHL_GetBinLength(int handle, [In] string name);
        /// <summary>Internal usage only</summary>
        [DllImport("mhl_srv.dll", SetLastError = true)]
        public static extern int _MHL_LoadProfile(int handle, [In] string name);
        /// <summary>Internal usage only</summary>
        [DllImport("mhl_srv.dll", SetLastError = true)]
        public static extern int _MHL_SaveProfile(int handle, [In] string name);
    }
}
