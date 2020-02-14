using System;
using System.Drawing;
using System.Windows.Forms;
using System.Threading;
using Microsoft.Win32;

#if HWELL6100

using Honeywell.DataCollection.Decoding;
using Honeywell.DataCollection.WinCE.Decoding;
using Honeywell.WinCE.Network.RF80211;
using Honeywell.WinCE.Network.RadioMgr;


namespace ScannerAll.Honeywell
{

    public class HWell6100Factory : BarcodeScannerFactory
    {
        public HWell6100Factory()
        {
            base.OEMInf = TERM_TYPE.HWELL6100;
        }
        public override BarcodeScanner GetBarcodeScanner(string OEM)
        {
            return (new HWell6100());
        }
    }

	/// <summary>
	/// This is the barcode scanner class for Dolphin devices.
	/// </summary>
    public class HWell6100 : BarcodeScanner 
	{
        //private HWellDec xEng;

        private DecodeControl xEng;

        private bool bMayScan = true;
        private DecodeBase.DecodeEventHandler dgDecEvH;

        public class HoneyWiFi : WiFiStat
        {
            private CustPB cSym;
            private bool m_Enabled;
            private System.Windows.Forms.Timer tmSig = null;
            private int m_SigPercent = 0,
                m_LinkQuality = 0;
            private bool m_Associated = false;

            private RFServices xRS;
            private RadioManager xRM;

            private void Init6100(int nLX, int nLY, int nSX, int nSY)
            {
                int nSize = 6;
                byte[] aMAC = new byte[6];
                cSym = new CustPB(this, "cSym");
                base.SigTB = cSym;
                //base.ConfigTextBox(32, 35, 164, 18);
                if ((nSX > 0) && (nSY > 0))
                    base.ConfigTextBox(nLX, nLY, nSX, nSY);
                else
                    base.ConfigTextBox();

                xRS = new RFServices();
                xRM = new RadioManager();
                if (RFServices.GetRFAdapterMACAddress(aMAC, ref nSize))
                {

                    sMACAddr = "";
                    for (int i = 0; i < 6; i++)
                        sMACAddr += String.Format("{0:X2}", aMAC[i]);
                }
                else
                    sMACAddr = "000000000000";

                tmSig = new System.Windows.Forms.Timer();
                tmSig.Enabled = false;
                tmSig.Interval = 500;
                tmSig.Tick += new EventHandler(tmSig_Tick);
            }

            public HoneyWiFi()
                : base()
            {
                Init6100(0,0,0,0);
                //cSym = new CustPB(this, "cSym");
                //base.STB = cSym;
                ////base.ConfigTextBox(32, 35, 164, 18);
                //base.ConfigTextBox(30, 22, 164, 18);

                //xRS = new RFServices();
                //xRM = new RadioManager();

                //tmSig = new System.Windows.Forms.Timer();
                //tmSig.Enabled = false;
                //tmSig.Interval = 500;
                //tmSig.Tick += new EventHandler(tmSig_Tick);
            }
            public HoneyWiFi(int nLX, int nLY, int nSX, int nSY)
                : base()
            {
                Init6100(nLX, nLY, nSX, nSY);
            }




            public void tmSig_Tick(object sender, EventArgs e)
            {
                RFServices.GetRFAdapterLinkStatus(ref m_LinkQuality, ref m_SigPercent, ref m_Associated);
                base.SignalPercent = m_SigPercent;
                //base.SigStrenght = m_LinkQuality;
                WiFiStat.GetIP();

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
            //        m_Enabled = value; }
            //}


            // nReg = 0 - Disable WiFi
            // nReg = 1 - Enable WiFi
            // nReg = 2 - Reset WiFi
            public override bool ResetWiFi(int nReg)
            {
                bool ret = false;

                if (nReg != 1)
                {
                        ret = xRM.Enable80211Radio(false);
                        Thread.Sleep(2000);
                }
                if (nReg >= 1)
                {
                    ret = xRM.Enable80211Radio(true);
                    Thread.Sleep(2000);
                    if ((ret) && (nReg == 2))
                    {//Wi-Fi доступен...
                        if (ConnectionType() == WiFiStat.CONN_TYPE.NOCONNECTIONS)
                        {
                            int i = 0,
                                iMax = 10;
                            ret = false;
                            i = 0; iMax = 10;
                            while ((i < iMax) && (ret == false))
                            {
                                ret = (ConnectionType() == WiFiStat.CONN_TYPE.NOCONNECTIONS) ? false : true;
                                if (!ret)
                                    Thread.Sleep(2000);
                                i++;
                            }
                        }
                        //if (ret)
                        //    IsEnabled = true;
                    }
                }

                return (ret);
            }

            public override string WiFiInfo()
            {
                bool ret = false;
                string s;
                RFServices.RFStatus xSt = RFServices.RFStatus.ST_SCANNING;

                RFServices.GetRFAdapterLinkStatus(ref m_LinkQuality, ref m_SigPercent, ref m_Associated);
                s = (m_Associated) ? "В сети" : "НЕТ сети";
                RFServices.GetRFAdapterStatus(ref xSt);
                s += "\r\n" + xSt.ToString();

                int nSize = 6;
                byte[] aMAC = new byte[6];

                bool bM = RFServices.GetRFAdapterMACAddress(aMAC, ref nSize);

                sMACAddr = "";
                foreach (byte b in aMAC)
                    sMACAddr += String.Format("{0:X2}", b);


                return (s);
            }

        }


		/// <summary>
		/// Инициализация сканера Dolphin 6100
		/// </summary>
		/// <returns>Whether initialization was successful</returns>
		public override bool Initialize() 
		{
            base.nTermType = TERM_TYPE.HWELL6100;
            xEng = new DecodeControl();

            xEng.DecodeEvent += new DecodeBase.DecodeEventHandler(xEng_DecodeEvent);
            xEng.TriggerKey = TriggerKeyEnum.TK_ONSCAN;
                
            xEng.DecodeMode = DecodeMode.DM_STANDARD;
            xEng.ScanTimeout = 5000;

            // Разрешение DATAMATRIX
            xEng.EnableSymbology(SymID.SYM_DATAMATRIX, true);
            xEng.AutoScan = true;

            // WiFi Init
            //base.WiFi = new HoneyWiFi();
            base.WiFi = new HoneyWiFi(30, 22, 164, 18);

			return(true);
		}

        void xEng_DecodeEvent(object sender, DecodeBase.DecodeEventArgs e)
        {
            BCId nCodeID = BCId.NoData;
            string sBarCode = string.Empty;
            BarcodeScannerEventArgs ba;

            try
            {
                //RightLEDOff();
                //--- Was the Decode Attempt Successful? ---
                if (e.DecodeResults.rResult == Result.RESULT_SUCCESS)
                {
                    //xEng.Device.SetLEDs(Device.LedSelect.RightGreen, Device.LedState.On, 0, 0);

                    //sBarCode = e.DecodeResults.chCodeID;
                    //switch (sBarCode)
                    //{
                    //    case "d":
                    //    case "D":
                    //        nCodeID = BCId.EAN13;
                    //        break;
                    //    case "j":
                    //        nCodeID = BCId.Code128;
                    //        break;
                    //    case "w":
                    //        nCodeID = BCId.DataMatrix;
                    //        break;
                    //    default:
                    //        break;
                    //}

                    nCodeID = base.SetBarcodeType(e.DecodeResults.chCodeID);
                    sBarCode = e.DecodeResults.pchMessage;

                    //--- Play an SDK Provided Audible Sound ---
                    //xEng.so..Sound.Play(Sound.SoundTypes.Success);
                    ba = new BarcodeScannerEventArgs(nCodeID, sBarCode, e.DecodeResults.binaryData, e.DecodeResults.binaryData.Length);
                    OnBarcodeScan(ba);
                }
                else
                {

                        //--- Async Decode Exception ---
                        switch (e.DecodeResults.rResult)
                        {
                            case Result.RESULT_ERR_CANCEL:            // Async Decode was Canceled
                                return;
                            case Result.RESULT_ERR_NODECODE:          // Scan Timeout
                                //MessageBox.Show("Scan Timeout Exceeded");
                                break;
                            default:
                                //MessageBox.Show(e.DecodeException.Message);
                                break;
                        }

                    //if (e.DecodeResults.rResult..DecodeException != null)
                    //{




                    //}
                    //else
                    //{
                    //    //--- Generic Async Exception ---
                    //    //MessageBox.Show(e.Exception.Message);
                    //}
                    //xEng.Sound.Play(Sound.SoundTypes.Failure);
                }
            }
            catch (Exception ex)
            {
                //MessageBox.Show(ex.Message);
            }

            return;
        }

        private void RightLEDOff()
        {
            //Not all modes are supported on all devices.
            //PlatformNotSupportedException will be thrown if this is the case.
            //Device dha = xEng.Device;
            try
            {
                //dha.SetLEDs(Device.LedSelect.RightGreen, Device.LedState.Off, 0, 0);
            }
            catch (PlatformNotSupportedException)
            {
                //Do Nothing
            }

            try
            {
                //dha.SetLEDs(Device.LedSelect.RightOrange, Device.LedState.Off, 0, 0);
            }
            catch (PlatformNotSupportedException)
            {
                //Do Nothing
            }

            try
            {
                //dha.SetLEDs(Device.LedSelect.RightRed, Device.LedState.Off, 0, 0);
            }
            catch (PlatformNotSupportedException)
            {
                //Do Nothing
            }
        }


		/// <summary>
        /// Start сканера Dolphin 9950
		/// </summary>
		public override void Start() 
		{
            if (bMayScan == true)
            {
                //RightLEDOff();
                //xEng.Device.SetLEDs(Device.LedSelect.RightRed, Device.LedState.On, 0, 0);
                //xEng.ScanBarcode();
                xEng.Connect();
                
            }
            bMayScan = false;
        }

		/// <summary>
        /// Stop a сканера Dolphin 9950
		/// </summary>
		public override void Stop() 
		{
            //xEng.CancelScanBarcode();
            //RightLEDOff();
            xEng.Disconnect();
            bMayScan = true;
        }

		/// <summary>
        /// Terminates сканера Dolphin 9950
		/// </summary>
		public override void Terminate()
		{
		}



#region IDisposable Members
    public override void Dispose(bool disposing)
    {
      if (disposing)
      {
        // Code to clean up managed resources
          xEng.Dispose();
      }
      // Code to clean up unmanaged resources
    }
#endregion
  }
}

#endif