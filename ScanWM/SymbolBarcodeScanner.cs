using System;
using System.Threading;

#if SYMBOL
using Symbol;
using Symbol.Barcode;
using Symbol.Keyboard;
using Symbol.Fusion;
using Symbol.Fusion.WLAN;
//using Symbol.WirelessLAN;
//using Symbol.WPAN;
using Symbol.ResourceCoordination;

namespace ScannerAll.Symbol
{
    /// <summary>
    /// This is the barcode scanner class factory for Symbol devices.
    /// </summary>
    public class SymbolBarcodeScannerFactory : BarcodeScannerFactory
    {
        ConfigData cfgDat;
        public SymbolBarcodeScannerFactory()
        {
            cfgDat = new ConfigData();
        }
        public override BarcodeScanner GetBarcodeScanner(string OEM)
        {
            return new SymbolBarcodeScanner(TERM_TYPE.SYMBOL, cfgDat, OEM);
        }
    }

	/// <summary>
	/// This is the barcode scanner class for Symbol devices.
	/// </summary>
	public class SymbolBarcodeScanner : BarcodeScanner 
	{
		private Reader symbolReader = null;
		private ReaderData symbolReaderData = null;
        private WLAN xWLan = null;
        //private Radio xRadio = null;

        // для обработки спецрежимов работы с клавиатурой
        private KeyPad xSymKeyPad = null;

        // флаг специальной обработки - true:стрелки в редактировании работают только с Shift
        private bool bSpecALP = false;

        // обработчик смены режима ALP
        private ALPHandler dgALP = null;

        private string
            m_OEM;

        private int kstCur;

        public SymbolBarcodeScanner(TERM_TYPE nT, ConfigData cfgD, string OEM)
        {
            base.nTermType = nT;
            m_OEM = OEM;

            switch (cfgD.KEYBOARD)
            {
                case ((int)KeyboardTypes.MC3000_28KEY_FP):
                case ((int)KeyboardTypes.MC3000_28KEY):
                    base.nKeys = 28;
                    xSymKeyPad = new KeyPad();
                    xSymKeyPad.AlphaNotify += new KeyPad.KeyboardEventHandler(ALPNotify);
                    break;
                case ((int)KeyboardTypes.MC3000_48KEY):
                    // спецрежим актуален только для 48 клавиш
                    base.nKeys = 48;
                    xSymKeyPad = new KeyPad();
                    xSymKeyPad.AlphaNotify += new KeyPad.KeyboardEventHandler(ALPNotify);
                    xSymKeyPad.KeyStateNotify += new KeyPad.KeyboardEventHandler(Sym48_KeyStateNotify);
                    break;
                default:
                    break;
            }
        }

        // обработчик нажатий ALP
        private void ALPNotify(object sender, KeyboardEventArgs e){
            if (bSpecALP == true)
            {// включен спецрежим
                if (dgALP != null)
                {
                    bool bNewALP = dgALP(xSymKeyPad.AlphaMode);
                    if (bNewALP != xSymKeyPad.AlphaMode)
                        xSymKeyPad.AlphaMode = bNewALP;
                }
            }
            return;
        }

        // установить/сбросить режим специальной обработки ALP-режима
        public void SetSpecKeyALP(bool bNewReg, ALPHandler dgA){
            if (xSymKeyPad != null)
            {
                bSpecALP = bNewReg;
                if (bNewReg == true)
                    dgALP = dgA;
                else
                    dgALP = null;
            }
        }

        // текущее значение ALP
        public bool bALP{
            get
            {
                if (xSymKeyPad != null)
                    return (xSymKeyPad.AlphaMode);
                else
                    return (false);
            }
            set
            {
                if (xSymKeyPad != null)
                    xSymKeyPad.AlphaMode = value;
            }
        }

        // статус Shift
        private void Sym48_KeyStateNotify(object sender, KeyboardEventArgs e)
        {
            if ((e.KeyState & e.ActiveModifier & KeyStates.KEYSTATE_SHIFT) != 0)
                kstCur = e.KeyState;
        }

        // сбросить Shift для своих стрелок
        public void SetShiftOff()
        {
            if (xSymKeyPad != null)
            {
                int newstate = kstCur;
                newstate |= KeyStates.KEYSTATE_UNSHIFT;
                //xSymKeyPad.SetKeyState(newstate, KeyStates.KEYSTATE_UNSHIFT, true);
                xSymKeyPad.SetKeyState(newstate, KeyStates.KEYSTATE_UNSHIFT, false);
            }
        }

        // состояние WiFi
        public class SymWiFi : WiFiStat
        {
            object
                SigHandler = null;
            bool 
                bUseFusion = true;
            Adapter
                xWiFiCard = null;

            private CustPB 
                cSym;

            public SymWiFi(WLAN x):base()
            {
                cSym = new CustPB(this, "cSym");
                base.pgbWiFiIndicator = cSym;
                base.ConfigTextBox(31, 24, 164, 16);
                //base.STB.BringToFront();
                //base.STB.Refresh();

                //bUseFusion = true;
                //xWiFiCard = x.Adapters[0];
                try
                {
                    xWiFiCard = x.Adapters[0];
                    x.EventResolution = 2000;
                    SetMAC(xWiFiCard.MacAddressRaw);
                }
                catch
                {
                    sMACAddr = "000000000000";
                }
            }

            //public SymWiFi(Radio x): base()
            //{
            //    bUseFusion = false;
            //    xWiFiCard = x;
            //    SetMAC(x.MACAddress.Adapter.Raw);
            //}

            private void SetMAC(byte[] aMAC)
            {
                try
                {
                    string sMAC = "";
                    for (int i = 0; i < 6; i++)
                        sMAC += String.Format("{0:X2}", aMAC[i]);
                    long nMACNow = long.Parse(sMAC, System.Globalization.NumberStyles.HexNumber);
                    sMACAddr = sMAC;
                }
                catch
                {
                    sMACAddr = "000000000000";
                }
            }


            public void SigQChangedF(object sender, StatusChangeArgs e)
            {
                if (e.Change == StatusChangeArgs.ChangeType.SIGNAL)
                {// SignalStrength в децибелах
                    //if (e.SignalStrength <= -95)
                    //{
                    //    base.SignalPercent = (e.SignalStrength == -95) ? 1 : 0;
                    //}
                    //else if (e.SignalStrength > -35)
                    //{
                    //    base.SignalPercent = 100;
                    //}
                    //else
                    //{
                    //    int nS = Math.Abs(e.SignalStrength);
                    //    base.SignalPercent = (int)(((float)(95 - nS) / 60) * 100);
                    //}

                    base.SignalPercent = base.RSS2Level(WiFiStat.MINRSS, WiFiStat.MAXRSS, e.SignalStrength);
                    switch (e.SignalQuality)
                    {
                        case Adapter.SignalQualityRange.EXCELLENT:
                            base.SignalQuality = 31;
                            break;
                        case Adapter.SignalQualityRange.FAIR:
                            base.SignalQuality = 15;
                            break;
                        case Adapter.SignalQualityRange.GOOD:
                            base.SignalQuality = 20;
                            break;
                        case Adapter.SignalQualityRange.NONE:
                            base.SignalQuality = 0;
                            break;
                        case Adapter.SignalQualityRange.POOR:
                            base.SignalQuality = 10;
                            break;
                        case Adapter.SignalQualityRange.VERYGOOD:
                            base.SignalQuality = 25;
                            break;
                    }
                }
                WiFiStat.GetIP();
                WiFiStat.sESSID = xWiFiCard.ESSID;
                base.pgbWiFiIndicator.Invalidate();
            }

            //private void SigQChanged(object sender, EventArgs e)
            //{
            //    WLANStatus xStatus = ((Radio)(this.xWiFiCard)).GetNextStatus();

            //    if ((xStatus != null) && (xStatus.Change == ChangeType.SIGNAL))
            //        base.SignalPercent = ((Radio)(this.xWiFiCard)).Signal.Percent;
            //    WiFiStat.GetIP();
            //    WiFiStat.sESSID = ((Radio)(xWiFiCard)).ESSID.Text;
            //    base.STB.Invalidate();
            //}

            // nReg = 0 - Disable WiFi
            // nReg = 1 - Enable WiFi
            // nReg = 2 - Reset WiFi
            public override bool ResetWiFi(int nReg)
            {
                bool 
                    ShowState,
                    ret = false;
                WLAN 
                    xWLAN_IN_COM = null;

                if (bUseFusion)
                {
                    if (nReg == 2)
                    {
                        ShowState = IsShownState;
                        ShowWiFi(null, false);
                        try
                        {
                            xWLAN_IN_COM = new WLAN(FusionAccessType.COMMAND_MODE);
                            if (xWLAN_IN_COM != null)
                            {
                                xWLAN_IN_COM.Adapters[0].PowerState = Adapter.PowerStates.OFF;
                                Thread.Sleep(2000);
                                xWLAN_IN_COM.Adapters[0].PowerState = Adapter.PowerStates.ON;
                                Thread.Sleep(7000);
                                ret = true;
                            }
                        }
                        catch
                        {
                        }
                        finally
                        {
                            if (xWLAN_IN_COM != null)
                            {
                                xWLAN_IN_COM.Dispose();
                                xWLAN_IN_COM = null;
                            }
                            ShowWiFi(null, ShowState);
                        }
                        //FusionResults fr = xWiFiCard.RenewDHCP();
                        //ret = (fr == FusionResults.SUCCESS) ? true : false;
                    }
                }
                return (ret);
            }

            public override bool IsShownState
            {
                //get { return (SigQHandler == null) ? false : true; }
                get 
                { 
                    return (SigHandler == null) ? false : true; 
                }
                //set
                //{
                //    if (value == true)
                //    {
                //        if (bUseFusion == true)
                //        {
                //            //SigQHandler = new Adapter.SignalQualityHandler(SigQChanged);
                //            //xAd.SignalQualityChanged += SigQHandler;
                //            SigHandler = new Adapter.SignalQualityHandler(SigQChangedF);
                //            xWiFiCard.SignalQualityChanged += (Adapter.SignalQualityHandler)SigHandler;
                //        }
                //        else
                //        {
                //            //SigHandler = new EventHandler(SigQChanged);
                //            //((Radio)xWiFiCard).StatusNotify += (EventHandler)SigHandler;
                //        }
                //        //base.STB.BringToFront();
                //        //base.STB.Refresh();

                //        WiFiStat.GetIP();
                //    }
                //    else
                //    {
                //        //if (SigQHandler != null)
                //        //{
                //        //    xAd.SignalQualityChanged -= SigQHandler;
                //        //    SigQHandler = null;
                //        //}
                //        if (SigHandler != null)
                //        {
                //            if (bUseFusion == true)
                //                xWiFiCard.SignalQualityChanged -= (Adapter.SignalQualityHandler)SigHandler;
                //            //else
                //                //((Radio)xWiFiCard).StatusNotify -= (EventHandler)SigHandler;
                //            SigHandler = null;
                //        }
                //    }
                //}
            }

            // отобразить/скрыть индикатор уровня
            public override void ShowWiFi(System.Windows.Forms.Control cP, bool ShowNow)
            {

                if (ShowNow)
                {
                    if (bUseFusion == true)
                    {
                        SigHandler = new Adapter.SignalQualityHandler(SigQChangedF);
                        xWiFiCard.SignalQualityChanged += (Adapter.SignalQualityHandler)SigHandler;
                    }
                    else
                    {
                        //SigHandler = new EventHandler(SigQChanged);
                        //((Radio)xWiFiCard).StatusNotify += (EventHandler)SigHandler;
                    }
                    //base.STB.BringToFront();
                    //base.STB.Refresh();
                    WiFiStat.GetIP();
                }
                else
                {
                    //if (SigQHandler != null)
                    //{
                    //    xAd.SignalQualityChanged -= SigQHandler;
                    //    SigQHandler = null;
                    //}
                    if (SigHandler != null)
                    {
                        if (bUseFusion == true)
                            xWiFiCard.SignalQualityChanged -= (Adapter.SignalQualityHandler)SigHandler;
                        //else
                        //((Radio)xWiFiCard).StatusNotify -= (EventHandler)SigHandler;
                        SigHandler = null;
                    }
                }
                base.ShowWiFi(cP, ShowNow);
            }

        }



		/// <summary>
		/// Initiates the Symbol scanner.
		/// </summary>
		/// <returns>Whether initialization was successful</returns>
		public override bool Initialize() 
		{
            // If scanner is already present then fail initialize
			if ( symbolReader != null ) 
				return false;

			// Create new scanner, first available scanner will be used.
			symbolReader = new Reader();

			// Create scanner data
			symbolReaderData = new ReaderData(ReaderDataTypes.Text, ReaderDataLengths.DefaultText);

			// Create event handler delegate
      
            symbolReader.ReadNotify +=new EventHandler(symbolReader_ReadNotify);

			// Enable scanner, with wait cursor
			symbolReader.Actions.Enable();

            symbolReader.Actions.GetParameters();

			// Setup scanner
			symbolReader.Parameters.Feedback.Success.BeepTime = 0;
			symbolReader.Parameters.Feedback.Success.WaveFile = "\\windows\\alarm3.wav";

            // Для чтения кодов плохого качества
            symbolReader.Decoders.CODE128.Enabled = true;
            symbolReader.Decoders.CODE128.EAN128 = true;
            symbolReader.Decoders.CODE128.SecurityLevel = CODE128.SECURITYLEVEL.LEVEL_3;
            symbolReader.Decoders.CODE128.Redundancy = false;
            symbolReader.Decoders.CODE128.Other128 = true;
            symbolReader.Decoders.CODE128.MaximumLength = 55;

            symbolReader.Decoders.I2OF5.Enabled = true;
            symbolReader.Decoders.I2OF5.MaximumLength = 30;

            symbolReader.Decoders.RSS14.Enabled =
            symbolReader.Decoders.RSSEXP.Enabled =
            symbolReader.Decoders.RSSLIM.Enabled = true;

            symbolReader.Decoders.RSSEXP.MaximumLength = 76;

            symbolReader.Actions.SetParameters();

            // Настройка WiFi отображения
            try
            {
                xWLan = new WLAN(FusionAccessType.STATISTICS_MODE);
                base.WiFi = new SymWiFi(xWLan);
            }
            catch(Exception e)
            {
                //xRadio = new Radio();
                //base.WiFi = new SymWiFi(xRadio);
            }

			return true;
		}


		/// <summary>
		/// Start a Symbol scan.
		/// </summary>
		public override void Start() 
		{
			// If we have both a scanner and data
			if ( ( symbolReader != null ) && ( symbolReaderData != null ) ) 
				// Submit a scan
				symbolReader.Actions.Read(symbolReaderData);
		}

        public void SoftStart()
        {
            symbolReader.Actions.ToggleSoftTrigger();
        }



		/// <summary>
		/// Stop a Symbol scan.
		/// </summary>
		public override void Stop() 
		{
			// If we have a scanner
			if ( symbolReader != null ) 
				// Flush (Cancel all pending scans)
				symbolReader.Actions.Flush();
				//symbolReader.ReadNotify -= BarcodeScannerEventHandler;
		}

		/// <summary>
		/// Terminates the Symbol scanner.
		/// </summary>
		public override void Terminate()
		{
			// If we have a scanner
			if ( symbolReader != null ) 
			{
				// Disable the scanner
				symbolReader.Actions.Disable();

				// Free it up
				symbolReader.Dispose();

				// Indicate we no longer have one
				symbolReader = null;
			}

			// If we have a scanner data object
			if ( symbolReaderData != null ) 
			{
				// Free it up
				symbolReaderData.Dispose();

				// Indicate we no longer have one
				symbolReaderData = null;
			}
            if (xSymKeyPad != null){
                xSymKeyPad.AlphaNotify -= ALPNotify;
                xSymKeyPad.KeyStateNotify -= Sym48_KeyStateNotify;
                xSymKeyPad.Dispose();
            }
            if (xWLan != null)
                xWLan.Dispose();
            //if (xRadio != null)
            //    xRadio.Dispose();
        }

		/// <summary>
		/// Event that fires when a Symbol scanner has performed a scan.
		/// </summary>
		private void symbolReader_ReadNotify(object sender, EventArgs e) 
		{
			ReaderData readerData = symbolReader.GetNextReaderData();

			// If it is a successful scan (as opposed to a failed one)
			if ( readerData.Result == Results.SUCCESS ) 
			{
                BCId nType = BCId.NoData;

                // Преобразовать Symbol-тип штрихкода к моему
                switch(readerData.Type){
                    case DecoderTypes.EAN128:
                    case DecoderTypes.CODE128:
                        nType = BCId.Code128;
                        break;
                    case DecoderTypes.EAN13:
                        nType = BCId.EAN13;
                        break;
                    case DecoderTypes.EAN8:
                        nType = BCId.EAN8;
                        break;
                    case DecoderTypes.I2OF5:
                    case DecoderTypes.D2OF5:
                    case DecoderTypes.IATA2OF5:
                    case DecoderTypes.CHINESE_2OF5:
                        nType = BCId.Interleaved25;
                        break;
                    case DecoderTypes.IMAGE:
                        nType = BCId.Image;
                        break;
                    case DecoderTypes.CODE11:
                        nType = BCId.Code11;
                        break;
                    case DecoderTypes.CODE39:
                        nType = BCId.Code39;
                        break;
                    case DecoderTypes.RSS14:
                    case DecoderTypes.RSSEXP:
                    case DecoderTypes.RSSLIM:
                        nType = BCId.GS1DataBar;
                        break;
                    default:
                        nType = BCId.Unknown;
                        break;
                }

                // Raise scan event to caller (with data)
                BarcodeScannerEventArgs xBCA = new BarcodeScannerEventArgs(nType, readerData.Text);
                OnBarcodeScan(xBCA);

				// Start the next scan
				Start();
			}
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