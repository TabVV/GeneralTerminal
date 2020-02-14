using System;
using System.Drawing;
using System.Windows.Forms;
using System.Threading;
using Microsoft.Win32;

#if HWELLHX2

using ManagedLXEAPI;


namespace ScannerAll.Honeywell
{

    public class HWellHX2Fact : BarcodeScannerFactory
    {
        public HWellHX2Fact()
        {
            base.OEMInf = TERM_TYPE.HWELLHX2;
        }
        public override BarcodeScanner GetBarcodeScanner(string OEM)
        {
            return (new HWellHX2Scan());
        }
    }

	/// <summary>
	/// This is the barcode scanner class for Dolphin devices.
	/// </summary>
//    public class HWellHX2Scan : BarcodeScanner 
//    {
//        //private HWellDec xEng;

//        private DecodeControl xEng;

//        private bool bMayScan = true;
//        private DecodeBase.DecodeEventHandler dgDecEvH;

//        public class HoneyWiFi : WiFiStat
//        {
//            private CustPB cSym;
//            private bool m_Enabled;
//            private System.Windows.Forms.Timer tmSig = null;
//            private int m_SigPercent = 0,
//                m_LinkQuality = 0;
//            private bool m_Associated = false;

//            private RFServices xRS;
//            private RadioManager xRM;

//            private void Init6100(int nLX, int nLY, int nSX, int nSY)
//            {
//                int nSize = 6;
//                byte[] aMAC = new byte[6];
//                cSym = new CustPB(this, "cSym");
//                base.STB = cSym;
//                //base.ConfigTextBox(32, 35, 164, 18);
//                if ((nSX > 0) && (nSY > 0))
//                    base.ConfigTextBox(nLX, nLY, nSX, nSY);
//                else
//                    base.ConfigTextBox();

//                xRS = new RFServices();
//                xRM = new RadioManager();
//                if (RFServices.GetRFAdapterMACAddress(aMAC, ref nSize))
//                {

//                    sMACAddr = "";
//                    for (int i = 0; i < 6; i++)
//                        sMACAddr += String.Format("{0:X2}", aMAC[i]);
//                }
//                else
//                    sMACAddr = "000000000000";

//                tmSig = new System.Windows.Forms.Timer();
//                tmSig.Enabled = false;
//                tmSig.Interval = 500;
//                tmSig.Tick += new EventHandler(tmSig_Tick);
//            }

//            public HoneyWiFi()
//                : base()
//            {
//                Init6100(0,0,0,0);
//                //cSym = new CustPB(this, "cSym");
//                //base.STB = cSym;
//                ////base.ConfigTextBox(32, 35, 164, 18);
//                //base.ConfigTextBox(30, 22, 164, 18);

//                //xRS = new RFServices();
//                //xRM = new RadioManager();

//                //tmSig = new System.Windows.Forms.Timer();
//                //tmSig.Enabled = false;
//                //tmSig.Interval = 500;
//                //tmSig.Tick += new EventHandler(tmSig_Tick);
//            }
//            public HoneyWiFi(int nLX, int nLY, int nSX, int nSY)
//                : base()
//            {
//                Init6100(nLX, nLY, nSX, nSY);
//            }




//            public void tmSig_Tick(object sender, EventArgs e)
//            {
//                RFServices.GetRFAdapterLinkStatus(ref m_LinkQuality, ref m_SigPercent, ref m_Associated);
//                base.SignalPercent = m_SigPercent;
//                base.SigStrenght = m_LinkQuality;
//                WiFiStat.GetIP();

//                base.STB.Invalidate();
//            }

//            public override bool IsEnabled
//            {
//                get { return m_Enabled; }
//                set
//                {
//                    if (value == true)
//                    {
//                        tmSig.Enabled = true;
//                        tmSig_Tick(tmSig, new EventArgs());
//                    }
//                    else
//                    {
//                        tmSig.Enabled = false;
//                    }
//                    m_Enabled = value; }
//            }


//            // nReg = 0 - Disable WiFi
//            // nReg = 1 - Enable WiFi
//            // nReg = 2 - Reset WiFi
//            public override bool ResetWiFi(int nReg)
//            {
//                bool ret = false;

//                if (nReg != 1)
//                {
//                        ret = xRM.Enable80211Radio(false);
//                        Thread.Sleep(2000);
//                }
//                if (nReg >= 1)
//                {
//                    ret = xRM.Enable80211Radio(true);
//                    Thread.Sleep(2000);
//                    if ((ret) && (nReg == 2))
//                    {//Wi-Fi доступен...
//                        if (ConnectionType() == WiFiStat.CONN_TYPE.NOCONNECTIONS)
//                        {
//                            int i = 0,
//                                iMax = 10;
//                            ret = false;
//                            i = 0; iMax = 10;
//                            while ((i < iMax) && (ret == false))
//                            {
//                                ret = (ConnectionType() == WiFiStat.CONN_TYPE.NOCONNECTIONS) ? false : true;
//                                if (!ret)
//                                    Thread.Sleep(2000);
//                                i++;
//                            }
//                        }
//                        if (ret)
//                            IsEnabled = true;
//                    }
//                }

//                return (ret);
//            }

//            public override string WiFiInfo()
//            {
//                bool ret = false;
//                string s;
//                RFServices.RFStatus xSt = RFServices.RFStatus.ST_SCANNING;

//                RFServices.GetRFAdapterLinkStatus(ref m_LinkQuality, ref m_SigPercent, ref m_Associated);
//                s = (m_Associated) ? "В сети" : "НЕТ сети";
//                RFServices.GetRFAdapterStatus(ref xSt);
//                s += "\r\n" + xSt.ToString();

//                int nSize = 6;
//                byte[] aMAC = new byte[6];

//                bool bM = RFServices.GetRFAdapterMACAddress(aMAC, ref nSize);

//                sMACAddr = "";
//                foreach (byte b in aMAC)
//                    sMACAddr += String.Format("{0:X2}", b);


//                return (s);
//            }

//        }


//        /// <summary>
//        /// Инициализация сканера Dolphin 6100
//        /// </summary>
//        /// <returns>Whether initialization was successful</returns>
//        public override bool Initialize() 
//        {
//            base.nTermType = TERM_TYPE.HWELLHX2;
//            xEng = new DecodeControl();

//            xEng.DecodeEvent += new DecodeBase.DecodeEventHandler(xEng_DecodeEvent);
//            xEng.TriggerKey = TriggerKeyEnum.TK_ONSCAN;
                
//            xEng.DecodeMode = DecodeMode.DM_STANDARD;
//            xEng.ScanTimeout = 5000;

//            // Разрешение DATAMATRIX
//            xEng.EnableSymbology(SymID.SYM_DATAMATRIX, true);
//            xEng.AutoScan = true;

//            // WiFi Init
//            //base.WiFi = new HoneyWiFi();
//            base.WiFi = new HoneyWiFi(30, 22, 164, 18);

//            return(true);
//        }

//        void xEng_DecodeEvent(object sender, DecodeBase.DecodeEventArgs e)
//        {
//            BCId nCodeID = BCId.NoData;
//            string sBarCode = string.Empty;
//            BarcodeScannerEventArgs ba;

//            try
//            {
//                //RightLEDOff();
//                //--- Was the Decode Attempt Successful? ---
//                if (e.DecodeResults.rResult == Result.RESULT_SUCCESS)
//                {
//                    //xEng.Device.SetLEDs(Device.LedSelect.RightGreen, Device.LedState.On, 0, 0);

//                    //sBarCode = e.DecodeResults.chCodeID;
//                    //switch (sBarCode)
//                    //{
//                    //    case "d":
//                    //    case "D":
//                    //        nCodeID = BCId.EAN13;
//                    //        break;
//                    //    case "j":
//                    //        nCodeID = BCId.Code128;
//                    //        break;
//                    //    case "w":
//                    //        nCodeID = BCId.DataMatrix;
//                    //        break;
//                    //    default:
//                    //        break;
//                    //}

//                    nCodeID = base.SetBarcodeType(e.DecodeResults.chCodeID);
//                    sBarCode = e.DecodeResults.pchMessage;

//                    //--- Play an SDK Provided Audible Sound ---
//                    //xEng.so..Sound.Play(Sound.SoundTypes.Success);
//                    ba = new BarcodeScannerEventArgs(nCodeID, sBarCode, e.DecodeResults.binaryData, e.DecodeResults.binaryData.Length);
//                    OnBarcodeScan(ba);
//                }
//                else
//                {

//                        //--- Async Decode Exception ---
//                        switch (e.DecodeResults.rResult)
//                        {
//                            case Result.RESULT_ERR_CANCEL:            // Async Decode was Canceled
//                                return;
//                            case Result.RESULT_ERR_NODECODE:          // Scan Timeout
//                                //MessageBox.Show("Scan Timeout Exceeded");
//                                break;
//                            default:
//                                //MessageBox.Show(e.DecodeException.Message);
//                                break;
//                        }

//                    //if (e.DecodeResults.rResult..DecodeException != null)
//                    //{




//                    //}
//                    //else
//                    //{
//                    //    //--- Generic Async Exception ---
//                    //    //MessageBox.Show(e.Exception.Message);
//                    //}
//                    //xEng.Sound.Play(Sound.SoundTypes.Failure);
//                }
//            }
//            catch (Exception ex)
//            {
//                //MessageBox.Show(ex.Message);
//            }

//            return;
//        }

//        private void RightLEDOff()
//        {
//            //Not all modes are supported on all devices.
//            //PlatformNotSupportedException will be thrown if this is the case.
//            //Device dha = xEng.Device;
//            try
//            {
//                //dha.SetLEDs(Device.LedSelect.RightGreen, Device.LedState.Off, 0, 0);
//            }
//            catch (PlatformNotSupportedException)
//            {
//                //Do Nothing
//            }

//            try
//            {
//                //dha.SetLEDs(Device.LedSelect.RightOrange, Device.LedState.Off, 0, 0);
//            }
//            catch (PlatformNotSupportedException)
//            {
//                //Do Nothing
//            }

//            try
//            {
//                //dha.SetLEDs(Device.LedSelect.RightRed, Device.LedState.Off, 0, 0);
//            }
//            catch (PlatformNotSupportedException)
//            {
//                //Do Nothing
//            }
//        }


//        /// <summary>
//        /// Start сканера Dolphin 9950
//        /// </summary>
//        public override void Start() 
//        {
//            if (bMayScan == true)
//            {
//                //RightLEDOff();
//                //xEng.Device.SetLEDs(Device.LedSelect.RightRed, Device.LedState.On, 0, 0);
//                //xEng.ScanBarcode();
//                xEng.Connect();
                
//            }
//            bMayScan = false;
//        }

//        /// <summary>
//        /// Stop a сканера Dolphin 9950
//        /// </summary>
//        public override void Stop() 
//        {
//            //xEng.CancelScanBarcode();
//            //RightLEDOff();
//            xEng.Disconnect();
//            bMayScan = true;
//        }

//        /// <summary>
//        /// Terminates сканера Dolphin 9950
//        /// </summary>
//        public override void Terminate()
//        {
//        }



//#region IDisposable Members
//    public override void Dispose(bool disposing)
//    {
//      if (disposing)
//      {
//        // Code to clean up managed resources
//          xEng.Dispose();
//      }
//      // Code to clean up unmanaged resources
//    }
//#endregion
//  }






    // Неизвестный тип сканера
    public class HWellHX2Scan : BarcodeScanner
    {

        ManagedLXEAPI.Scanner 
            m_LocalScan;

        public override bool Initialize()
        {
            Scanner.SCAN_SYMBOLOGY
                xSym;

            nTermType = TERM_TYPE.HWELLHX2;
            m_LocalScan = new Scanner();

            m_LocalScan.ScannerKeysOff();  // Disbable Scan Data as Keyboard Input
            m_LocalScan.BarcodeScan += new Scanner.ScannerEventHandler(ScanMessage);

            //xSym = new Scanner.SCAN_SYMBOLOGY();
            //xSym.Name = @"EAN128";
            //m_LocalScan.SymGetConfig(ref xSym);
            //xSym.stripid = 0;
            //m_LocalScan.SymSetConfig(xSym);

            return true;
        }

        private void ScanMessage(object sender)
        {

            int 
                nR,
                nMaxLen;          // Maximum length of a 1-D Barcode
            string
                sSymID,
                sBarCode = string.Empty;
            Scanner.CODE_ID
                scID;
            BCId 
                nCodeID = BCId.NoData;
            BarcodeScannerEventArgs 
                ba;

            try
            {
                //--- Was the Decode Attempt Successful? ---
                if (HX2Scan.GetStatus() == ManagedLXEAPI.Scanner.SCAN_COMPLETE)
                {
                    nMaxLen = 128;
                    sBarCode = HX2Scan.GetData(ref nMaxLen);
                    //nR = HX2Scan.PutData(sBarCode, nMaxLen);
                    //scID = HX2Scan.GetCodeID();
                    if (nMaxLen > 1)
                    {
                        if (Char.IsLetter(sBarCode, 0))
                        {
                            sSymID = sBarCode.Substring(0, 1);
                            sBarCode = sBarCode.Substring(1);
                            nCodeID = SetBarcodeType(sSymID);
                        }
                        else
                        {
                            nCodeID = (nMaxLen > 13) ? BCId.Code128 : BCId.EAN13;
                        }

                        ba = new BarcodeScannerEventArgs(nCodeID, sBarCode);
                        OnBarcodeScan(ba);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }

            return;
        }


        protected override BCId SetBarcodeType(string sT)
        {
            BCId ret = BCId.NoData;

            switch (sT)
            {
                case "A":
                    ret = BCId.EAN13;
                    break;
                case "B":
                    ret = BCId.Code39;
                    break;
                case "C":
                    ret = BCId.Codabar;
                    break;
                case "D":
                case "K":
                    ret = BCId.Code128;
                    break;
                case "E":
                    ret = BCId.Code93;
                    break;
                case "F":
                    ret = BCId.Interleaved25;
                    break;
                default:
                    break;
            }
            return (ret);
        }


        public ManagedLXEAPI.Scanner HX2Scan
        {
            get { return m_LocalScan; }
        }

        public override void Start()
        {
            //HX2Scan.ScannerStart();
        }
        public override void Stop()
        {
            //HX2Scan.ScannerStop();
        }
        public override void Terminate()
        {
        }
        public override void Dispose(bool disposing)
        {
            if (disposing)
            {
            }
        }


    }





}

#endif