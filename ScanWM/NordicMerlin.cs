using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using System.Threading;
using Microsoft.Win32;

using NordicId;

#if NRDMERLIN   

namespace ScannerAll.Nordic
{

    public class NordicMerlinFactory : BarcodeScannerFactory
    {
        public NordicMerlinFactory(TERM_TYPE tt)
        {
            base.OEMInf = tt;
        }

        public override BarcodeScanner GetBarcodeScanner(string OEM)
        {
            return (new Nordics(base.OEMInf, OEM));
        }

    }

    public class Nordics : BarcodeScanner
    {
        // Virtual key contant for F12

        private const string PREV_PROFILE = "Prev_SkladGP_Profile";

        // коды клавиш для Nordic
        public enum VK_NRD : int
        {
            VK_HANDGRIP   = 249,        // клавиша на рукоятке
            VK_CIRCLE     = 248,        // SCAN_RFID ?

            VK_PERIOD     = 190,        // точка
            VK_F12        = 123,
            VK_SCAN       = 233,
            VK_SIP        = 234,        // экранная клава
        
            VK_NOREAL     = 255
        }


        // MHL drivers
        MHLDriver
            hScan = null,
            hRfid = null,
            hKeyb = null;

        // Hotkey capture
        HotkeyHelper 
            hkHelperRFID = null,
            hotkeyHelper = null;

        private int 
            m_BC_ScanKey = -1,
            m_RFID_ScanKey = -1;

        private bool 
            m_ScanInProgress = false,
            m_RFIDInProgress = false;

        private
            List<string> lstEPC;
        private int 
            m_TagsCount = 0;
        private string
            m_OEM;

        public Nordics(TERM_TYPE t, string OEM)
        {
            base.nTermType = t;
            m_OEM = OEM;
        }

        public class MerlinWiFi : WiFiStat
        {
            private bool
                m_Enabled,
                m_Associated = false;

            private int
                m_SigPercent = 0,
                m_LinkQuality = 0;
            private System.Windows.Forms.Timer 
                tmSig = null;

            private CustPB cSym;
            private MHLDriver 
                hUtil = null,
                hWlan = null;

            public MerlinWiFi()
                : base()
            {
                string
                    //sM,
                    s;
                hWlan = new MHLDriver();
                hUtil = new MHLDriver();
                try
                {
                    hWlan.Open("WLAN");
                    hUtil.Open("Utility");

                    //s = hWlan.GetString("WLAN.MacAddress").Replace(":", "").ToUpper();
                    //try
                    //{
                    //    s = hWlan.GetString("WLAN.MacAddress");
                    //}
                    //catch
                    //{
                    //    s = "";
                    //}
                    //if (s.Length == 0)

                    try
                    {
                        s = hUtil.GetString("Utility.MacAddress");
                    }
                    catch
                    {
                        s = "000000000000";
                    }
                    s = s.Replace(":", "").ToUpper();

                    //s = hUtil.GetString("Utility.MacAddress");
                    //s = hWlan.GetString("WLAN.MacAddress");
                    //sM = hUtil.GetString("Utility.MacAddress").Replace(":", "").ToUpper();
                    base.MACAddreess = s;

                    //base.STB = new CustPB(this, "cSym");
                    base.SigTB = new CustPB(this, "cSym");
                    base.ConfigTextBox(32, 25, 164, 18);

                    tmSig = new System.Windows.Forms.Timer();
                    tmSig.Enabled = false;
                    tmSig.Interval = 1000;
                    tmSig.Tick += new EventHandler(tmSig_Tick);
                }
                catch (Exception ex)
                {
                    throw new Exception("WiFi не открыта!");
                }

            }

            public void tmSig_Tick(object sender, EventArgs e)
            {
                int
                    nSigPrc = 0,
                    nSigStr = 0;
                try
                {
                    nSigPrc = (int)hWlan.GetDword("WLAN.SignalStrength");
                    nSigStr = hWlan.GetInt("WLAN.SignalStrength.dBm");
                }
                catch
                {
                    nSigPrc = 0;
                    nSigStr = 0;
                }

                base.SignalPercent = nSigPrc;
                //base.SigStrenght = nSigStr;
                //base.STB.Invalidate();
                base.SigTB.Invalidate();
            }

            //public override bool IsEnabled
            //{
            //    get { return m_Enabled; }
            //    set
            //    {
            //        if (value == true)
            //        {
            //            tmSig.Enabled = true;
            //            tmSig_Tick(tmSig, new EventArgs());
            //        }
            //        else
            //        {
            //            tmSig.Enabled = false;
            //        }
            //        m_Enabled = value;
            //    }
            //}




            public MHLDriver DrvWLAN
            {
                get { return hWlan; }
                //set { hRfid = value; }
            }
            public MHLDriver DrvUtil
            {
                get { return hUtil; }
                //set { hRfid = value; }
            }

            public virtual string MACAddreess
            {
                get { return sMACAddr; }
            }


        }

        /// <summary>
        /// Инициализация сканера Nordic Merlin
        /// </summary>
        /// <returns>Whether initialization was successful</returns>
        public override bool Initialize()
        {
            //base.nTermType = TERM_TYPE.NRDMERLIN;
            base.WiFi = new MerlinWiFi();
            return (true);
        }









        private bool ReadersInit(string sProfName)
        {
            bool 
                bSetVK,
                bRet = false;

            do
            {// MHL drivers
                hKeyb = new MHLDriver();
                try
                {
                    hKeyb.Open("Keyboard");
                }
                catch (Exception ex)
                {
                    throw new Exception("Клавиатура не открыта!");
                }
                // Hotkey capture
                hotkeyHelper = new HotkeyHelper();

                hScan = new MHLDriver();
                try
                {
                    hScan.Open("Scanner");
                    bRet = hScan.SetDword("Scanner.Timeout", 5);

                    bSetVK = hKeyb.SetDword("Keyboard.ScanMode", 0);
                    bSetVK = hKeyb.SetDword("SpecialKey.Scan.All.VK", BarCodeScanKey);
                }
                catch (Exception ex)
                {
                    throw new Exception("Сканер не открыт!");
                }

                if (RFIDScanKey > 0)
                {
                    hRfid = new MHLDriver();
                    try
                    {
                        hRfid.Open("RFID");
                    }
                    catch (Exception ex)
                    {
                        throw new Exception("RFID Driver failure: " + ex.ToString());
                    }
                    if (hRfid.IsOpen())
                    {
                        try
                        {
                            //hRfid.SetString("RFID.TagType", "EPC C1G2");
                        }
                        catch (Exception ex)
                        {
                            throw new Exception("RFID Ttype failure: " + ex.ToString());
                        }
                    }
                }


                if (hKeyb.IsOpen())
                {
                    // Save current keyboard map
                    hKeyb.SaveProfile(sProfName);
                    // Map "Scan" button to VK_F12 (0x7B)
                    if (RFIDScanKey > 0)
                    {
                        //hKeyb.SetDword("SpecialKey.Square.All.VK", 0);
                        if (base.nTermType == TERM_TYPE.NRDMERLIN)
                        {
                            bool bn = hKeyb.SetDword("SpecialKey.Circle.All.VK", RFIDScanKey);
                        }
                    }
                    // Reload map
                    hKeyb.SetDword("Keyboard.Reload", 1);
                }


                hotkeyHelper.SetCallbackDelegate(new HotkeyHelper.HotkeyCallback(HandleScanKey));
                // Register BarCodeScanKey as global hotkey
                hotkeyHelper.RegisterKey(BarCodeScanKey, KeyModifiers.None);

                //hkHelperRFID = new HotkeyHelper();
                //hkHelperRFID.SetCallbackDelegate(new HotkeyHelper.HotkeyCallback(HandleRFIDScanKey));
                //hkHelperRFID.RegisterKey(RFIDScanKey, KeyModifiers.None);


                bRet = true;

            } while (false);

            return (bRet);
        }







        // Обработка нажатия на скан-клавишу
        public void HandleScanKey(int vk)
        {
            if (vk == BarCodeScanKey)
            {
                PerformScan();
            }
        }


		private delegate void SetScanResultTextDelegate(String message);

        private void SetScanResultText(String message, String sAIMSI)
        {
            BCId 
                bcID = BCId.Unknown;

            //if (base.BCInvoker.InvokeRequired)
            //{
            //    // we were called on a worker thread
            //    // marshal the call to the user interface thread
            //    base.BCInvoker.Invoke(new SetScanResultTextDelegate(SetScanResultText), new object[] { message });
            //    return;
            //}
             //this code can only be reached
             //by the user interface thread

            //this.Scan_Text.Text = message;


            if (message.Length > 0)
            {
                //if (message[message.Length - 1] == '\0')
                //    message = message.Remove(message.Length - 1, 1);

                if (dicAIMs.ContainsKey(sAIMSI))
                    bcID = dicAIMs[sAIMSI];
                else if (dicAIMs.ContainsKey(sAIMSI.Substring(0, 1)))
                    bcID = dicAIMs[sAIMSI.Substring(0, 1)];
            }

            OnBarcodeScan(new BarcodeScannerEventArgs( bcID, message ));
        }

        private void SetScanState(Boolean scanning)
        {
            //if (this.InvokeRequired)
            //{
            //    // We were called on a worker thread,
            //    // marshal the call to the user interface thread
            //    this.Invoke(new SetScanStateDelegate(SetScanState), new object[] { scanning });
            //    return;
            //}

            // This code can only be reached by the user interface thread
            //Scan_Text.Enabled = !scanning;
            ScanInProgress = scanning;
            //Scan.Text = scanning ? "Cancel" : "Scan";
        }

        private void ScannerWorkerThreadFunction()
        {
            SetScanState(true);
            //SetScanResultText("");

            if (hScan.IsOpen())
            {
                // Start scanning
                try
                {
                    hScan.SetDword("Scanner.Scan", 2);
                }
                catch
                {
                    // Ignore
                }

                if (hScan.GetLastError() == 0)
                {
                    // We got something
                    string sID = hScan.GetString("Scanner.ScanAIMSI").Substring(1);
                    SetScanResultText(hScan.GetString("Scanner.ScanResultString"), sID);
                }
                else
                {
                    // Hmm.. Couldn't decode code, show human readable message
                    //SetScanResultText(hScan.GetString("Scanner.ScanResultInfo"));
                }
            }
            else
            {
                //SetScanResultText("Scanner not available");
            }
            // Re-enable controls
            SetScanState(false);
        }

        public void PerformScan()
        {
            if (hScan.IsOpen())
            {
                // If 'Scan_Text' control is disabled, we're still scanning..
                //if (Scan_Text.Enabled == false)



                if (ScanInProgress)
                {
                    // Cancel                
                    hScan.Execute("Scanner.CancelScan");
                }
                else
                {
                    // Create worker thread for scanning
                    //Thread ScannerThread = new Thread(new ThreadStart(this.ScannerWorkerThreadFunction));
                    //ScannerThread.Start();
                    ScannerWorkerThreadFunction();
                }
            }
            else
            {
                //SetScanResultText("Scanner not available");
            }
        }


        #region RFID функции

        // Обработка нажатия на RFID-скан-клавишу
        public void HandleRFIDScanKey(int vk)
        {
            if (vk == RFIDScanKey)
            {
                PerformInventory();
            }
        }


        private void PerformInventory()
        {
            if (RFIDInProgress)
            {
                // Still running..
                return;
            }

            lstEPC = new List<string>();
            TagsCount = 0;

            if (hRfid.IsOpen())
            {
                // Create worker thread for scanning
                //Thread inventoryThread = new Thread(new ThreadStart(this.InventoryWorkerThreadFunction));
                //inventoryThread.Start();
                InventoryWorkerThreadFunction();
            }
            else
            {
                AddTagItem("RFID failure");
            }
        }

        private void InventoryWorkerThreadFunction()
        {
            uint error = 0;
            int tag_count = 0;

            //SetInventoryState(true);
            RFIDInProgress = true;
            //AddTagItem("Scanning..");

            try
            {
                hRfid.Execute("RFID.Inventory");
            }
            catch
            {
                // Ignore error
            }

            error = hRfid.GetDword("RFID.ExecError");
            tag_count = hRfid.GetInt("RFID.TagsCount");

            if (error == 0)
            {
                //AddTagItem("Scanning.." + tag_count.ToString() + " Tags found");

                for (uint i = 0; i < tag_count; i++)
                {
                    hRfid.SetDword("RFID.CurrentId", i);
                    string serial = hRfid.GetString("RFID.SerialString");
                    AddTagItem("Slot " + i.ToString("d2") + ": " + serial);
                }
            }
            else
            {
                //AddTagItem("Scanning..RFID Read Failed, reason: " + error.ToString("d"));
            }

            //AddTagItem("Done");
            //SetInventoryState(false);
            RFIDInProgress = false;
        }

        private delegate void AddTagItemDelegate(String message);

        private void AddTagItem(String message)
        {
            //if (this.InvokeRequired)
            //{
            //    // We were called on a worker thread,
            //    // marshal the call to the user interface thread
            //    this.Invoke(new AddTagItemDelegate(AddTagItem), new object[] { message });
            //    return;
            //}

            // Ehis code can only be reached by the user interface thread
            lstEPC.Add(message);
        }

        //private delegate void SetInventoryStateDelegate(Boolean running);

        // Used for setting text to UI component from another thread
        //private void SetInventoryState(Boolean running)
        //{
        //    //if (this.InvokeRequired)
        //    //{
        //    //    // we were called on a worker thread
        //    //    // marshal the call to the user interface thread
        //    //    this.Invoke(new SetInventoryStateDelegate(SetInventoryState), new object[] { running });
        //    //    return;
        //    //}

        //    // This code can only be reached by the user interface thread
        //    RFIDInProgress = running;

        //    //try
        //    //{
        //    //    hKeyBl.SetDword("KeyBacklight.scan", running ? 0 : 100);
        //    //}
        //    //catch
        //    //{
        //    //    // Ignore
        //    //}
        //}

        #endregion


        /// <summary>
        /// Start сканера Nordic Merlin
        /// </summary>
        public override void Start()
        {
            if (BarCodeScanKey < 0)
                BarCodeScanKey = (int)VK_NRD.VK_HANDGRIP;

            bool ret = ReadersInit(PREV_PROFILE);
        }

        /// <summary>
        /// Stop сканера Nordic Merlin
        /// </summary>
        public override void Stop()
        {
        }

        /// <summary>
        /// Terminates сканера Nordic Merlin
        /// </summary>
        public override void Terminate()
        {
        }

        public override bool TouchScr(bool bEnable)
        {
            bool bRet = true;
            if (!bEnable)
            {// отключение TouchScreen
                if (System.Environment.OSVersion.Version.Major <= 5)
                    TouchPanelDisable();
                else
                {
                }
            }
            else
            {// включение TouchScreen
            }
            return (bRet);

        }


        #region IDisposable Members
        public override void Dispose(bool disposing)
        {
            if (disposing)
            {
                // Code to clean up managed resources
                if (hScan != null)
                {
                    hScan.Execute("Scanner.CancelScan");
                    hScan.Close();
                    hScan = null;
                }

                if ((hKeyb != null) && hKeyb.IsOpen())
                {
                    hKeyb.LoadProfile(PREV_PROFILE);
                    hKeyb.SetDword("Keyboard.Reload", 1);
                    hKeyb.Close();
                    hKeyb = null;
                }
                if (hRfid != null)
                {
                    hRfid.Close();
                    hRfid = null;
                }
                if (((MerlinWiFi)base.WiFi).DrvWLAN != null)
                {
                    ((MerlinWiFi)base.WiFi).DrvWLAN.Close();
                    
                }
                if (((MerlinWiFi)base.WiFi).DrvUtil != null)
                {
                    ((MerlinWiFi)base.WiFi).DrvUtil.Close();

                }
            }
            // Code to clean up unmanaged resources
        }
        #endregion

        public int BarCodeScanKey
        {
            get { return m_BC_ScanKey; }
            set { m_BC_ScanKey = value; }
        }

        public int RFIDScanKey
        {
            get { return m_RFID_ScanKey; }
            set { m_RFID_ScanKey = value; }
        }

        public bool ScanInProgress
        {
            get { return m_ScanInProgress; }
            set { m_ScanInProgress = value; }
        }

        public bool RFIDInProgress
        {
            get { return m_RFIDInProgress; }
            set { m_RFIDInProgress = value; }
        }

        public MHLDriver DrvRFID
        {
            get { return hRfid; }
            //set { hRfid = value; }
        }


        public List<string> GetAvailTags()
        {
            PerformInventory();
            return (lstEPC);
        }

        public int TagsCount
        {
            get { return m_TagsCount; }
            set { m_TagsCount = value; }
        }


    }

    public class RFIDLabelInfo
    {
        private MHLDriver
            m_hRfid = null;
        private int
            m_KillPass,
            m_AccPass,
            m_Offset,
            m_Count;

        public bool
            m_Secure,
            m_LockEPC,
            m_LockUD,
            XTIDPresent,
            LReady4Write,
            LReady;

        public int nCurInList;

        public byte[] aRes;
        public byte[] aEPC;
        public byte[] aTID;
        public byte[] aUData;
        public byte[] aBLR;

        public bool[] aSecured = new bool[5] { false, false, false, false, false };

        public uint
            nMDID,
            nModNum,
            nCRC,
            nLenEPC,
            nPC;
        public long lSerialInt;

        public string BinEPC;

        public uint Bytes2Int(byte[] aR, int nOfs)
        {
            return (((uint)aR[0 + nOfs] << 24) + ((uint)aR[1 + nOfs] << 16) +
                    ((uint)aR[2 + nOfs] << 8) + ((uint)aR[3 + nOfs]));
        }


        public bool Old_PassLocked()
        {
            bool
                bOld = Drv.throwsException,
                bRet = false;
            byte[]
                aPass;

            try
            {
                Drv.throwsException = true;
                Drv.SetBool("RFID.EPCC1G2.Secured", false);

                if (Drv.SetDword("RFID.EPCC1G2.Bank", 0))
                {// чтение Pass
                    Drv.SetDword("RFID.BlockPointer", 0);
                    Drv.SetDword("RFID.BlockCount", 4);
                    try
                    {
                        aPass = Drv.GetBin("RFID.BlockData");
                    }
                    catch
                    {
                        aSecured[0] = true;
                    }
                    Drv.SetDword("RFID.BlockPointer", 4);
                    Drv.SetDword("RFID.BlockCount", 4);
                    try
                    {
                        aPass = Drv.GetBin("RFID.BlockData");
                    }
                    catch
                    {
                        aSecured[1] = true;
                    }

                    Drv.SetDword("RFID.BlockPointer", 0);
                    Drv.SetDword("RFID.BlockCount", 8);
                    aRes = Drv.GetBin("RFID.BlockData");
                    KillPassword = (int)Bytes2Int(aRes, 0);
                    if (KillPassword > 0)
                        aSecured[0] = true;
                    AccessPassword = (int)Bytes2Int(aRes, 4);
                    if (AccessPassword > 0)
                        aSecured[1] = true;
                    bRet = true;
                }
            }
            catch
            {
            }
            finally
            {
                Drv.throwsException = bOld;
            }
            return (bRet);
        }




        // установка флагов парольной защиты
        public bool PassLocked()
        {
            bool
                bOld = Drv.throwsException,
                bRet = false;
            //byte[]
            //    aPass;

            try
            {
                Drv.throwsException = true;
                Drv.SetBool("RFID.EPCC1G2.Secured", false);

                if (Drv.SetDword("RFID.EPCC1G2.Bank", 0))
                {// чтение Pass
                    //Drv.SetDword("RFID.BlockPointer", 0);
                    //Drv.SetDword("RFID.BlockCount", 4);
                    //try
                    //{
                    //    aPass = Drv.GetBin("RFID.BlockData");
                    //}
                    //catch
                    //{
                    //    aSecured[0] = true;
                    //}
                    //Drv.SetDword("RFID.BlockPointer", 4);
                    //Drv.SetDword("RFID.BlockCount", 4);
                    //try
                    //{
                    //    aPass = Drv.GetBin("RFID.BlockData");
                    //}
                    //catch
                    //{
                    //    aSecured[1] = true;
                    //}

                    try
                    {
                        Drv.SetDword("RFID.BlockPointer", 0);
                        Drv.SetDword("RFID.BlockCount", 8);
                        aRes = Drv.GetBin("RFID.BlockData");
                        KillPassword = (int)Bytes2Int(aRes, 0);
                        if (KillPassword > 0)
                            aSecured[0] = true;
                        //AccessPassword = (int)Bytes2Int(aRes, 4);
                        //if (AccessPassword > 0)
                        //    aSecured[1] = true;
                    }
                    catch
                    {
                        bRet = 
                        aSecured[1] = true;
                    }


                    //bRet = true;
                }
            }
            catch
            {
            }
            finally
            {
                Drv.throwsException = bOld;
            }
            return (bRet);
        }






        //private string Byte2Hex(byte[] aB)
        //{
        //    string s = "";
        //    for (int i = 0; i < aB.Length; i++)
        //    {
        //        s += aB[i].ToString("X2");
        //    }
        //    return (s);
        //}

        private byte[] Hex2Byte(string s)
        {
            string s1b;
            int j = 0;
            byte[] aB = new byte[s.Length / 2];
            for (int i = 0; i < s.Length; i = i + 2)
            {
                s1b = s.Substring(i, 2);
                aB[j++] = byte.Parse(s1b, System.Globalization.NumberStyles.HexNumber);
            }
            return (aB);
        }


        private void Old_TIDInfo(MHLDriver hRf)
        {
            bool
                bOld = hRf.throwsException;
            int nC = 4;
            byte[]
                aSerial,
                aB;

            hRf.throwsException = false;
            hRf.SetDword("RFID.BlockPointer", 0);
            hRf.SetDword("RFID.BlockCount", nC);
            aB = hRf.GetBin("RFID.BlockData");

            nMDID = ((uint)aB[2]) >> 4;
            nMDID = ((uint)aB[1] << 8) + nMDID;

            nModNum = ((uint)aB[2]) & 0x0F;
            nModNum = (nModNum << 8) + ((uint)aB[3]);
            if ((aB[1] & 0x80) > 0)
            {// флаг XTID установлен
                XTIDPresent = true;
            }

            hRf.SetDword("RFID.BlockPointer", 6);
            hRf.SetDword("RFID.BlockCount", 6);
            aSerial = hRf.GetBin("RFID.BlockData");

            aTID = new byte[12];
            aB.CopyTo(aTID, 0);
            if (aSerial != null)
            {
                aSerial.CopyTo(aTID, 4);
                lSerialInt = (long)((aSerial[0] << 8) + aSerial[1]) << 32;
                lSerialInt += (long)((aSerial[2] << 8) + aSerial[3]) << 16;
                lSerialInt += (long)((aSerial[4] << 8) + aSerial[5]);
            }

            hRf.throwsException = bOld;
        }

        private void TIDInfo(MHLDriver hRf)
        {
            bool
                bOld = hRf.throwsException;
            int nC = 4;
            byte[]
                aSerial,
                aB;

            hRf.throwsException = false;
            hRf.SetDword("RFID.BlockPointer", 0);
            hRf.SetDword("RFID.BlockCount", nC);
            //aB = new byte[128];
            //aB = hRf.GetBin("RFID.EPCC1G2.Read");

            aB = hRf.GetBin("RFID.BlockData");

            nMDID = ((uint)aB[2]) >> 4;
            nMDID = ((uint)aB[1] << 8) + nMDID;

            nModNum = ((uint)aB[2]) & 0x0F;
            nModNum = (nModNum << 8) + ((uint)aB[3]);
            if ((aB[1] & 0x80) > 0)
            {// флаг XTID установлен
                XTIDPresent = true;
            }

            hRf.SetDword("RFID.BlockPointer", 6);
            hRf.SetDword("RFID.BlockCount", 6);
            aSerial = hRf.GetBin("RFID.BlockData");

            aTID = new byte[12];
            aB.CopyTo(aTID, 0);
            if (aSerial != null)
            {
                aSerial.CopyTo(aTID, 4);
                lSerialInt = (long)((aSerial[0] << 8) + aSerial[1]) << 32;
                lSerialInt += (long)((aSerial[2] << 8) + aSerial[3]) << 16;
                lSerialInt += (long)((aSerial[4] << 8) + aSerial[5]);
            }

            hRf.throwsException = bOld;
        }


        public RFIDLabelInfo(string sCurInvent, MHLDriver hRf)
        {
            Drv = hRf;
            nCurInList = -1;
            int i = sCurInvent.IndexOf("Slot");
            if (i >= 0)
            {
                nCurInList = int.Parse(sCurInvent.Substring(i + 5, 2));
                GetRFIDLabelInfo();
            }
        }

        public RFIDLabelInfo(int nT, MHLDriver hRf)
        {
            Drv = hRf;
            nCurInList = nT;
            if (nT >= 0)
                GetRFIDLabelInfo();
        }

        // подготовка метки по индексу в списке инвентаризации
        private void GetRFIDLabelInfo()
        {
            byte[] aB;

            LReady = false;
            XTIDPresent = false;
            lSerialInt = 0;

            aEPC = new byte[0];
            aTID = new byte[0];
            aBLR = new byte[0];
            BinEPC = "";

            nCRC = nLenEPC = nPC = 0;
            nMDID = nModNum = 0;
            Secure = false;

            try
            {
                if (Drv.SetDword("RFID.CurrentId", nCurInList))
                {
                    aEPC = Drv.GetBin("RFID.ChipId");
                    LReady = true;
                    PassLocked();
                    LReady4Write = !aSecured[1];
                    //if (aSecured[1] && (AccessPassword == 0))
                    //{
                    //    return;
                    //}

                    if (Drv.SetBin("RFID.EPCC1G2.Id", aEPC))
                    {
                        // доступ
                        Drv.SetBool("RFID.EPCC1G2.Secured", false);

                        //Drv.SetBool("RFID.EPCC1G2.Secured", aSecured[1]);

                        if (Drv.SetDword("RFID.EPCC1G2.Bank", 2))
                        {// чтение TID
                            int nC = 4;

                            TIDInfo(Drv);

                            bool bOld = Drv.throwsException;
                            Drv.throwsException = false;
                            for (nC = 4; nC <= 12; nC = nC + 2)
                            {
                                Drv.SetDword("RFID.BlockPointer", 0);
                                Drv.SetDword("RFID.BlockCount", nC);
                                aB = Drv.GetBin("RFID.BlockData");
                                if ((aB == null) || (aB.Length == 0))
                                    continue;
                                aTID = aB;
                            }
                            Drv.throwsException = bOld;
                            nMDID = ((uint)aTID[2]) >> 4;
                            nMDID = ((uint)aTID[1] << 8) + nMDID;

                            nModNum = ((uint)aTID[2]) & 0x0F;
                            nModNum = (nModNum << 8) + ((uint)aTID[3]);

                        }
                        if (Drv.SetDword("RFID.EPCC1G2.Bank", 1))
                        {// чтение служебной перед EPC
                            Drv.SetDword("RFID.BlockPointer", 0);
                            Drv.SetDword("RFID.BlockCount", 16);
                            aBLR = Drv.GetBin("RFID.BlockData");

                            nCRC = ((uint)aBLR[0] << 8) + ((uint)aBLR[1]);
                            nPC = ((uint)aBLR[2] << 8) + ((uint)aBLR[3]);
                            nLenEPC = ((uint)aBLR[2] >> 3);
                        }
                    }
                    else
                    {
                        aBLR = aEPC;
                    }
                }
                //*****
            }
            catch (Exception e)
            {
                LReady = false;
                MessageBox.Show("RFID Error!");
            }
        }

        // режим Secure
        public bool Secure
        {
            get { return m_Secure; }
            set { m_Secure = value; }
        }

        // пароль на уничтожение
        public int KillPassword
        {
            get { return m_KillPass; }
            set { m_KillPass = value; }
        }

        // пароль на доступ
        public int AccessPassword
        {
            get { return m_AccPass; }
            set { m_AccPass = value; }
        }


        // смещение в банке памяти
        public int WOffset
        {
            get { return m_Offset; }
            set { m_Offset = value; }
        }

        public int WCount
        {
            get { return m_Count; }
            set { m_Count = value; }
        }

        public MHLDriver Drv
        {
            get { return m_hRfid; }
            set { m_hRfid = value; }
        }



        // Запись в банки Reserved или UserData
        public bool WriteBank(int nBank, string s, int nOffs, int nAccPass)
        {
            bool
                bSec,
                bRet = false;
            byte[]
                aBinData;
            int
                nCount;

            try
            {
                if (nBank == 0)
                    s = s.PadLeft(8, '0');
                aBinData = Hex2Byte(s);
                nCount = aBinData.Length;

                bSec = (nBank > 0) ? aSecured[nBank + 1] :
                    (nOffs == 0) ? aSecured[0] : aSecured[1];
                if (bSec)
                    bRet = Drv.SetInt("RFID.EPCC1G2.Password", NordPass(nAccPass));
                bRet = Drv.SetBool("RFID.EPCC1G2.Secured", bSec);

                if (bRet && (Drv.SetBin("RFID.EPCC1G2.Id", aEPC)))
                {
                    if (Drv.SetDword("RFID.EPCC1G2.Bank", nBank))
                    {
                        bRet = Drv.SetDword("RFID.BlockPointer", nOffs);
                        bRet &= Drv.SetDword("RFID.BlockCount", nCount);
                        bRet &= Drv.SetBin("RFID.BlockData", aBinData);
                        //bRet &= Drv.SetBin("RFID.EPCC1G2.Write", aBinData);
                    }
                }
            }
            catch
            {
                bRet = false;
                MessageBox.Show(Drv.GetLastErrorMessage());
            }

            return (bRet);
        }


        // Установка защиты от записи
        public bool LockTag(int nLockKPass, int nLockAPass, int nLockEPC, int nLockUData, int nAPass, bool bSec)
        {
            bool
                bRet = false;
            int
                nLP,
                nPayLoad = 0;

            if (nLockKPass >= 0)
            {
                nPayLoad |= (3 << 18);              // Mask
                nPayLoad |= (nLockKPass << 8);      // Action
            }
            if (nLockAPass >= 0)
            {
                nPayLoad |= (3 << 16);              // Mask
                nPayLoad |= (nLockAPass << 6);
            }
            if (nLockEPC >= 0)
            {
                nPayLoad |= (3 << 14);              // Mask
                nPayLoad |= (nLockEPC << 4);
            }
            if (nLockUData >= 0)
            {
                nPayLoad |= (3 << 10);              // Mask
                nPayLoad |= (nLockUData);
            }

            try
            {
                if (Drv.SetInt("RFID.EPCC1G2.Password", NordPass(nAPass)))
                {
                    if (Drv.SetBool("RFID.EPCC1G2.Secured", true))
                    {
                        if (Drv.SetBin("RFID.EPCC1G2.Id", aEPC))
                        {
                            //if (Drv.SetInt("RFID.EPCC1G2.LockPayload", 0))
                            //{
                            //    if (Drv.SetInt("RFID.EPCC1G2.LockPayload", nPayLoad))
                            //    {
                            //        nLP = Drv.GetInt("RFID.EPCC1G2.LockPayload");
                            //        bRet = Drv.SetBool("RFID.EPCC1G2.Lock", true);
                            //    }
                            //}
                            if (Drv.SetInt("RFID.EPCC1G2.LockPayload", 0))
                            {
                                if (bSec)
                                {
                                    bRet = Drv.SetBool("RFID.EPCC1G2.PwdMem.Kill.Secured", true);
                                    bRet &= Drv.SetBool("RFID.EPCC1G2.PwdMem.Access.Secured", true);
                                    bRet &= Drv.SetBool("RFID.EPCC1G2.UIIMemory.Secured", true);
                                    bRet &= Drv.SetBool("RFID.EPCC1G2.UserMemory.Secured", true);
                                }
                                else
                                {
                                    bRet = Drv.SetBool("RFID.EPCC1G2.PwdMem.Kill.Open", true);
                                    bRet &= Drv.SetBool("RFID.EPCC1G2.PwdMem.Access.Open", true);
                                    bRet &= Drv.SetBool("RFID.EPCC1G2.UIIMemory.Open", true);
                                    bRet &= Drv.SetBool("RFID.EPCC1G2.UserMemory.Open", true);
                                }
                                nLP = Drv.GetInt("RFID.EPCC1G2.LockPayload");

                                if (bRet)
                                {
                                    bRet = Drv.SetBool("RFID.EPCC1G2.Lock", true);
                                }
                            }
                        }
                    }
                }
            }
            catch
            {
                bRet = false;
                MessageBox.Show(m_hRfid.GetLastErrorMessage());
            }

            return (bRet);
        }

        // Очистка метки
        public bool ClearTag(int nAPass)
        {
            bool
                bSec,
                bRet = false;
            try
            {


                if (m_hRfid.SetDword("RFID.EPCC1G2.Bank", 0))
                {
                    bSec = aSecured[0];
                    m_hRfid.SetBool("RFID.EPCC1G2.Secured", bSec);
                    if (bSec)
                        Drv.SetInt("RFID.EPCC1G2.Password", NordPass(nAPass));
                    if (LockTag(0, 0, 0, 0, nAPass, false))
                    {
                        bRet = m_hRfid.SetDword("RFID.BlockPointer", 0);
                        bRet &= m_hRfid.SetDword("RFID.BlockCount", 8);
                        bRet &= m_hRfid.SetBin("RFID.BlockData", Hex2Byte("0000000000000000"));

                    }
                }
            }
            catch
            {
                MessageBox.Show(m_hRfid.GetLastErrorMessage());
            }

            return (bRet);
        }

        // Пиздец метке
        public bool KillTag(int nKPass)
        {
            bool
                bSec,
                bRet = false;
            try
            {
                if (Drv.SetInt("RFID.KillPassword", NordPass(nKPass)))
                {
                    if (Drv.SetDword("RFID.CurrentId", nCurInList))
                        bRet = Drv.SetBool("RFID.KillTag", true);
                }

            }
            catch
            {
                bRet = false;
                MessageBox.Show(m_hRfid.GetLastErrorMessage());
            }
            return (bRet);
        }

        public bool WriteEPC(string s, int nAPass)
        {
            object
                xObj;
            bool
                bRet = false;
            int
                i,
                nBPtr, nBCount,
                nCurLabel;
            uint
                j, k, l, m, n;

            string
                sE = "";

            byte[]
                aBin128,
                aBinData;

            bool
                UseTags = false,
                UseBlockData = true;



            if ((LReady) && (s.Length > 0))
            {
                aBinData = Hex2Byte(s);
                try
                {
                    bRet = Drv.SetInt("RFID.EPCC1G2.Password", NordPass(nAPass));
                    bRet = Drv.SetBool("RFID.EPCC1G2.Secured", (nAPass > 0) ? true : false);
                    bRet = Drv.SetBin("RFID.EPCC1G2.Id", aEPC);

                    if (bRet && Drv.SetDword("RFID.EPCC1G2.Bank", 1))
                    {
                        // смещение BlockPointer
                        //nBPtr = nOffs;

                        // количество байт BlockCount
                        nBCount = aBinData.Length;

                        if (UseTags)
                        {
                        }
                        else
                        {
                            byte nNewLen = (byte)(nBCount / 2);
                            nNewLen <<= 3;
                            byte SavedAttr = aBLR[2];
                            SavedAttr &= 7;// три последних разряда 
                            nNewLen |= SavedAttr;
                            nBCount += 2;

                            aBin128 = new byte[nBCount];
                            aBinData.CopyTo(aBin128, 2);
                            aBin128[0] = nNewLen;
                            aBin128[1] = aBLR[3];

                            bRet = m_hRfid.SetDword("RFID.BlockPointer", 2);
                            bRet &= m_hRfid.SetDword("RFID.BlockCount", nBCount);

                            if (bRet)
                            {
                                sE = "Запись EPC";
                                if (UseBlockData)
                                {
                                    //bRet = m_hRfid.SetBin("RFID.BlockData", aBinData);
                                    bRet = m_hRfid.SetBin("RFID.BlockData", aBin128);
                                }
                                else
                                {
                                    //bRet = m_hRfid.SetBin("RFID.EPCC1G2.Write", aBinData);
                                    bRet = m_hRfid.SetBin("RFID.EPCC1G2.Write", aBin128);
                                }
                            }
                        }
                    }
                }
                catch
                {
                    bRet = false;
                    string
                        sErr = String.Format("{0} ({1})", m_hRfid.GetLastErrorMessage(), sE);
                    MessageBox.Show(sErr);
                }
            }
            return (bRet);
        }





        public bool SearchRL(string EPC)
        {
            bool bRet = false;
            string s = Drv.GetString("RFID.ScanSingleString");
            if (s == EPC)
                bRet = true;
            return (bRet);
        }

        public int NordPass(int nP)
        {
            byte[]
                inB = BitConverter.GetBytes(nP),
                outB = new byte[] { 0, 0, 0, 0 };

            outB[0] = inB[3];
            outB[1] = inB[2];
            outB[2] = inB[1];
            outB[3] = inB[0];
            return (BitConverter.ToInt32(outB, 0));
        }



        // Установка защиты от записи
        public bool LockTagTTT()
        {
            bool
                bRet = false;
            int
                nLP;

            try
            {
                bRet = Drv.SetBool("RFID.EPCC1G2.Secured", false);
                bRet &= Drv.SetBin("RFID.EPCC1G2.Id", aEPC);
                if (bRet && Drv.SetDword("RFID.EPCC1G2.Bank", 0))
                {
                    // Kill-пароль
                    bRet = Drv.SetDword("RFID.BlockPointer", 0);
                    bRet &= Drv.SetDword("RFID.BlockCount", 4);
                    bRet &= Drv.SetBin("RFID.BlockData", new byte[] { 0x0, 0x0, 0xAA, 0xAA });

                    // Access-пароль
                    bRet = Drv.SetDword("RFID.BlockPointer", 4);
                    bRet &= Drv.SetDword("RFID.BlockCount", 4);
                    //bRet &= Drv.SetBin("RFID.BlockData", new byte[] { 0x0, 0x0, 0x22, 0x22 });
                    bRet &= Drv.SetBin("RFID.BlockData", new byte[] { 0x0, 0x0D, 0xE3, 0x13 });
                }

                //if (Drv.SetInt("RFID.EPCC1G2.Password", 0x22220000))
                nLP = NordPass(0xDE313);
                if (Drv.SetInt("RFID.EPCC1G2.Password", nLP))
                {
                    if (Drv.SetBool("RFID.EPCC1G2.Secured", true))
                    {
                        if (Drv.SetInt("RFID.EPCC1G2.LockPayload", 0))
                        {
                            //if (Drv.SetInt("RFID.EPCC1G2.LockPayload", 0x000FCEA2))
                            //{
                            //    nLP = Drv.GetInt("RFID.EPCC1G2.LockPayload");
                            //    bRet = Drv.SetBool("RFID.EPCC1G2.Lock", true);
                            //}

                            bRet = Drv.SetBool("RFID.EPCC1G2.PwdMem.Kill.Secured", true);
                            bRet &= Drv.SetBool("RFID.EPCC1G2.PwdMem.Access.Secured", true);
                            bRet &= Drv.SetBool("RFID.EPCC1G2.UIIMemory.Secured", true);
                            bRet &= Drv.SetBool("RFID.EPCC1G2.UserMemory.Secured", true);

                            nLP = Drv.GetInt("RFID.EPCC1G2.LockPayload");
                            bRet = Drv.SetBool("RFID.EPCC1G2.Lock", true);
                        }

                    }
                }
            }
            catch
            {
                bRet = false;
                MessageBox.Show(m_hRfid.GetLastErrorMessage());
            }
            return (bRet);
        }



        // Установка защиты от записи
        public bool LockTagTTT(string sKP, string sAP)
        {
            bool
                bRet = false;
            int
                nLP;
            byte[]
                bD;
            string
                sE = "";

            try
            {
                bRet = Drv.SetBool("RFID.EPCC1G2.Secured", false);
                bRet &= Drv.SetBin("RFID.EPCC1G2.Id", aEPC);
                if (bRet && Drv.SetDword("RFID.EPCC1G2.Bank", 0))
                {
                    // Kill-пароль
                    bRet = Drv.SetDword("RFID.BlockPointer", 0);
                    bRet &= Drv.SetDword("RFID.BlockCount", 4);
                    bD = Hex2Byte(sKP.PadLeft(8, '0'));
                    sE = "KPass";
                    bRet &= Drv.SetBin("RFID.BlockData", bD);

                    // Access-пароль
                    bRet = Drv.SetDword("RFID.BlockPointer", 4);
                    bRet &= Drv.SetDword("RFID.BlockCount", 4);
                    bD = Hex2Byte(sAP.PadLeft(8, '0'));
                    sE = "APass";
                    bRet &= Drv.SetBin("RFID.BlockData", bD);
                }

                nLP = int.Parse(sAP, System.Globalization.NumberStyles.HexNumber);
                if (Drv.SetInt("RFID.EPCC1G2.Password", NordPass(nLP)))
                {
                    if (Drv.SetBool("RFID.EPCC1G2.Secured", true))
                    {
                        if (Drv.SetInt("RFID.EPCC1G2.LockPayload", 0))
                        {
                            //if (Drv.SetInt("RFID.EPCC1G2.LockPayload", 0x000FCEA2))
                            //{
                            //    nLP = Drv.GetInt("RFID.EPCC1G2.LockPayload");
                            //    bRet = Drv.SetBool("RFID.EPCC1G2.Lock", true);
                            //}

                            bRet = Drv.SetBool("RFID.EPCC1G2.PwdMem.Kill.Secured", true);
                            bRet &= Drv.SetBool("RFID.EPCC1G2.PwdMem.Access.Secured", true);
                            bRet &= Drv.SetBool("RFID.EPCC1G2.UIIMemory.Secured", true);
                            bRet &= Drv.SetBool("RFID.EPCC1G2.UserMemory.Secured", true);

                            nLP = Drv.GetInt("RFID.EPCC1G2.LockPayload");
                            sE = "Lock";
                            bRet = Drv.SetBool("RFID.EPCC1G2.Lock", true);
                        }

                    }
                }
            }
            catch
            {
                bRet = false;
                string
                    s = String.Format("{0} ({1})", Drv.GetLastErrorMessage(), sE);
                MessageBox.Show(s);
            }
            return (bRet);
        }





        // Снять 
        public bool ClearTagTTT()
        {
            bool
                bRet = false;
            int
                nLP;

            try
            {
                bRet = Drv.SetBin("RFID.EPCC1G2.Id", aEPC);

                //if (Drv.SetInt("RFID.EPCC1G2.Password", 0x22220000))
                if (Drv.SetInt("RFID.EPCC1G2.Password", NordPass(0xDE313)))
                {
                    if (Drv.SetBool("RFID.EPCC1G2.Secured", true))
                    {
                        if (Drv.SetInt("RFID.EPCC1G2.LockPayload", 0))
                        {
                            if (Drv.SetInt("RFID.EPCC1G2.LockPayload", 0x000FCC00))
                            {
                                nLP = Drv.GetInt("RFID.EPCC1G2.LockPayload");
                                bRet = Drv.SetBool("RFID.EPCC1G2.Lock", true);
                            }
                        }

                    }
                }
                if (Drv.SetDword("RFID.EPCC1G2.Bank", 0))
                {
                    // Clear 
                    bRet = Drv.SetDword("RFID.BlockPointer", 0);
                    bRet &= Drv.SetDword("RFID.BlockCount", 8);
                    bRet &= Drv.SetBin("RFID.BlockData", new byte[] { 0, 0, 0, 0, 0, 0, 0, 0 });
                }

            }
            catch
            {
                bRet = false;
                MessageBox.Show(m_hRfid.GetLastErrorMessage());
            }
            return (bRet);
        }





    }


}

#endif