using System;
using System.Drawing;
using System.Windows.Forms;
using System.Threading;

#if DOLPH9950

using HandHeldProducts.Embedded.Decoding;
using HandHeldProducts.Embedded.Hardware;
using HandHeldProducts.Embedded.Utility;
using HandHeldProducts.Embedded.Wireless;

namespace ScannerAll.Honeywell
{

    public class HoneyBarcodeScannerFactory : BarcodeScannerFactory
    {
        public HoneyBarcodeScannerFactory()
        {
            base.OEMInf = TERM_TYPE.DOLPH9950;
        }
        public override BarcodeScanner GetBarcodeScanner(string OEM)
        {
            return (new HoneyBarcodeScanner(OEM));
        }
    }

	/// <summary>
	/// This is the barcode scanner class for Dolphin devices.
	/// </summary>
	public class HoneyBarcodeScanner : BarcodeScanner 
	{
        private DecodeAssembly 
            xEng;
        private bool 
            bMayScan = true;

        private string
            sOEM = "DOLPHIN9950";

        private class HoneyWiFi : WiFiStat
        {
            private CustPB cSym;
            private bool m_Enabled;
            private System.Windows.Forms.Timer tmSig = null;
            private int m_SigPercent = 0,
                m_LinkQuality = 0;
            private bool m_Associated = false;

            private RadioServices xRS;

            public HoneyWiFi(): base()
            {
                byte[] aMAC;

                cSym = new CustPB(this, "cSym");
                base.STB = cSym;
                base.ConfigTextBox(31, 24, 164, 16);
                xRS = new RadioServices();
                aMAC = xRS.GetRadioMacAddress();

                try
                {
                    sMACAddr = "";
                    for (int i = 0; i < 6; i++)
                        sMACAddr += String.Format("{0:X2}", aMAC[i]);
                }
                catch
                {
                    sMACAddr = "000000000000";
                }

                tmSig = new System.Windows.Forms.Timer();
                tmSig.Enabled = false;
                tmSig.Interval = 1500;
                tmSig.Tick += new EventHandler(tmSig_Tick);
            }

            void tmSig_Tick(object sender, EventArgs e)
            {
                m_SigPercent = 0;
                xRS.GetLinkStatus(ref m_LinkQuality, ref m_SigPercent, ref m_Associated);
                if (m_SigPercent < 0)
                    m_SigPercent = 0;
                base.SignalPercent = m_SigPercent;
                base.STB.Invalidate();
                //cSym.Refresh();
            }


            public override bool IsEnabled
            {
                get { return m_Enabled; }
                set
                {
                    if (value == true)
                    {
                        tmSig.Enabled = true;
                    }
                    else
                    {
                        tmSig.Enabled = false;
                    }
                    m_Enabled = value; }
            }

            // nReg = 0 - Disable WiFi
            // nReg = 1 - Enable WiFi
            // nReg = 2 - Reset WiFi
            public override bool ResetWiFi(int nReg)
            {
                bool ret = false;
                WirelessManager xWM = new WirelessManager();

                if (nReg != 1)
                {
                    xWM.SetRadioState(WirelessManager.Radios.Wlan, WirelessManager.RadioState.Off, true, false);
                    Thread.Sleep(2000);
                }
                if (nReg >= 1)
                {
                    xWM.SetRadioState(WirelessManager.Radios.Wlan, WirelessManager.RadioState.Connectable, true, false);
                    Thread.Sleep(2000);
                    if ( nReg == 2 )
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
                        if (ret)
                            IsEnabled = true;
                    }
                }

                return (ret);
            }

            public override string WiFiInfo()
            {
                string s;
                xRS.GetLinkStatus(ref m_LinkQuality, ref m_SigPercent, ref m_Associated);
                s = (m_Associated) ? "В сети" : "НЕТ сети";
                s += "\r\n" + xRS.Status.ToString();
                return (s);
            }


        }

        public HoneyBarcodeScanner(string OEM)
        {
            sOEM = OEM;
        }


		/// <summary>
		/// Инициализация сканера Dolphin 9950
		/// </summary>
		/// <returns>Whether initialization was successful</returns>
		public override bool Initialize() 
		{
            base.nTermType = TERM_TYPE.DOLPH9950;
            xEng = new DecodeAssembly();
            
            xEng.DecodeEvent += new DecodeAssembly.DecodeEventHandler(xEng_DecodeEvent);
            xEng.DecodeMode = DecodeAssembly.DecodeModes.Standard;
            xEng.ScanTimeout = 5000;

            // Разрешение DATAMATRIX
            xEng.EnableSymbology(SymbologyConfigurator.Symbologies.DataMatrix, true);

            // WiFi Init
            base.WiFi = new HoneyWiFi();

			return(true);
		}

        void xEng_DecodeEvent(object sender, DecodeAssembly.DecodeEventArgs e)
        {
            BCId nCodeID = BCId.NoData;
            string sBarCode = string.Empty;
            BarcodeScannerEventArgs ba;

            try
            {
                RightLEDOff();
                //--- Was the Decode Attempt Successful? ---
                if (e.ResultCode == DecodeAssembly.ResultCodes.Success)
                {
                    xEng.Device.SetLEDs(Device.LedSelect.RightGreen, Device.LedState.On, 0, 0);
                    //sBarCode = e.CodeId;
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
                    nCodeID = base.SetBarcodeType(e.CodeId);
                    sBarCode = e.Message;

                    //--- Play an SDK Provided Audible Sound ---
                    xEng.Sound.Play(Sound.SoundTypes.Success);
                }
                else
                {
                    if (e.DecodeException != null)
                    {
                        //--- Async Decode Exception ---
                        switch (e.DecodeException.ResultCode)
                        {
                            case DecodeAssembly.ResultCodes.Cancel:            // Async Decode was Canceled
                                return;
                            case DecodeAssembly.ResultCodes.NoDecode:          // Scan Timeout
                                //MessageBox.Show("Scan Timeout Exceeded");
                                break;
                            default:
                                //MessageBox.Show(e.DecodeException.Message);
                                break;
                        }
                    }
                    else
                    {
                        //--- Generic Async Exception ---
                        //MessageBox.Show(e.Exception.Message);
                    }
                    //xEng.Sound.Play(Sound.SoundTypes.Failure);
                }
            }
            catch (Exception ex)
            {
                //MessageBox.Show(ex.Message);
            }

            ba = new BarcodeScannerEventArgs(nCodeID, sBarCode, e.RawData, e.Length);
            OnBarcodeScan(ba);
            return;
        }

        private void RightLEDOff()
        {
            //Not all modes are supported on all devices.
            //PlatformNotSupportedException will be thrown if this is the case.
            Device dha = xEng.Device;
            try
            {
                dha.SetLEDs(Device.LedSelect.RightGreen, Device.LedState.Off, 0, 0);
            }
            catch (PlatformNotSupportedException)
            {
                //Do Nothing
            }

            try
            {
                dha.SetLEDs(Device.LedSelect.RightOrange, Device.LedState.Off, 0, 0);
            }
            catch (PlatformNotSupportedException)
            {
                //Do Nothing
            }

            try
            {
                dha.SetLEDs(Device.LedSelect.RightRed, Device.LedState.Off, 0, 0);
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
                RightLEDOff();
                xEng.Device.SetLEDs(Device.LedSelect.RightRed, Device.LedState.On, 0, 0);
                xEng.ScanBarcode();
            }
            bMayScan = false;
        }

		/// <summary>
        /// Stop a сканера Dolphin 9950
		/// </summary>
		public override void Stop() 
		{
            xEng.CancelScanBarcode();
            RightLEDOff();
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