using System;

#if PSC
using Terminal.API;

//namespace Microsoft.Samples.Barcode.PSC
namespace ScannerAll.PSC
{

    public class PSCBarcodeScannerFactory : BarcodeScannerFactory
    {
        System.Windows.Forms.Control ctl;

        public PSCBarcodeScannerFactory(System.Windows.Forms.Control ctlInvoker)
        {
            // Для PSC нужен Control, обрабатывающий Event сканирования
            ctl = ctlInvoker;
            try
            {
                Terminal.API.UnitModel umPSC = Terminal.API.UnitAPI.UnitGetModel();
                if (umPSC == Terminal.API.UnitModel.Model4220)
                    base.OEMInf = TERM_TYPE.PSC4220;
                else
                    base.OEMInf = TERM_TYPE.PSC4410;
            }
            catch { }
        }

        public override BarcodeScanner GetBarcodeScanner()
        {
            PSCBarcodeScanner pscBC = new PSCBarcodeScanner(base.OEMInf);
            pscBC.SetScanHandler(ctl);
            return ( pscBC );
        }
    }

	/// <summary>
	/// This is the barcode scanner class for PSC devices.
	/// </summary>
	public class PSCBarcodeScanner : BarcodeScanner 
	{
        // Для PSC нужен Control, обрабатывающий Event сканирования
        public static System.Windows.Forms.Control cInvoker = null;

        private DcdHandle hPSC = null;
        private DcdEvent dcdEvent;

        private CodeId nCodeID;
        private string sBarCode;

        public PSCBarcodeScanner(TERM_TYPE nT)
        {
            base.nTermType = nT;
        }

		/// <summary>
		/// Инициализация PSC сканера
		/// </summary>
		/// <returns>Whether initialization was successful</returns>
		public override bool Initialize() 
		{
            bool ret = false;
            if (null == hPSC)
            {
                try
                {
                    hPSC = new Terminal.API.DcdHandle(DcdDeviceCap.Exists | DcdDeviceCap.Barcode);
                    SetScanHandler(cInvoker);
                    ret = true;
                }
                //catch (DcdException) - если надо
                catch{}
            }
			return(ret);
		}

        public void SetScanHandler(System.Windows.Forms.Control cMainInvoker)
        {
            if (null != cMainInvoker)
            {
                cInvoker = cMainInvoker;
                // Now that we've got a connection to a barcode reading device, assign a
                // method for the DcdEvent.  A recurring request is used so that we will
                // continue to get barcode data until our dialog is closed.
                DcdRequestType reqType = (DcdRequestType)1 | DcdRequestType.PostRecurring;

                // Initialize event
                dcdEvent = new DcdEvent(hPSC, reqType, cInvoker);
                dcdEvent.Scanned += new DcdScanned(dcdEvent_Scanned);
            }
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
            if (dcdEvent != null)
            {
                if (dcdEvent.IsListening)
                {
                    dcdEvent.StopScanListener();
                }
            }
		}

		/// <summary>
		/// Event that fires when a PSC scanner has performed a scan.
		/// </summary>
        private void dcdEvent_Scanned(object sender, DcdEventArgs e)
        {
            nCodeID  = CodeId.NoData;
            sBarCode = string.Empty;

            // Obtain the string and code id.
            try
            {
                sBarCode = hPSC.ReadString(e.RequestID, ref nCodeID);
            }
            catch (Exception)
            {
                //MessageBox.Show("Error reading string!");
            }

            OnBarcodeScan(new BarcodeScannerEventArgs( (BCId)nCodeID, sBarCode ));
            return;
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