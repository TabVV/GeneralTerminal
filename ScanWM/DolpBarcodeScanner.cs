using System;
using System.Drawing;
using System.Windows.Forms;
using System.Threading;
using Microsoft.Win32;

#if DOLPH7850

using HHP.DataCollection.Common;
using HHP.DataCollection.Decoding;
using HHP.DataCollection.PDTDecoding;
using HHP.Network.RF80211;
using HHP.Network.RadioMgr;

namespace ScannerAll.Dolphin
{

    public class DolphinBarcodeScannerFactory : BarcodeScannerFactory
    {
        public DolphinBarcodeScannerFactory()
        {
            // Creates a new instance of the DLScannerSetup class
            //ImagerProperties scannerSetup = new ImagerProperties();
            base.OEMInf = TERM_TYPE.DOLPH7850;
        }
        public override BarcodeScanner GetBarcodeScanner()
        {
            //return (new DolphinBarcodeScanner(base.OEMInf));
            return (new DolphinBarcodeScanner());
        }
    }

	/// <summary>
	/// This is the barcode scanner class for Dolphin devices.
	/// </summary>
	public class DolphinBarcodeScanner : BarcodeScanner 
	{
        private DecodeControl xEng;

        private class DolphinWiFi : WiFiStat
        {
            private CustPB cSym;
            private bool m_Enabled;
            private System.Windows.Forms.Timer tmSig = null;
            private int m_SigPercent = 0,
                m_LinkQuality = 0;
            private bool m_Associated = false;
            //private RadioMgrServices xRM;

            public DolphinWiFi(): base()
            {
                int nSize = 64;
                byte[] aMAC = new byte[64];

                cSym = new CustPB(this, "cSym");
                base.STB = cSym;
                base.ConfigTextBox(31, 24, 164, 16);

                //xRM = new RadioMgrServices();
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

            void tmSig_Tick(object sender, EventArgs e)
            {
                try
                {
                    RFServices.GetRFAdapterLinkStatus(ref m_LinkQuality, ref m_SigPercent, ref m_Associated);
                }
                catch { }
                base.SignalPercent = m_SigPercent;
                base.SigStrenght = m_LinkQuality;
                WiFiStat.GetIP();
                base.STB.Invalidate();
            }


            public override bool IsEnabled
            {
                get { return m_Enabled; }
                set
                {
                    if (value == true)
                    {
                        tmSig.Enabled = true;
                        tmSig_Tick(tmSig, new EventArgs());
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

                if (nReg != 1)
                {
                    ret = RadioMgrServices.SetRadioMode(RadioMgrServices.RadioOPMode.OP_NONE);
                    Thread.Sleep(2000);
                }
                if (nReg >= 1)
                {
                    ret = RadioMgrServices.SetRadioMode(RadioMgrServices.RadioOPMode.OP_WIFI);
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
                        if (ret)
                            IsEnabled = true;
                    }
                }

                return (ret);
            }
            
            
            public override string WiFiInfo()
            {
                bool ret = false;
                string s;
                //HHP.Network.RF80211.RFServices.RFStatus xSt;
                RFServices.RFStatus xSt = RFServices.RFStatus.ST_SCANNING;

                RFServices.GetRFAdapterLinkStatus(ref m_LinkQuality, ref m_SigPercent, ref m_Associated);
                s = (m_Associated) ? "В сети" : "НЕТ сети";
                RFServices.GetRFAdapterStatus(ref xSt);
                s += "\r\n" + xSt.ToString();

                return (s);
            }


            //public override CONN_TYPE ConnectionType()
            //{
            //    int nAdReg;
            //    RFServices.RFStatus xSt = RFServices.RFStatus.ST_SCANNING;

            //    CONN_TYPE eCType = CONN_TYPE.NOCONNECTIONS;
            //    if (WiFiStat.GetIP())
            //    {
            //        nAdReg = (int)Registry.GetValue(WiFiStat.sPathHW, "Cradled", 0);
            //        if (nAdReg == 1)
            //            eCType = CONN_TYPE.ACTIVESYNC;
            //        else
            //        {
            //            RFServices.GetRFAdapterStatus(ref xSt);
            //            if (xSt == RFServices.RFStatus.ST_AUTHENTICATED)
            //                eCType = CONN_TYPE.WIFI;
            //        }
            //    }
            //    return (eCType);
            //}


        }

        //public DolphinBarcodeScanner(TERM_TYPE nT)
        //{
        //    base.nTermType = nT;
        //}

		/// <summary>
		/// Инициализация сканера Dolphin 7850
		/// </summary>
		/// <returns>Whether initialization was successful</returns>
		public override bool Initialize() 
		{

            base.nTermType = TERM_TYPE.DOLPH7850;
            xEng = new DecodeControl();
            xEng.Visible = false;

            //xEng.DecodeMode = DecodeMode.DM_STANDARD;
            xEng.DecodeMode = DecodeMode.DM_STANDARD;
            xEng.ScanTimeout = 5000;
            xEng.AudioMode = AudioDevice.SND_STD_AND_FRONT;
            xEng.AutoSounds = true;
            xEng.AutoLEDs = true;

            // Разрешение DATAMATRIX
            xEng.EnableSymbology(SymID.SYM_DATAMATRIX, true);
/*
            xEng.EnableSymbology(SymID.SYM_EAN13, true);
            xEng.EnableSymbology(SymID.SYM_EAN8, true);
            xEng.EnableSymbology(SymID.SYM_INT25, true);
            xEng.EnableSymbology(SymID.SYM_CODE128, true);
 */ 

            xEng.DecodeEvent += new DecodeBase.DecodeEventHandler(xEng_DecodeEvent);


            //SymbologyConfig sc = new SymbologyConfig(SymID.SYM_DATAMATRIX);
            //sc.ReadConfig(SetupType.ST_CURRENT);
            //sc.flags |= SymFlags.SYMBOLOGY_ENABLE;
            //sc.WriteConfig();


            // WiFi Init
            base.WiFi = new DolphinWiFi();

			return true;
		}

        void xEng_DecodeEvent(object sender, DecodeBase.DecodeEventArgs e)
        {
            BCId nCodeID = BCId.NoData;
            string sBarCode = string.Empty;
            BarcodeScannerEventArgs ba;

            // Obtain the string and code id.
            try
            {
                if ((e.DecodeResults.nLength > 0) && (e.DecodeResults.rResult == Result.RESULT_SUCCESS))
                {
                    //switch (e.DecodeResults.chCodeID)
                    //{
                    //    case "d":
                    //    case "D":
                    //        nCodeID = BCId.EAN13;
                    //        break;
                    //    case "e":
                    //        nCodeID = BCId.Interleaved25;
                    //        break;
                    //    case "I":
                    //    case "j":
                    //        nCodeID = BCId.Code128;
                    //        break;
                    //    case "w":
                    //        nCodeID = BCId.DataMatrix;
                    //        break;
                    //    default:
                    //        break;
                    //}
nCodeID = SetBarcodeType(e.DecodeResults.chCodeID);
                    sBarCode = e.DecodeResults.pchMessage;
                }
            }
            catch (Exception)
            {
                //MessageBox.Show("Error reading string!");
            }

            ba = new BarcodeScannerEventArgs(nCodeID, e.DecodeResults.pchMessage, e.DecodeResults.binaryData, 
                e.DecodeResults.nLength);
            
            OnBarcodeScan(ba);
            return;
        }

		/// <summary>
        /// Start сканера Dolphin 7850
		/// </summary>
		public override void Start() 
		{
            xEng.AutoScan = true;
		}

		/// <summary>
        /// Stop a сканера Dolphin 7850
		/// </summary>
		public override void Stop() 
		{
            xEng.AutoScan = false;
        }

		/// <summary>
        /// Terminates сканера Dolphin 7850
		/// </summary>
		public override void Terminate()
		{
		}

        public void SoftStart()
        {
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