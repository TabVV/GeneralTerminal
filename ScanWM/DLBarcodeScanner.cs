using System;
using System.Drawing;
using System.Windows.Forms;
using System.Threading;
using System.Runtime.InteropServices;
using System.Reflection;
using Microsoft.Win32;

#if DATALOGIC


using datalogic.pdc;
using datalogic.datacapture;
using Datalogic.API;

namespace ScannerAll.DL
{

    public class DLBarcodeScannerFactory : BarcodeScannerFactory
    {
        public static string
            DL_OEM = "";
        public DLBarcodeScannerFactory(Control xC)
        {
            base.xControlInvoker = xC;
            // Creates a new instance of the DLScannerSetup class
            datalogic.pdc.DLScannerSetup scannerSetup = new datalogic.pdc.DLScannerSetup();
            datalogic.pdc.DLScanEngineClassID sID = scannerSetup.getReaderIdentifier();
            if (sID == datalogic.pdc.DLScanEngineClassID.SE_READER_BCD_CLASS_ID)
            {// линейный сканер
                base.OEMInf = TERM_TYPE.DL_SCORP;
            }
            else if (sID == datalogic.pdc.DLScanEngineClassID.SE_READER_IMAGER_CLASS_ID)
            {// это Imager
                base.OEMInf = TERM_TYPE.DL_JET;
            }
        }

        public override BarcodeScanner GetBarcodeScanner(string OEM)
        {
            DL_OEM = OEM;
            return (new DLBarcodeScanner(OEM));
        }
    }

	/// <summary>
	/// This is the barcode scanner class for DataLogics devices.
	/// </summary>
	public class DLBarcodeScanner : BarcodeScanner 
	{
        private 
            byte[] baDat;
        private string
            sOEM = "DATALOGIC";
        public static bool
            bIsX4 = false;

        private DLScanEngineClassID 
            scID;
        private ScannerEngine 
            scEng = null;
        //private DecodeHandle
        //    hDcd = null;
        //private DecodeEvent
        //    dcdEvent;

        public class DLWiFi : WiFiStat
        {
            private const int MINRSS = -99;
            private const int MAXRSS = -30;


            private DLRsignal 
                rsWF = null;
            private datalogic.wireless.WirelessParams 
                xWiFiPars = null;
            private datalogic.wireless.RadioBase
                xRBase = null;

            private class DLRsignal : datalogic.wireless.RadioSignal
            {
                //public System.Windows.Forms.Control cB;
                //private Font ftSConn;
                public DLRsignal()
                    : base()
                {
                    this.BackColor = CustPB.cBackColor;
                    this.FillColor = CustPB.cBarColorH;
                    this.LowLevelColor = CustPB.cBarColorL;
                    WiFiStat.Threshold[0] = 25;
                    this.Threshold = WiFiStat.Threshold[1];
                    //this.Font = new Font(this.Font.Name, this.Font.Size, FontStyle.Bold);
                    //this.Font = new Font(this.Font.Name, 8.0F, FontStyle.Bold);
                    this.TextInside = false;
                    this.Enabled = false;
                    this.Visible = false;
                    this.Active = false;

                    //cB = (System.Windows.Forms.Control)this;
                }

                protected override void OnPaint(PaintEventArgs e)
                {
                    int
                        nSigPercent = base.GetSignalLevel();

                    if (nSigPercent <= WiFiStat.Threshold[0])
                    {
                        this.Threshold = WiFiStat.Threshold[0];
                        this.LowLevelColor = CustPB.cBarColorL;
                        this.FillColor = CustPB.cBarColorL;
                    }
                    else if (nSigPercent <= WiFiStat.Threshold[1])
                    {
                        this.Threshold = WiFiStat.Threshold[1];
                        this.LowLevelColor = CustPB.cBarColorM;
                        this.FillColor = CustPB.cBarColorM;
                    }
                    else
                    {
                        this.Threshold = 100;
                        this.LowLevelColor = CustPB.cBarColorH;
                        this.FillColor = CustPB.cBarColorH;
                    }

                    base.OnPaint(e);
                    //string s = (this.IsAssociated() == true) ? WiFiStat.sYesConn : WiFiStat.sNOConn;
                    WiFiStat.GetIP();
                    string s = (WiFiStat.sIP.Length > 0) ? WiFiStat.sYesConn : WiFiStat.sNOConn;
                    s += " (" + nSigPercent.ToString() + "%)";

                    e.Graphics.DrawString(s, this.Font, new SolidBrush(CustPB.cTextColor),
                        (Width / 2 - (e.Graphics.MeasureString(s, Font).Width / 2.0F)),
                            Height / 2 - (e.Graphics.MeasureString(s, Font).Height / 2.0F));
                }

            }

            private class X4WiFiIndicator : CustPB
            {
                private WiFiStat
                    xDLWiFiInfo = null;

                public X4WiFiIndicator(WiFiStat x):base(x, "pgbDLWiFi")
                {
                    xDLWiFiInfo = x;
                    WiFiStat.Threshold[0] = 25;
                    this.Visible = false;               // первоначально индикатора нет
                }

                protected override void OnPaint(PaintEventArgs e)
                {

/*
                    if (nSigPercent <= WiFiStat.Threshold[0])
                    {
                        this.Threshold = WiFiStat.Threshold[0];
                        this.LowLevelColor = CustPB.cBarColorL;
                        this.FillColor = CustPB.cBarColorL;
                    }
                    else if (nSigPercent <= WiFiStat.Threshold[1])
                    {
                        this.Threshold = WiFiStat.Threshold[1];
                        this.LowLevelColor = CustPB.cBarColorM;
                        this.FillColor = CustPB.cBarColorM;
                    }
                    else
                    {
                        this.Threshold = 100;
                        this.LowLevelColor = CustPB.cBarColorH;
                        this.FillColor = CustPB.cBarColorH;
                    }
 */ 

                    base.OnPaint(e);
                    //string s = (this.IsAssociated() == true) ? WiFiStat.sYesConn : WiFiStat.sNOConn;

                    //WiFiStat.GetIP();
                    //string s = (WiFiStat.sIP.Length > 0) ? WiFiStat.sYesConn : WiFiStat.sNOConn;
                    //s += " (" + nSigPercent.ToString() + "%)";

                    //e.Graphics.DrawString(s, this.Font, new SolidBrush(CustPB.cTextColor),
                    //    (Width / 2 - (e.Graphics.MeasureString(s, Font).Width / 2.0F)),
                    //        Height / 2 - (e.Graphics.MeasureString(s, Font).Height / 2.0F));
                }

            }

            public DLWiFi():base()
            {
                UniWiFi();
                base.ConfigTextBox();
            }

            public DLWiFi(int nLX, int nLY, int nSX, int nSY):base()
            {
                UniWiFi();
                base.ConfigTextBox(nLX, nLY, nSX, nSY);
            }


            /// получить RSSI, Quality, SSID
            private bool WiFiPars()
            {
                bool 
                    wifiOn = false;
                uint
                    uQual = 0;
                if (!DLBarcodeScanner.bIsX4)
                {
                    wifiOn = this.rsWF.IsAssociated();
                    base.SignalPercent = this.rsWF.GetSignalLevel();
                    ushort
                        uLev = 0;
                    this.rsWF.GetSignal(ref uLev);
                    base.SignalQuality = uLev;
                    base.SSID = xWiFiPars.GetSSID();
                }
                else
                {
                    wifiOn = Device.GetWiFiPowerStatus();
                    if (wifiOn)
                    {
                        WiFiStatus status = Datalogic.API.WiFi.GetCurrentStatus();
                        base.SignalPercent = base.RSS2Level(MINRSS, MAXRSS, status.Rssi);
                        Datalogic.API.Device.WiFiGetSignalQuality(out uQual);
                        base.SignalQuality = (int)uQual;

                        //String active = Datalogic.API.WiFi.GetActiveConfig();
                        //if (active != "")
                        //{
                        //    StandardWiFiConfig cfg = Datalogic.API.WiFi.GetConfig(active);
                        //    base.SSID = cfg.Base.SSID;
                        //}
                        base.SSID = "";
                    }
                }

                return (wifiOn);
            }


            private void UniWiFi()
            {
                byte[] 
                    macAddress = new byte[13];
                string
                    s = "";

                if (!DLBarcodeScanner.bIsX4)
                {
                    xWiFiPars = new datalogic.wireless.WirelessParams();
                    rsWF = new DLRsignal();
                    xRBase = new datalogic.wireless.RadioBase();

                    rsWF.Active = false;
                    if (xWiFiPars.GetMACAddress(out s))
                    {
                        sMACAddr = "";
                        string[] sA = s.Split(new char[] { ':' });
                        foreach (string ss in sA)
                            sMACAddr += ss;
                    }
                    base.pgbWiFiIndicator = (System.Windows.Forms.Control)rsWF;
                }
                else
                {
                    Device.WiFiGetMacAddress(out macAddress);
                    for (int i = 0; i < macAddress.Length; i++)
                    {
                        s += macAddress[i].ToString("X2");
                    }
                    sMACAddr = s;

                    base.UseTimer4Show = true;
                    base.RefreshInterval = 2000;
                    base.pgbWiFiIndicator = new X4WiFiIndicator(this);
                }
                WiFiPars();

                base.ConfigFont(8.0F, FontStyle.Regular);
            }

            /// получить RSSI, SSID
            public override string WiFiInfo()
            {
                WiFiPars();
                return (base.WiFiInfo());
            }



            //public override bool IsEnabled
            //{
            //    get { return base.pgbWiFiIndicator.Visible; }
            //    set 
            //    { 
            //        base.pgbWiFiIndicator.Visible = value;
            //        //base.SetTimer4WiFi(value, 2000);
            //        if (value == true)
            //        {
            //            WiFiStat.tmWiFiShow.Tick += new EventHandler(tmWiFiShow_Tick);
            //            WiFiStat.tmWiFiShow.Enabled = true;
            //        }
            //        else
            //        {
            //            if (WiFiStat.tmWiFiShow.Enabled)
            //            {
            //                WiFiStat.tmWiFiShow.Tick -= tmWiFiShow_Tick;
            //                WiFiStat.tmWiFiShow.Enabled = false;
            //            }
            //        }
            //    }
            //}


            // отобразить/скрыть индикатор уровня
            public override void ShowWiFi(System.Windows.Forms.Control cP, bool ShowNow)
            {

                if (!DLBarcodeScanner.bIsX4)
                {
                    rsWF.Active = ShowNow;
                }
                else
                {
                    if (ShowNow)
                    {
                        if (!WiFiStat.tmWiFiShow.Enabled)
                        {
                            WiFiStat.tmWiFiShow.Tick += new EventHandler(tmWiFiShow_Tick);
                        }
                    }
                    else
                    {
                        if (WiFiStat.tmWiFiShow.Enabled)
                        {
                            WiFiStat.tmWiFiShow.Tick -= tmWiFiShow_Tick;
                        }
                    }
                }
                base.ShowWiFi(cP, ShowNow);
            }



            void tmWiFiShow_Tick(object sender, EventArgs e)
            {
                int
                    nSigdBm = 0;

                if (Device.WiFiGetRssi(out nSigdBm))
                {
                    WiFiStat.SigRSSIdBm = nSigdBm;
                }

                base.RSS2Level(MINRSS, MAXRSS, nSigdBm);

                base.pgbWiFiIndicator.Refresh();
            }





            [DllImport("dl_api.dll", CharSet = CharSet.Auto, SetLastError = true)]
            public static extern void DLWIFI_Enable(bool Enable);
            [DllImport("dl_api.dll", CharSet = CharSet.Auto, SetLastError = true)]
            public static extern bool DLWIFI_IsEnabled();



            // nReg = 0 - Disable WiFi
            // nReg = 1 - Enable WiFi
            // nReg = 2 - Reset WiFi
            public override bool ResetWiFi(int nReg)
            {
                return (ResetWiFi(nReg, xWiFiIndicatorParent));
            }

            public override bool ResetWiFi(int nReg, Control xP)
            {
                bool 
                    ret = true,
                    bOldState = IsShownState;

                ShowWiFi(xP, false);
                Application.DoEvents();
                if (!DLBarcodeScanner.bIsX4)
                {
                    ret = DLWIFI_IsEnabled();
                    if (nReg != 1)
                    {// выключаем, если не требуется принудительное включение
                        DLWIFI_Enable(false);
                        Thread.Sleep(6000);
                        ret = true;
                    }
                    if (nReg >= 1)
                    {// включаем, если не требуется принудительное выключение
                        DLWIFI_Enable(true);
                        Thread.Sleep(4000);
                        ret = DLWIFI_IsEnabled();
                    }
                }
                else
                {
                    //ShowWiFi(null, false);
                    try
                    {
                        if (nReg != 1)
                        {// выключаем, если не требуется принудительное включение

                            // пока не работает
                            //ret = Device.SetWiFiPowerState(false);
                            //Thread.Sleep(3000);
                            //ret = true;
                        }
                        if (nReg >= 1)
                        {// включаем, если не требуется принудительное выключение

                            // 14.09.18
                            //ret = Device.SetWiFiPowerState(true);
                            //Thread.Sleep(6000);
                            //ret = Device.GetWiFiPowerStatus();

                            ret = true;
                        }
                    }
                    catch { }
                    finally
                    {
                        //ShowWiFi(null, bOldState);
                    }
                }
                ShowWiFi(xP, bOldState);
                Application.DoEvents();
                return (ret);
            }

            public bool DLWiFiState()
            {
                return( DLWIFI_IsEnabled());
            }
        }

        public DLBarcodeScanner(string OEM)
        {
            sOEM = OEM;
            //bIsX4 = (sOEM.IndexOf("X4") > 0) ? true : false;
            // WiFi Init
            base.WiFi = new DLWiFi(30, 22, 164, 18);
        }

		/// <summary>
		/// Инициализация PSC сканера
		/// </summary>
		/// <returns>Whether initialization was successful</returns>
		public override bool Initialize() 
		{
            //if (bIsX4)
            bIsX4 = (DLBarcodeScannerFactory.DL_OEM.IndexOf("X4") > 0) ? true : false;

            if (bIsX4)
            {
                //try
                //{
                //    hDcd = new DecodeHandle(DecodeDeviceCap.Exists | DecodeDeviceCap.Barcode);
                //}
                //catch (DecodeException)
                //{
                //    //MessageBox.Show("Exception loading barcode decoder.", "Decoder Error");
                //    //return false;
                //}
                //DecodeRequest reqType = (DecodeRequest)1 | DecodeRequest.PostRecurring;

                //dcdEvent = new DecodeEvent(hDcd, reqType, null);
                //dcdEvent.Scanned += new DecodeScanned(dcdEvent_Scanned_X4);
            }

                if (scID == DLScanEngineClassID.SE_READER_IMAGER_CLASS_ID)
                {
                    scEng = new datalogic.datacapture.Imager();
                    ((Imager)scEng).ImgType = datalogic.datacapture.Imager.ImageType.JPEG;
                    base.nSCEngineType = SCAN_TYPE.IMAGER;
                    base.nTermType = TERM_TYPE.DL_JET;
                }
                else
                {
                    scEng = new datalogic.datacapture.Laser();
                    base.nSCEngineType = SCAN_TYPE.LASER;
                    base.nTermType = TERM_TYPE.DL_SCORP;
                }

                scEng.GoodReadEvent += new datalogic.datacapture.ScannerEngine.LaserEventHandler(dcdEvent_Scanned);
                scEng.ScannerEnabled = true;
			return true;
		}

		/// <summary>
		/// Start a Intermec scan (not needed).
		/// </summary>
		public override void Start() 
		{
		}

		/// <summary>
		/// Stop a Intermec scan (not needed).
		/// </summary>
		public override void Stop() 
		{
		}

		/// <summary>
		/// Terminates the Intermec scanner.
		/// </summary>
		public override void Terminate()
		{
            if (bIsX4)
            {
                //if (dcdEvent.IsListening)
                //{
                //    dcdEvent.StopScanListener();
                //}

                //if (hDcd != null)
                //{
                //    hDcd.Dispose();
                //}
            }
            if (scEng != null)
            {
                scEng.ReleaseTrigger();

                // Коммент от 21.07.15 для совместной работы с AtlasWMS
                //scEng.ScannerEnabled = false;

                scEng.DeInit();
                scEng = null;
            }
        }

        public void SoftStart()
        {
            scEng.ScannerEnabled = true;
            scEng.PressTrigger();
        }



        //private void dcdEvent_Scanned_X4(object sender, DecodeEventArgs e)
        //{
        //    CodeId cID = CodeId.NoData;
        //    string dcdData = string.Empty;
        //    byte[]
        //        byteData;
        //    BCId nCodeID  = BCId.NoData;

        //    // Obtain the string and code id.
        //    try
        //    {
        //        dcdData = hDcd.ReadString(e.RequestID, ref cID);
        //    }
        //    catch (Exception)
        //    {
        //        MessageBox.Show("Error reading string!");
        //        return;
        //    }

        //        switch (cID)
        //        {
        //            case CodeId.EAN13:
        //            case CodeId.EAN8:
        //                nCodeID = BCId.EAN13;
        //                break;
        //            case CodeId.Code128:
        //                nCodeID = BCId.Code128;
        //                break;
        //            case CodeId.Interleaved25:
        //                nCodeID = BCId.Interleaved25;
        //                break;
        //            case CodeId.Code39:
        //                nCodeID = BCId.Code39;
        //                break;
        //            case CodeId.GS1Expanded:
        //                nCodeID = BCId.GS1DataBar;
        //                break;
        //            default:
        //                nCodeID = BCId.Unknown;
        //                break;
        //        }

        //    // убрать форматирование для AtlasWMS
        //    int nL = dcdData.Length;
        //            byteData = System.Text.Encoding.UTF8.GetBytes(dcdData);
        //    if (byteData.Length >= 5)
        //    {
        //        if ((byteData[0] == '\x2') &&                       // STX
        //            (byteData[byteData.Length - 1] == '\x3') &&     // ETX
        //            (byteData[1] == ']'))                           // AIM
        //        {
        //            dcdData = dcdData.Substring(4, nL - 5);
        //            dcdData = dcdData.Replace('$', '\x1D');       // $ -> FNC1
        //            byteData = System.Text.Encoding.UTF8.GetBytes(dcdData);
        //        }
        //    }
        //    OnBarcodeScan(new BarcodeScannerEventArgs(nCodeID, dcdData, byteData, byteData.Length));
        //}



		/// <summary>
		/// Event that fires when a PSC scanner has performed a scan.
		/// </summary>
        private void dcdEvent_Scanned(ScannerEngine sender)
        {
            BCId nCodeID  = BCId.NoData;
            string sBarCode = string.Empty;
            byte[] byteData = sender.BarcodeDataAsByteArray;

            // Obtain the string and code id.
            try
            {
                sBarCode = sender.BarcodeDataAsText;
                baDat = sender.BarcodeDataAsByteArray;
                string sAIM = sender.BarcodeTypeAsAIMIdentifier;

                BARCODE_Identifier bi = sender.BarcodeTypeAsIdentifier;
                switch (bi)
                {
                    case BARCODE_Identifier.BARCODE_ID_EAN_13:
                    case BARCODE_Identifier.BARCODE_ID_EAN_13_ADDON_2:
                    case BARCODE_Identifier.BARCODE_ID_EAN_13_ADDON_5:
                        nCodeID = BCId.EAN13;
                        break;
                    case BARCODE_Identifier.BARCODE_ID_CODE_128_STANDARD:
                        nCodeID = BCId.Code128;
                        break;
                    case BARCODE_Identifier.BARCODE_ID_CODE_EAN_128:
                        nCodeID = BCId.Code128;
                        break;
                    case BARCODE_Identifier.BARCODE_ID_CODE_ISBT_128:
                        nCodeID = BCId.Code128;
                        break;
                    case BARCODE_Identifier.BARCODE_ID_CODE_ISBT_128_CON:
                        nCodeID = BCId.Code128;
                        break;
                    case BARCODE_Identifier.BARCODE_ID_CODE_25_INTERLEAVED:
                        nCodeID = BCId.Interleaved25;
                        break;
                    case BARCODE_Identifier.BARCODE_ID_CODE_25_MATRIX:
                        nCodeID = BCId.Matrix25;
                        break;
                    case BARCODE_Identifier.BARCODE_ID_CODE_39_STANDARD:
                        nCodeID = BCId.Code39;
                        break;
                    case (BARCODE_Identifier)8804:
                        nCodeID = BCId.GS1DataBar;
                        break;
                    default:
                        nCodeID = BCId.Unknown;
                        break;
                }
            }
            catch (Exception)
            {
                //MessageBox.Show("Error reading string!");
            }

            //OnBarcodeScan(new BarcodeScannerEventArgs( nCodeID, sBarCode ));

            // убрать форматирование для AtlasWMS
            int nL = sBarCode.Length;
            if (byteData.Length >= 5)
            {
                if ((byteData[0] == '\x2') &&                       // STX
                    (byteData[byteData.Length - 1] == '\x3') &&     // ETX
                    (byteData[1] == ']'))                           // AIM
                {
                    sBarCode = sBarCode.Substring(4, nL - 5);
                    sBarCode = sBarCode.Replace('$', '\x1D');       // $ -> FNC1
                    byteData = System.Text.Encoding.UTF8.GetBytes(sBarCode);
                }
            }

            OnBarcodeScan(new BarcodeScannerEventArgs(nCodeID, sBarCode, byteData, byteData.Length));
            return;
        }

        [DllImport("coredll.dll", EntryPoint="GetDC", SetLastError = true)]
        static extern IntPtr GetDC(IntPtr hWnd);

        [DllImport("coredll.dll", EntryPoint = "ReleaseDC", SetLastError = true)]
        static extern int ReleaseDC(IntPtr hWnd, IntPtr hDC);

        [DllImport("coredll.dll", SetLastError = true)]
        static extern int ExtEscape (IntPtr hdc, uint nEscape, uint cbInput, byte[] lpszInData, int cbOutput, IntPtr lpszOutData);

        // ВКЛ/ВЫКЛ TouchScreen
        public override bool TouchScr(bool bEnable)
        {
            bool 
                bRet = true;
            if (sOEM.IndexOf("X") > -1)
            {
                try
                {
                    //Assembly xAs = Assembly.LoadFrom(@"\Windows\Datalogic.API.dll");
                    Assembly xAs = Assembly.LoadFrom(@"Datalogic.API.dll");
                    object xD = xAs.CreateInstance("Datalogic.API.Device");
                    MethodInfo xMI = xD.GetType().GetMethod("SetTouchScreenEnable");
                    object[] xPars = new object[] { bEnable };
                    bRet = (bool)xMI.Invoke(null, xPars);
                }
                catch
                {
                    bRet = false;
                }
                return (bRet);
            }

            //#define ESCAPECODEBASE          100000
            //#define TOUCH_PANEL_ENABLE      (ESCAPECODEBASE + 14)
            //#define TOUCH_PANEL_STATUS      (ESCAPECODEBASE + 15)
            int
                nRet;
            uint
                nEsc = (100000 + 14),
                cbInpt;
            IntPtr hScr = GetDC(System.IntPtr.Zero);
            if (hScr != IntPtr.Zero)
            {
                if (!bEnable)
                {// отключение TouchScreen
                    cbInpt = 0;
                }
                else
                {// включение TouchScreen
                    cbInpt = 1;
                }
                nRet = ExtEscape(hScr, nEsc, cbInpt, null, 0, System.IntPtr.Zero);
                if (nRet <= 0)
                    bRet = false;
                ReleaseDC(System.IntPtr.Zero, hScr);
            }
            return (bRet);
        }

        public override bool TouchState(out bool bCurState)
        {
            bool
                bRet = false;

            bCurState = false;
            if (sOEM.IndexOf("X") > -1)
            {
                try
                {
                    //Assembly xAs = Assembly.LoadFrom(@"\Windows\Datalogic.API.dll");
                    Assembly xAs = Assembly.LoadFrom(@"Datalogic.API.dll");
                    object xD = xAs.CreateInstance("Datalogic.API.Device");
                    MethodInfo xMI = xD.GetType().GetMethod("GetTouchScreenEnable");
                    object[] xPars = new object[] { bCurState };
                    bRet = (bool)xMI.Invoke(null, xPars);
                    if (bRet)
                        bCurState = (bool)xPars[0];
                }
                catch
                {
                    bRet = false;
                }
            }

            return (bRet);
        }










    #region IDisposable Members
    public override void Dispose(bool disposing)
    {
      if (disposing)
      {
        // Code to clean up managed resources
        Terminate();
      }
      // Code to clean up unmanaged resources
    }
    #endregion
  }
}

#endif