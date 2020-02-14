using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace NordicId
{
    /// <summary>
    /// This class is used to redirect scanner results to user form instance through delegate.
    /// You can enable or disable scan helper functionality on device at any time.
    /// </summary>
    /// /// <example>
    /// <code>
    /// class MyForm : .....
    /// {
    ///     ScanHelper scanHelper = new ScanHelper(); 
    /// 
    ///     public void ResultDelegate(string scanResult)
    ///     {
    ///         MessageBox.Show(scanResult, "RESULT");            
    ///     }
    /// 
    ///     // Must be called at init time
    ///     public void InitScanHelper() 
    ///     {
    ///         if (!scanHelper.Initialize(this, ResultDelegate))
    ///         {
    ///             MessageBox.Show("Could not init ScanHelper", "ERROR");
    ///             // .. Handle error here ..
    ///         }
    /// 
    ///         // Scan helper initialized successfully
    ///     }
    /// 
    ///     // ...... Rest of the code ......
    /// }
    /// </code>
    /// </example>
    public class ScanHelper
    {
        /// <summary>
        /// Delegate prototype for getting scanner results from ScanHelper
        /// </summary>
        /// <param name="barcode">Result string</param>
        public delegate void ScanResult(string barcode);

        // Size statically defined at other end
        private const int MESSAGE_MAX_SIZE = 4096;

        private IntPtr hRedirectSignalEvent = IntPtr.Zero;
        private IntPtr hMsgQueueHandle = IntPtr.Zero;
        
        private ScanResult scanResultDelegate = null;
        private Thread scannerThread = null;
        private bool runWorkerThread = false;
        private Form destFormInstance = null;

        /// <summary>
        /// std constructor.
        /// </summary>
        public ScanHelper()
        {

        }

        /// <summary>
        /// Constructor with destination form and delegate
        /// </summary>
        public ScanHelper(Form destFormInstance, ScanResult resultDelegate)
        {
            Initialize(destFormInstance, resultDelegate);
        }

        /// <summary>
        /// Initialize scan helper with destination form and delegate
        /// Returns true on success, false on failure
        /// </summary>
        public bool Initialize(Form destFormInstance, ScanResult resultDelegate)
        {
            if (!runWorkerThread)
            {
                if (destFormInstance == null || resultDelegate == null) 
                    return false;

                // Create worker thread for scanning
                if (SetupQueue())
                {
                    this.SetResultDelegate(destFormInstance, resultDelegate);

                    // Attach to forms disposed event
                    this.destFormInstance.Disposed += new EventHandler(destFormInstance_Disposed);

                    runWorkerThread = true;
                    scannerThread = new Thread(new ThreadStart(this.ScannerWorkerThreadFunction));
                    scannerThread.Start();
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Set scan helper destination form and delegate
        /// Returns true on success, false on failure
        /// </summary>
        public bool SetResultDelegate(Form destFormInstance, ScanResult resultDelegate)
        {
            if (destFormInstance == null || resultDelegate == null)
                return false;

            // Detach form disposed event
            if (this.destFormInstance != null)
            {
                destFormInstance.Disposed -= new EventHandler(destFormInstance_Disposed);
            }

            this.scanResultDelegate = resultDelegate;
            this.destFormInstance = destFormInstance;

            // Attach to form disposed event
            this.destFormInstance.Disposed += new EventHandler(destFormInstance_Disposed);

            return true;
        }

        /// <summary>
        /// Returns true if scan helper is running
        /// </summary>
        public bool IsRunning() 
        { 
            return runWorkerThread; 
        }

        /// <summary>
        /// Stop scan helper
        /// </summary>
        public void Shutdown()
        {
            if (runWorkerThread && scannerThread != null)
            {
                runWorkerThread = false;
                FreeQueue();
                scannerThread.Join(1000);
                scannerThread = null;

                if (destFormInstance != null)
                {
                    destFormInstance.Disposed -= new EventHandler(destFormInstance_Disposed);
                }
                
                scanResultDelegate = null;
                destFormInstance = null;
            }
        }

        /// <summary>
        /// std destructor.
        /// </summary>
        ~ScanHelper()
        {
            Shutdown();
        }

        private void destFormInstance_Disposed(object sender, EventArgs e)
        {
            Shutdown();
        }

        private bool SetupQueue()
        {
            WIN32.MSGQUEUEOPTIONS q_opts = new WIN32.MSGQUEUEOPTIONS();
            q_opts.dwSize = Marshal.SizeOf(q_opts);
            q_opts.dwFlags = WIN32.MSGQUEUE_ALLOW_BROKEN;
            q_opts.cbMaxMessage = MESSAGE_MAX_SIZE;
            q_opts.dwMaxMessages = 10;
            q_opts.bReadAccess = WIN32.ACCESS_READ;

            hMsgQueueHandle = WIN32.CreateMsgQueue("WEDGE_REDIRECT_QUEUE", ref q_opts);                                                    
            hRedirectSignalEvent = WIN32.CreateEvent(IntPtr.Zero, false, false, "WEDGE_REDIRECTOR");

            return (hMsgQueueHandle != IntPtr.Zero && hRedirectSignalEvent != IntPtr.Zero);
        }

        private void FreeQueue()
        {
            if (hRedirectSignalEvent != IntPtr.Zero)
            {
                // Stop redirecting Wedge data to our queue
                WIN32.ResetEvent(hRedirectSignalEvent);
                WIN32.CloseHandle(hRedirectSignalEvent);
            }
            hRedirectSignalEvent = IntPtr.Zero;

            if (hMsgQueueHandle != IntPtr.Zero)
            {
                WIN32.CloseMsgQueue(hMsgQueueHandle);
            }
            hMsgQueueHandle = IntPtr.Zero;
        }

        private void ScannerWorkerThreadFunction()
        {
            // Wedge read buffer, note that 2D codes can be quite big
            IntPtr msgBuffer  = Marshal.AllocHGlobal(MESSAGE_MAX_SIZE);
            int bytesRead     = 0;
            int msgProperties = 0;

            while (runWorkerThread && hMsgQueueHandle != IntPtr.Zero)
            {
                WIN32.SetEvent(hRedirectSignalEvent);

                bytesRead = 0;
                msgProperties = 0;

                if (WIN32.ReadMsgQueue(hMsgQueueHandle, msgBuffer, MESSAGE_MAX_SIZE, out bytesRead, WIN32.INFINITE, out msgProperties))
                {
                    String msg_string = Marshal.PtrToStringUni(msgBuffer, bytesRead / 2);
                    // Notify user form delegate
                    if (scanResultDelegate != null)
                    {
                        destFormInstance.Invoke(new ScanResult(scanResultDelegate), new object[] { msg_string });
                    }
                }
                else
                {
                    // Fail!
                    break;                    
                }                
            }

            Marshal.FreeHGlobal(msgBuffer);
            runWorkerThread = false;
        }
    }
}
