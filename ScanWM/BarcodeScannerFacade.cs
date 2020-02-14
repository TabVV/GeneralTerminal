using System;
using System.Collections.Generic;
using System.Net;
using System.Windows.Forms;
using System.Drawing;
using System.Runtime.InteropServices;
using Microsoft.Win32;

/// Компилируется с одним из следующих символов:
/// DATALOGIC
/// SYMBOL
/// DOLPH7850
/// HWELL6100
/// HWELLHX2
/// PSC

namespace ScannerAll
{
    // Тип используемого терминала
    public enum TERM_TYPE : int
    {
        PSC4220     = 1,
        PSC4410     = 2,
        SYMBOL      = 10,
        DL_JET      = 20,
        DL_SCORP    = 24,
        DOLPH7850   = 30,
        HWELL6100   = 31,
        DOLPH9950   = 35,
        HWELLHX2    = 36,
        NRDMERLIN   = 41,
        NRDMORPHIC  = 42,
        UNKNOWN     = 333
    }
    // Тип считывателя
    public enum SCAN_TYPE : int
    {
        LASER = 0,
        IMAGER = 1
    }
    // Типы штрих-кодов
    // (из PSC-констант)
    public enum BCId : int{
        NoData          = 0, 
        Code39          = 1,
        Standard25      = 2,
        Matrix25        = 3,
        Interleaved25   = 4,
        Codabar         = 5,
        Ames            = 6,
        Code93          = 7,
        Code128         = 8,
        UPCA            = 9,
        UPCE            = 10,
        EAN13           = 11,
        EAN8            = 12,
        Code11          = 13,
        MSI             = 14,

        GS1DataBar      = 15,

        RFID            = 27,
        Image           = 28,
        OCR             = 33,
        DataMatrix      = 201,
        QR              = 220,
        PDF417          = 230,
        Aztec           = 240,

        Unknown         = 500
    }

    // делегат для обработки нажатий на ALP (для Symbol-48)
    public delegate bool ALPHandler(bool bCurAlp);


    public class WiFiStat
    {
        [Flags]
        public enum CONN_TYPE
        {
            NOCONNECTIONS,
            ACTIVESYNC,
            WIFI,
            BLUETOOTH,
            GSM
        }

        public class NetConn
        {
            public string IPAddr;
            public CONN_TYPE TConn = CONN_TYPE.NOCONNECTIONS;
            public bool IsActive;
        }

        protected static int MINRSS = -99;
        protected static int MAXRSS = -30;

        public static int
            SigRSSIdBm = 0,
            SigRSSIPercent = 0,
            SigQuality = 0,
            SigPercent = 0;
            //SigStrenght = 0;

        protected static int[] Threshold = { 15, 45 };

        protected static string sYesConn = "Подключен";
        protected static string sNOConn = "НЕТ сети";
        protected static string sESSID = "";
        protected static string sIP = "";
        protected static string sHost = "";
        protected static string sPathHW = @"HKEY_LOCAL_MACHINE\System\State\Hardware\";
        protected static string sMACAddr = "";

        private static bool m_Connected = false;
        public static System.Windows.Forms.Timer 
            tmWiFiShow = null;

        protected System.Windows.Forms.Control 
            pgbWiFiIndicator,                                   //ProgressBar
            xWiFiIndicatorParent = null;                        //Parent для ProgressBar

        public List<NetConn> xNetConn;

        private bool
            m_UseTimer4ShowWiFi = false;

        private int
            m_TimerInterval = 3000;

        public WiFiStat()
        {
            sHost = System.Net.Dns.GetHostName();
            xNetConn = new List<NetConn>();
            GetIP();
            sMACAddr = "000000000000";

            tmWiFiShow = new System.Windows.Forms.Timer();
            //tmWiFiShow.Tick += new EventHandler(Timer4WiFiProcessor);
            tmWiFiShow.Interval = m_TimerInterval;
            tmWiFiShow.Enabled = false;
        }


        /// флаг вывода индикатора WiFi по таймеру
        public virtual bool UseTimer4Show
        {
            get { return m_UseTimer4ShowWiFi; }
            set { m_UseTimer4ShowWiFi = value; }
        }

        // интервал обновления индикатора
        public virtual int RefreshInterval
        {
            get { return m_TimerInterval; }
            set { m_TimerInterval = value; }
        }



        public void SetTimer4WiFi(bool bUseTimer, int nInterval)
        {
            tmWiFiShow.Enabled = bUseTimer;
            UseTimer4Show = bUseTimer;
        }


        public virtual int RSS2Level(int MINdBm, int MAXdBm, int RSS)
        {
            int 
                nLevel = 0;

            if (MINdBm < MAXdBm)
            {
                if (RSS <= MINdBm)
                {
                    nLevel = 0;
                }
                else if (RSS >= MAXdBm)
                {
                    nLevel = 100;
                }
                else
                {
                    nLevel = (RSS - MINdBm) * 100 / (MAXdBm - MINdBm);
                }
            }
            else if (MINdBm > MAXdBm)
            {
                if (RSS >= MINdBm)
                {
                    nLevel = 0;
                }
                else if (RSS <= MAXdBm)
                {
                    nLevel = 100;
                }
                else
                {
                    nLevel = (RSS - MINdBm) * 100 / (MAXdBm - MINdBm);
                }
            }
            else
            {
                nLevel = 0;
            }

            SignalPercent = nLevel;
            return nLevel;
        }


        public virtual bool IsShownState
        {
            get { return (pgbWiFiIndicator == null)?false:pgbWiFiIndicator.Visible; }
            //set
            //{
            //    if (pgbWiFiIndicator is Control)
            //        pgbWiFiIndicator.Visible = value;
            //}
        }

        // сигнал в %
        public virtual int SignalPercent
        {
            get { return SigPercent; }
            set { SigPercent = value;}
        }

        // сигнал в единицах
        public virtual int SignalQuality
        {
            get { return SigQuality; }
            set { SigQuality = value; }
        }

        // имя точки
        public virtual string SSID
        {
            get { return sESSID; }
            set { sESSID = value; }
        }
        
        // контрол для отображения уровня
        public virtual System.Windows.Forms.Control SigTB
        {
            get { return pgbWiFiIndicator; }
            set { pgbWiFiIndicator = value; }
        }


        protected static bool GetIP()
        {
            string s = "No IP ";
            m_Connected = false;
            System.Net.IPHostEntry ipList = System.Net.Dns.GetHostEntry(sHost);
            foreach (System.Net.IPAddress ip in ipList.AddressList)
            {
                if (System.Net.IPAddress.IsLoopback(ip) == false)
                {
                    m_Connected = true;
                    s = ip.ToString();
                    break;
                }
            }
            sIP = (m_Connected == true) ? s : "";
            return (m_Connected);
        }

        public virtual CONN_TYPE ConnectionType()
        {
            byte[] ipAddr;
            System.Net.IPAddress ipA;
            string sPathASync = @"HKEY_LOCAL_MACHINE\Comm\Tcpip\Hosts\ppp_peer";
            string[] aAdNames = { "WiFi" };
            CONN_TYPE eCType = CONN_TYPE.NOCONNECTIONS;

            //RegistryKey rk = Registry.LocalMachine;
            /*
                        try
                        {
                            ipList = System.Net.Dns.GetHostEntry("PPP_PEER");
                            eCType = CONN_TYPE.ACTIVESYNC;
                        }
                        catch
                        {
                            ipList = new System.Net.IPHostEntry();

                        }
                        if ((ipList.AddressList != null)&&(ipList.AddressList.Length > 0))
                            ret = ipList.AddressList[0].ToString();

            */
            if (GetIP())
            {
                ipAddr = (byte[])Registry.GetValue(sPathASync, "ipaddr", new byte[] { });
                if ((ipAddr != null) && (ipAddr.Length == 4))
                {// ASync present
                    ipA = new System.Net.IPAddress(ipAddr);
                    eCType = CONN_TYPE.ACTIVESYNC;
                }
                else
                {
                    eCType = CONN_TYPE.WIFI;
                }
            }
            return (eCType);
        }

        public virtual bool GetIPList()
        {
            byte[] ipASync;
            IPAddress ipAS;
            NetConn xConn;
            string sPathASync = @"HKEY_LOCAL_MACHINE\Comm\Tcpip\Hosts\ppp_peer";

            m_Connected = false;

            ipASync = (byte[])Registry.GetValue(sPathASync, "ipaddr", new byte[] { });
            if ((ipASync != null) && (ipASync.Length == 4))
            {// ASync present
                ipAS = new System.Net.IPAddress(ipASync);
            }
            else
                ipAS = null;

            xNetConn = new List<NetConn>();
            System.Net.IPHostEntry ipList = System.Net.Dns.GetHostEntry(sHost);
            foreach (System.Net.IPAddress ip in ipList.AddressList)
            {
                if (System.Net.IPAddress.IsLoopback(ip) == false)
                {
                    m_Connected = true;
                    xConn = new NetConn();
                    xConn.IPAddr = ip.ToString();
                    if (ipAS != null)
                    {
                        xConn.TConn = CONN_TYPE.ACTIVESYNC;
                        byte[] bA = ip.GetAddressBytes();
                        for (int i = 0; i < 3; i++)
                            if (bA[i] != ipASync[i])
                            {
                                xConn.TConn = CONN_TYPE.NOCONNECTIONS;
                                break;
                            }
                    }
                    if (xConn.TConn == CONN_TYPE.NOCONNECTIONS)
                    {// это не ActiveSync
                        xConn.TConn = CONN_TYPE.WIFI;
                    }
                    xNetConn.Add(xConn);
                }
            }
            if (m_Connected)
            {
                sIP = xNetConn[0].IPAddr;
            }
            else
            {
                sIP = "";
            }
            return (m_Connected);
        }

        //protected void ConfigTextBox(int LocX, int LocY, int SizeX, int SizeY)
        public void ConfigTextBox(int LocX, int LocY, int SizeX, int SizeY)
        {
            pgbWiFiIndicator.Height = SizeY;
            pgbWiFiIndicator.Width = SizeX;
            pgbWiFiIndicator.Location = new System.Drawing.Point(LocX, LocY);
            ConfigFont(8, FontStyle.Regular);
        }
        protected void ConfigTextBox()
        {
            //STB.Height = 23;
            //STB.Width = 200;
            //STB.Location = new System.Drawing.Point(14, 96);
            //ConfigTextBox(31, 25, 164, 13);
            ConfigTextBox(31, 35, 164, 18);
        }
        protected void ConfigFont(Single s, FontStyle fs)
        {
            pgbWiFiIndicator.Font = new Font(pgbWiFiIndicator.Font.Name, s, fs);
        }

        public virtual void ShowWiFi(System.Windows.Forms.Control cP)
        {
            ShowWiFi(cP, true);
        }

        public virtual void ShowWiFi(System.Windows.Forms.Control cP, bool ShowNow)
        {
            if (pgbWiFiIndicator != null)
            {
                if (xWiFiIndicatorParent == null)
                {
                    xWiFiIndicatorParent = cP;
                    xWiFiIndicatorParent.Controls.Add(pgbWiFiIndicator);
                }
                pgbWiFiIndicator.Visible = ShowNow;
                if (UseTimer4Show)
                    tmWiFiShow.Enabled = ShowNow;
            }
            //IsEnabled = ShowNow;
        }

        public virtual bool ResetWiFi(int nReg)
        {
            return(ResetWiFi(nReg, xWiFiIndicatorParent));
        }

        public virtual bool ResetWiFi(int nReg, Control xP)
        {
            bool ret = true;
            //MessageBox.Show("Main Reset");
            return (ret);
        }

        public virtual string  WiFiInfo()
        {
            string 
                ret = "";
            GetIPList();
            foreach (NetConn xc in xNetConn)
            {
                ret += "\r\n" + xc.IPAddr + " - " + xc.TConn.ToString();
            }
            return (ret);
        }

        public virtual string IPCurrent
        {
            get{ return sIP;}
        }

        public virtual string MACAddreess
        {
            get { return sMACAddr; }
            set { sMACAddr = value; }
        }


        public class CustPB : Control
        {
            private WiFiStat 
                xWiFi = null;

            public static Color cBackColor = Color.LightGray;

            // высокий уровень
            public static Color cBarColorH = Color.LimeGreen;
            // средний уровень
            public static Color cBarColorM = Color.Gold;
            // высокий уровень
            public static Color cBarColorL = Color.Red;

            public static Color cTextColor = Color.Navy;

            //protected override void OnPaint(PaintEventArgs e)
            //{
            //    // Call the OnPaint method of the base class.
            //    base.OnPaint(e);
            //    // Call methods of the System.Drawing.Graphics object.
            //    e.Graphics.DrawString(Text, Font, new SolidBrush(ForeColor), ClientRectangle);
            //}
            //public delegate void OnPaint(PaintEventArgs e);
            //public EventHandler 

            public CustPB(){}

            public CustPB(WiFiStat x, string sN)
                : base()
            {
                this.Name = sN;
                this.Location = new System.Drawing.Point(9, 95);
                this.Size = new System.Drawing.Size(100, 23);
                this.BackColor = cBackColor;
                this.Font = new Font(this.Font.Name, this.Font.Size, FontStyle.Bold);
                this.Visible = true;

                xWiFi = x;
            }
            //static int nIinSyms = 0;
            //static string[] saSyms = { @"-", @"/", @"|", @"\"};
            protected override void OnPaint(PaintEventArgs e)
            {
                int nW;
                string s;

                base.OnPaint(e);
                Color cB = (xWiFi.SignalPercent <= WiFiStat.Threshold[0]) ? cBarColorL :
                           (xWiFi.SignalPercent <= WiFiStat.Threshold[1]) ? cBarColorM : cBarColorH;

                //string s = (xWiFi.SignalPercent >= WiFiStat.Threshold) ? WiFiStat.sYesConn : WiFiStat.sNOConn;
                s = (WiFiStat.sIP.Length > 0) ? WiFiStat.sYesConn : WiFiStat.sNOConn;
                //s += "(" + WiFiStat.sESSID + ")";

                //nIinSyms = (nIinSyms == 3) ? 0 : nIinSyms + 1;
                //s += "( " + xWiFi.SignalPercent.ToString() + "% " + saSyms[nIinSyms] + " )";

                s += " ( " + xWiFi.SignalPercent.ToString() + "%)";

                e.Graphics.FillRectangle(new SolidBrush(cBackColor), 0, 0,
                                         this.Width, this.Height);
                // полоска сигнала
                nW = (int)((((float)xWiFi.SignalPercent) / 100) * this.Width);
                e.Graphics.FillRectangle(new SolidBrush(cB), 0, 0,
                                         nW, this.Height);
                // текст статуса соединения
                e.Graphics.DrawString(s, this.Font, new SolidBrush(cTextColor),
                    (Width / 2 - (e.Graphics.MeasureString(s, Font).Width / 2.0F)),
                        Height / 2 - (e.Graphics.MeasureString(s, Font).Height / 2.0F));
                // граница контрола
                e.Graphics.DrawRectangle(new Pen(Color.Olive), 0, 0, Width - 1, Height - 1);
            }


        }



    }






	/// <summary>
	/// This is the barcode scanner facade class that return a generic barcode scanner object.
	/// </summary>
	public class BarcodeScannerFacade
	{
        // Для определения производителя
        [DllImport("coredll.dll", SetLastError = true)]
        public extern static int SystemParametersInfo(int Act, int Pars, string addr, int WinIni);
        private const int SPI_GETOEMINFO = 258;

        /// <summary>
		/// Creates and returns a generic (device independent) barcode scanner object.
		/// </summary>
		/// <returns>Generic barcode scanner object</returns>
		public static BarcodeScanner GetBarcodeScanner(System.Windows.Forms.Control ctlInvoker)
		{
			BarcodeScannerFactory BarcodeScannerFactory = null;
            BarcodeScanner BarcodeScanner = null;

            string sOEM = new string(' ', 50);

            int result = SystemParametersInfo(SPI_GETOEMINFO, 50, sOEM, 0);
            if (result != 0)
                sOEM = sOEM.Substring(0, sOEM.IndexOf('\0')).ToUpper();

			// Is this a Symbol device?
            if ((sOEM.IndexOf("SYMBOL") > -1) || (sOEM.IndexOf("MOTOROLA") > -1))
            {
#if SYMBOL
                BarcodeScannerFactory = new Symbol.SymbolBarcodeScannerFactory();
#endif
            }
            else if (sOEM.IndexOf("HAND HELD") > -1)
            {
#if DOLPH7850
                BarcodeScannerFactory = new Dolphin.DolphinBarcodeScannerFactory();
#endif
            }
            else if (sOEM.IndexOf("HONEYWELL") > -1)
            {
#if DOLPH9950
                BarcodeScannerFactory = new Honeywell.HoneyBarcodeScannerFactory();
#elif HWELL6100
                BarcodeScannerFactory = new Honeywell.HWell6100Factory();
#endif
            }
            else if (sOEM.IndexOf("DATALOGIC") > -1)
            {
#if DATALOGIC
                BarcodeScannerFactory = new DL.DLBarcodeScannerFactory(ctlInvoker);
#endif
            }
            else if (sOEM.IndexOf("HX2") > -1)
            {
#if HWELLHX2
                BarcodeScannerFactory = new Honeywell.HWellHX2Fact();
#endif
            }
            else if (sOEM.IndexOf("NORDIC") > -1)
            {
#if NRDMERLIN   
                BarcodeScannerFactory = new Nordic.NordicMerlinFactory( (sOEM.IndexOf("MORPHIC") > -1)?TERM_TYPE.NRDMORPHIC:TERM_TYPE.NRDMERLIN);
#endif
            }
            else
            {
#if PSC
                BarcodeScannerFactory = new PSC.PSCBarcodeScannerFactory(ctlInvoker);
#endif
            }
            
			// Конкретная модель определена
            if (BarcodeScannerFactory == null)
                BarcodeScannerFactory = new UnknownBarcodeScannerFactory();
            BarcodeScanner = BarcodeScannerFactory.GetBarcodeScanner(sOEM);
            BarcodeScanner.BCInvoker = ctlInvoker;

			return BarcodeScanner;
		}
	}

    /// <summary>
    /// This is the abstract barcode scanner class factory.
    /// </summary>
    public abstract class BarcodeScannerFactory
    {
        protected Control xControlInvoker = null;
        protected TERM_TYPE OEMInf = TERM_TYPE.UNKNOWN;
        //public abstract BarcodeScanner GetBarcodeScanner(Control xC);
        public abstract BarcodeScanner GetBarcodeScanner(string OEM);
    }

    // Для создания Неизвестный тип сканера
    public class UnknownBarcodeScannerFactory : BarcodeScannerFactory
    {
        //public override BarcodeScanner GetBarcodeScanner(Control xC)
        //{
        //    return new UnknownBarcodeScanner();
        //}
        public override BarcodeScanner GetBarcodeScanner(string OEM)
        {
            return new UnknownBarcodeScanner();
        }
    }

    // Неизвестный тип сканера
    public class UnknownBarcodeScanner : BarcodeScanner 
    {
        public override bool Initialize()
        {
            return true;
        }
        public override void Start()
        {
        }
        public override void Stop()
        {
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


    /// <summary>
    /// This is the abstract barcode scanner class.
    /// </summary>
    public abstract class BarcodeScanner : IDisposable
    {
        public TERM_TYPE nTermType = TERM_TYPE.UNKNOWN;
        public SCAN_TYPE nSCEngineType = SCAN_TYPE.LASER;
        public Dictionary<string, BCId> dicAIMs;

        public int nKeys = 0;
        public int nDesktopWidth;
        public int nDesktopHigh;
        public WiFiStat WiFi = new WiFiStat();

        private Control m_BCInvoker = null;

        public Control BCInvoker
        {
            get { return m_BCInvoker; }
            set { m_BCInvoker = value; }
        }

        public BarcodeScanner():this(null){}

        public BarcodeScanner(Control xCInv)
        {
            BCInvoker = xCInv;
            dicAIMs = new Dictionary<string, BCId>();
            dicAIMs.Add("E", BCId.EAN13);
            dicAIMs.Add("C", BCId.Code128);
            dicAIMs.Add("A", BCId.Code39);
            dicAIMs.Add("I", BCId.Interleaved25);
            dicAIMs.Add("F", BCId.Codabar);
            dicAIMs.Add("G", BCId.Code93);
            dicAIMs.Add("H", BCId.Code11);
            dicAIMs.Add("M", BCId.MSI);

            if (Initialize())
            {
                switch (this.nTermType)
                {
                    case TERM_TYPE.DOLPH9950:
                        break;
                    case TERM_TYPE.NRDMERLIN:
                        break;
                    default:
                        this.Start();
                        break;
                }
            }
        }

        // Methods that need to be implemented in subclass.
        public abstract bool Initialize();
        public abstract void Start();
        public abstract void Stop();
        public abstract void Terminate();

        // Event delegate and handler
        public delegate void BarcodeScanEventHandler(object sender, BarcodeScannerEventArgs e);
        public event BarcodeScanEventHandler BarcodeScan;

        /// <summary>
        /// Event that calls the delegate.
        /// </summary>
        protected virtual void OnBarcodeScan(BarcodeScannerEventArgs e)
        {
            if (BarcodeScan != null)
            {
                // Invokes the delegate. 
                if ((BCInvoker != null) && BCInvoker.InvokeRequired)
                {
                    //object[] xArgs = new object[]{this, e};
                    BCInvoker.Invoke(new BarcodeScanEventHandler(BarcodeScan), new object[] { this, e });
                    //BCInvoker.Invoke(new BarcodeScanEventHandler(BarcodeScan), xArgs);
                }
                else
                    BarcodeScan(this, e);
            }
        }

        // приведение типа штрихкода к своему стандарту
        // для терминалов Honeywell
        protected virtual BCId SetBarcodeType(string sT)
        {
            BCId ret = BCId.NoData;

            switch (sT)
            {
                case "d":
                case "D":
                    ret = BCId.EAN13;
                    break;
                case "e":
                    ret = BCId.Interleaved25;
                    break;
                case "I":
                case "j":
                    ret = BCId.Code128;
                    break;
                case "w":
                    ret = BCId.DataMatrix;
                    break;
                default:
                    break;
            }
            return (ret);
        }

        [DllImport("touch.dll", SetLastError = true)]
        public extern static void TouchPanelDisable();
        [DllImport("touch.dll", SetLastError = true)]
        public extern static bool TouchPanelEnable(IntPtr CallBackFunc);

        public virtual bool TouchScr(bool bEnable)
        {
            bool bRet = true;
            if (!bEnable)
            {// отключение TouchScreen
                TouchPanelDisable();
            }
            else
            {// включение TouchScreen
            }
            return (bRet);
        }

        public virtual bool TouchState(out bool bCurState)
        {
            bool bRet = true;
            bCurState = true;
            return (bRet);
        }

        public abstract void Dispose(bool disposing);

        public virtual void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~BarcodeScanner()
        {
            Dispose(false);
        }

    }

    /// <summary>
    /// This is the event arguments for the Barcode Scanner class event BarcodeScan.
    /// </summary>
    public class BarcodeScannerEventArgs : EventArgs
    {

        private BCId nBCId;
        private string data = "";
        private byte[] binData = null;
        private int nBinLen = 0;

        private DateTime
            m_ScanDT = DateTime.Now;

        public BarcodeScannerEventArgs(BCId nBarcodeID, string s, byte[] bData, int nLen)
        {
            this.nBCId   = nBarcodeID;
            this.binData = bData;
            this.nBinLen = nLen;
            this.data = s;
        }

        public BarcodeScannerEventArgs(BCId nBarcodeID, string data):this(nBarcodeID, data, null, 0){}

        // тип прочитанного штрихкода
        public BCId nID
        {
            get { return nBCId; }
        }

        // строка с прочитанным штрихкодом
        public string Data
        {
            get { return data; }
        }

        // массив прочитанных байт
        public byte[] BinData
        {
            get { return binData; }
        }

        // размер массива прочитанных байт
        public int LenBinData
        {
            get { return nBinLen; }
        }

        // дата-время сканирования
        public DateTime ScanDTime
        {
            get { return m_ScanDT; }
        }


    }

}