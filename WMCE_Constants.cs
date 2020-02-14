using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;
using System.Drawing;
using System.Runtime.InteropServices;
using System.IO;
using System.Xml;
using System.Xml.Serialization;
using System.Data;
using System.Threading;

using FRACT = System.Decimal;


namespace PDA.OS
{
    
    public sealed class W32{
        // Windows constants
        // Message codes
        public const int WM_KEYDOWN = 0x0100;
        public const int WM_KEYUP = 0x0101;
        public const int WM_SYSKEYDOWN = 0x0104;
        public const int WM_SYSKEYUP = 0x0105;

        // User-defined messages
        public const int WM_APP = 0x8000;
        public const int WM_SCANNED = WM_APP + 1;

        // Виртуальные коды клавиш ()
        public const int VK_SHIFT = 0x10;
        public const int VK_LSHIFT = 0xA0;
        public const int VK_RSHIFT = 0xA1;
        public const int VK_LCONTROL = 0xA2;
        public const int VK_RCONTROL = 0xA3;
        public const int VK_LALT = 0xA4;
        public const int VK_RALT = 0xA5;

        public const int VK_LEFT = 0x25;
        public const int VK_UP = 0x26;
        public const int VK_RIGHT = 0x27;
        public const int VK_DOWN = 0x28;

        public const int VK_PGUP = 33;
        public const int VK_PGDOWN = 34;

        public const int VK_BACK = 0x08;


        public const int VK_ESC = 27;
        public const int VK_TAB = 0x09;
        public const int VK_ENTER = 0x0D;
        public const int VK_SPACE = 32;         // пробел

        public const int VK_HOME = 36;
        public const int VK_USER_QUIT = 0xC4;  //196 - (FN2-Esc)
        public const int VK_SCAN = 0xE8;

        public const int VK_MONSIGN = 150;      // решетка
        public const int VK_QUOTE = 151;        // кавычкм
        public const int VK_EQUAL = 0xBB;       // равенство

        public const int VK_MULTIPLY = 0x6A;    // умножить (*)

        public const int VK_D0 = 0x30;          // 0
        public const int VK_D1 = 0x31;          // 1
        public const int VK_D2 = 0x32;          // 2
        public const int VK_D3 = 0x33;          // 3
        public const int VK_D4 = 0x34;          // 4
        public const int VK_D5 = 0x35;          // 5
        public const int VK_D6 = 0x36;          // 6
        public const int VK_D7 = 0x37;          // 7
        public const int VK_D8 = 0x38;          // 8
        public const int VK_D9 = 0x39;          // 9
        public const int VK_ASCII_M = 0x4D;     // просто M
        public const int VK_ASCII_S = 0x53;     // просто S
        public const int VK_ASCII_Y = 89;       // просто Y

        public const int VK_F24 = 207;
        public const int VK_F25 = 208;

        public const int VK_FWIN = 209;
        public const int VK_FCALIB = 211;

        public const int VK_APHOST = 0xC0;     // апостроф

#if HWELL6100
        // Dolphin6100
        public const int VK_CONTROL = 212;
        public const int VK_HYPHEN = 0xBD;      // минус (-)
        public const int VK_PERIOD = 0xBE;      // точка

        public const int VK_F1 = 196;
        public const int VK_F2 = 197;
        public const int VK_F3 = 198;
        public const int VK_F4 = 199;
        public const int VK_F5 = 200;
        public const int VK_F6 = 201;
        public const int VK_F7 = 202;
        public const int VK_F8 = 203;
        public const int VK_F9 = 204;
        public const int VK_F10= 205;

        public const int VK_FUNC_F1 = 215;
        public const int VK_FUNC_F2 = 216;
        public const int VK_FUNC_F3 = 217;
        public const int VK_FUNC_F4 = 218;

#elif DOLPH7850
        public const int VK_F1      = 146;
        public const int VK_F2      = 147;
        public const int VK_F3      = 114;
        public const int VK_F4      = 115;
        public const int VK_F5      = 116;
        public const int VK_F6      = 148;
        public const int VK_F7      = 149;
        public const int VK_F8      = 119;
        public const int VK_F9      = 120;
        public const int VK_F10     = 121;

        public const int VK_PLUS    = 107;      // плюс (+)
        public const int VK_HYPHEN  = 109;      // минус (-)
        public const int VK_PERIOD  = 110;      // точка
        public const int VK_COMMA   = 188;      // запятая
#elif DOLPH9950
        public const int VK_F1      = 227;
        public const int VK_F2      = 228;
        public const int VK_F3      = 230;
        public const int VK_F4      = 233;

        public const int VK_PLUS    = 107;      // плюс (+)
        public const int VK_HYPHEN  = 109;      // минус (-)
        public const int VK_PERIOD  = 190;      // точка
        public const int VK_COMMA   = 188;      // запятая

        public const int VK_SEND    = 114;      // Blue-Shift (Send)

        // физически F6-F10 нет (имитация F1-F4 + Blue)
        public const int VK_F5      = 234;      // ;
        public const int VK_F6      = 235;      // :
        public const int VK_F7      = 236;      // \ (backslash)
        public const int VK_F8      = 237;      // / (forwardslash)

        public const int VK_F9      = 239;      // ну не знаю
        public const int VK_F10     = 240;      // ну не знаю

#else
        public const int VK_CONTROL = 0x11;

        public const int VK_F1 = 0x70;
        public const int VK_F2 = 0x71;
        public const int VK_F3 = 0x72;
        public const int VK_F4 = 0x73;
        public const int VK_F5 = 0x74;
        public const int VK_F6 = 0x75;
        public const int VK_F7 = 0x76;
        public const int VK_F8 = 0x77;
        public const int VK_F9 = 0x78;
        public const int VK_F10 = 0x79;

        public const int VK_PLUS   = 0xBB;      // плюс (+)
        public const int VK_HYPHEN = 0xBD;      // минус (-)
        public const int VK_PERIOD = 0xBE;      // точка
        public const int VK_COMMA  = 0xBC;       // запятая

        public const int VK_SEND = 114;      // Blue-Shift (Send)
#endif

        public const int VK_F11 = 0x7A;
        public const int VK_F12 = 0x7B;
        public const int VK_F13 = 0x7C;
        public const int VK_F14 = 0x7D;
        public const int VK_F15 = 0x7E;
        public const int VK_F16 = 0x7F;
        public const int VK_F17 = 0x80;
        public const int VK_F18 = 0x81;
        public const int VK_F19 = 0x82;
        public const int VK_F20 = 0x83;
        public const int VK_F21 = 0x84;
        public const int VK_F22 = 0x85;
        public const int VK_F23 = 0x86;

        public const int VK_DEL   = 46;

        // приходят с компа
        public const int VK_F1_PC = 112;
        public const int VK_F2_PC = 113;
        public const int VK_F3_PC = 114;
        public const int VK_F4_PC = 115;
        public const int VK_F5_PC = 116;
        public const int VK_F6_PC = 117;
        public const int VK_F7_PC = 118;
        public const int VK_F8_PC = 119;
        public const int VK_F9_PC = 120;
        public const int VK_F10_PC = 121;
        public const int VK_F11_PC = 122;
        public const int VK_F12_PC = 123;

        //[DllImport("coredll.dll")]
        //public static extern bool Beep(int freq, int duration);

        // типы для MessageBeep
        public const uint MB_0LOW_CLOSED        = 0x00000000;
        public const uint MB_1MIDDL_HAND        = 0x00000010;
        public const uint MB_2PROBK_QUESTION    = 0x00000020;
        public const uint MB_3GONG_EXCLAM       = 0x00000030;
        public const uint MB_4HIGH_FLY          = 0x00000040;


        // Для keybd_event
        internal const int KEYEVENTF_KEYUP = 0x02;
        internal const int KEYEVENTF_SILENT = 0x04;

        [DllImport("coredll.dll")]
        internal extern static void keybd_event(byte bVk, byte bScan, Int32 dwFlags, Int32 dwExtraInfo);

        public const int MOUSEEVENTF_LEFTDOWN = 0x0002; /* left button down */
        public const int MOUSEEVENTF_LEFTUP = 0x0004; /* left button up */
        public const int MOUSEEVENTF_ABSOLUTE = 0x8000; /* absolute move */

        [DllImport("coredll.dll", SetLastError = true)]
        public extern static void mouse_event(int nFlags, int x, int y, int Buttons, int ExtInf);

        // Вызов главного меню через имитацию клик
        public static void SimulMouseClick(int x, int y, Form f)
        {
            Point p = f.PointToScreen(new Point(x, y));
            int m1 = (65535 / Screen.PrimaryScreen.Bounds.Width),
                m2 = (65535 / Screen.PrimaryScreen.Bounds.Height);
            int xf = m1 * p.X,
                yf = m2 * p.Y;
            mouse_event(MOUSEEVENTF_LEFTDOWN | MOUSEEVENTF_ABSOLUTE, xf, yf, 0, 0);
            System.Threading.Thread.Sleep(300);
            mouse_event(MOUSEEVENTF_LEFTUP | MOUSEEVENTF_ABSOLUTE, xf, yf, 0, 0);
            System.Threading.Thread.Sleep(100);
        }

        // Имитация нажатия клавиши
        public static void SimulKey(int VKey, int SKey)
        {
            W32.keybd_event((byte)VKey, (byte)SKey, W32.KEYEVENTF_SILENT, 0);
            System.Threading.Thread.Sleep(250);
            W32.keybd_event((byte)VKey, (byte)SKey, W32.KEYEVENTF_KEYUP | W32.KEYEVENTF_SILENT, 0);
        }


        private string sAbrakadabra = "54546dsd45d5d";
        public static void BeepByKey()
        {
            byte kk = VK_F12;
            int nWait = 200;

            for (int i = 0; i < 2; i++)
            {
                keybd_event(kk, kk, 0, 0);
                keybd_event(kk, kk, KEYEVENTF_KEYUP, 0);
                Thread.Sleep(nWait - (i + 1) * 50);
            }
        }

        #region Not_Used
        /*
        [DllImport("coredll.dll", SetLastError = true)]
        public static extern IntPtr GetCapture();
         * 
        [DllImport("coredll.dll", SetLastError = true)]
        public static extern IntPtr MoveWindow(IntPtr hWnd, int X, int Y, int cx, int cy, bool Refresh);
         * 
        //[DllImport("coredll.dll", CharSet = CharSet.Auto)]
        //private static extern IntPtr GetForegroundWindow();
         * 
 * Определение Handle окна
         * и неудачная попытка сделать его на весь эран
            //this.Capture = true;
            //hMForm = GetCapture();
            //this.Capture = false;
         * 
            //MoveWindow(hMForm, 0, 0, 240, 320, true);
 * 
        [DllImport("coredll.dll", SetLastError = true)]
        internal static extern IntPtr CreateFile(String lpFileName, UInt32 dwDesiredAccess, UInt32 dwShareMode,
            IntPtr lpSecurityAttributes, UInt32 dwCreationDisposition, UInt32 dwFlagsAndAttributes,
            IntPtr hTemplateFile);

        //Constants for dwDesiredAccess:
        internal const UInt32 GENERIC_READ = 0x80000000;
        internal const UInt32 GENERIC_WRITE = 0x40000000;
        //Constants for dwCreationDisposition:
        internal const UInt32 OPEN_EXISTING = 3;

        [DllImport("coredll.dll")]
        internal static extern Boolean CloseHandle(IntPtr hObject);

 * 
 */
        #endregion

    }

    public class BATT_INF
    {
        // режим вывода сообщения на индикаторе
        public enum BITEXT : int
        {
            None = 0,
            Percent = 1,
            Message = 2,
            UserDef = 4
        }

        // описание уровней батареи
        private class LevelDef
        {
            public int LVal;
            public string LName;
            public Color LColB;
            public Color LColF;

            public LevelDef(int v, string s, Color c, Color f)
            {
                LVal = v;
                LName = s;
                LColB = c;
                LColF = f;
            }
        }

        // контрол отображения состояния батареи
        private class BIControl : UserControl
        {
            // родительский контрол
            private Control xParent = null;
            // цвет фона родительского контрола
            private Color colP;


            // геометрия индикатора
            private Point pLoc = new Point(2, 215);
            private Size Sz = new Size(77, 23);

            // уровень заряда в процентах
            private int nSigPrc = 100;

            // порог мигания (%)
            private int m_BlinkVal = 8;

            // предыдущий цвет мигания
            private Color m_CurrBlinkCol;


            // структура с информацией по батарее
            private static SYSTEM_POWER_STATUS_EX2 sysPS2 = new SYSTEM_POWER_STATUS_EX2();


            private void ConfigControl()
            {
                lstLD.Add(new LevelDef(5, "Опасно", Color.Red, Color.White));
                lstLD.Add(new LevelDef(8, "Низкий", Color.DarkRed, Color.White));
                lstLD.Add(new LevelDef(15, "Понижен", Color.Olive, Color.White));
                lstLD.Add(new LevelDef(45, "Средний", Color.ForestGreen, Color.White));
                lstLD.Add(new LevelDef(75, "Высокий", Color.MediumSeaGreen, Color.Black));

                this.Visible = false;
                this.Enabled = false;
                pLoc = this.Location;
                Sz = this.Size;

                tmShow = new System.Windows.Forms.Timer();
                tmShow.Tick += new EventHandler(tmShow_Tick);
                tmShow.Interval = m_DelayShow;
                tmShow.Enabled = false;

                tmBlink = new System.Windows.Forms.Timer();
                tmBlink.Tick += new EventHandler(tmBlink_Tick);
                tmBlink.Interval = m_DelayBlink;
                tmBlink.Enabled = false;

                colP = xParent.BackColor;
            }


            // получить статус батареи
            private void GetBattState()
            {
                int sLen = Marshal.SizeOf(sysPS2);
                try
                {
                    if (GetSystemPowerStatusEx2(ref sysPS2, sLen, true) == true)
                    {
                        nSigPrc = sysPS2.BatteryLifePercent;
                        if ((nSigPrc == 0) && (sysPS2.ACLineStatus == 1))
                            nSigPrc = 100;
                    }
                    else
                        nSigPrc = -1;
                }
                catch
                {
                    nSigPrc = 100;
                }
            }

            // Обработка таймера общего уровня
            private void tmShow_Tick(object sender, EventArgs e)
            {
                GetBattState();
                if (nSigPrc <= m_BlinkVal)
                {// вывод в мигающем режиме
                    if (tmBlink.Enabled == false)
                    {
                        m_CurrBlinkCol = colP;
                        tmBlink.Enabled = true;
                    }
                }
                else
                {
                    tmBlink.Enabled = false;
                    this.Refresh();
                }

            }

            // Обработка таймера для опасного уровня
            private void tmBlink_Tick(object sender, EventArgs e)
            {
                this.Refresh();
            }

            public BIControl(Control xP, Size s, Point loc)
            {
                float fFS = 9F;

                xP.SuspendLayout();
                this.SuspendLayout();

                xParent = xP;
                this.BackColor = xP.BackColor;
                this.ForeColor = xP.BackColor;
                this.BorderStyle = BorderStyle.Fixed3D;

                if (s == Size.Empty)
                    s = Sz;
                else
                {// размеры заданы подберем шрифт
                    if (s.Height < 6)
                        m_TitleMode = BITEXT.None;
                    else
                    {
                        if (s.Height <= 10)
                            fFS = s.Height - 4;
                        else if (s.Height > 15)
                            fFS = s.Height - 6;
                    }
                }
                if (loc == Point.Empty)
                    loc = pLoc;

                this.Font = new System.Drawing.Font("Tahoma", fFS, System.Drawing.FontStyle.Bold);

                this.Location = loc;
                this.Size = s;

                this.Name = "BTINF";
                this.TabIndex = 0;

                ConfigControl();
                this.ResumeLayout();

                xP.Controls.Add(this);
                xP.ResumeLayout();
            }

            protected override void OnPaint(PaintEventArgs e)
            {
                int
                    iM = lstLD.Count - 1,
                    i;
                string s = "";
                LevelDef
                    //ld,
                    l = lstLD[iM];

                base.OnPaint(e);

                //foreach (LevelDef ld in lstLD)
                //{
                //    if (nSigPrc <= ld.LVal)
                //    {
                //        l = ld;
                //        break;
                //    }
                //}

                for (i = 0; i < iM; i++)
                {
                    if (nSigPrc <= lstLD[i].LVal)
                    {
                        l = lstLD[i];
                        break;
                    }
                }


                // фон, соответствующий уровню заряда
                Color cB = l.LColB,
                    cF = l.LColF;
                if (tmBlink.Enabled == true)
                {
                    if (m_CurrBlinkCol != colP)
                    {// переключаемся на фон
                        cB = colP;
                        cF = colP;
                    }
                    m_CurrBlinkCol = cB;
                }

                e.Graphics.FillRectangle(new SolidBrush(cB), 0, 0, this.Width, this.Height);
                if (m_TitleMode != BITEXT.None)
                {
                    if (m_TitleMode == BITEXT.Percent)
                        s = nSigPrc.ToString() + "%";
                    else if (m_TitleMode == BITEXT.UserDef)
                        s = m_BIText;

                    e.Graphics.DrawString(s, this.Font, new SolidBrush(cF),
                        (this.Width / 2 - (e.Graphics.MeasureString(s, Font).Width / 2.0F)),
                            this.Height / 2 - (e.Graphics.MeasureString(s, Font).Height / 2.0F));
                }
            }

            //====================================
            // таймер обновления, мигания
            public System.Windows.Forms.Timer tmShow,
                tmBlink;

            // периодичность обновления, мигания
            public int
                m_DelayShow = 1000 * 10,
                m_DelayBlink = 400;

            // содержание сообщения на индикаторе
            public BITEXT
                m_TitleMode = BITEXT.Percent;

            // UserDefined текст сообщения на индикаторе
            public string
                m_BIText = "";

            // цвета для разных уровней заряда
            public List<LevelDef>
                lstLD = new List<LevelDef>(5);


            [StructLayout(LayoutKind.Sequential)]
            public struct SYSTEM_POWER_STATUS_EX2
            {
                public byte ACLineStatus;
                public byte BatteryFlag;
                public byte BatteryLifePercent;
                public byte Reserved1;
                public uint BatteryLifeTime;
                public uint BatteryFullLifeTime;
                public byte Reserved2;
                public byte BackupBatteryFlag;
                public byte BackupBatteryLifePercent;
                public byte Reserved3;
                public uint BackupBatteryLifeTime;
                public uint BackupBatteryFullLifeTime;
                // Далее идут уникальные поля (только в EX2)
                public uint BatteryVoltage; 		    // Reports Reading of battery voltage in millivolts (0..65535 mV)
                public uint BatteryCurrent;				// Reports Instantaneous current drain (mA). 0..32767 for charge, 0 to -32768 for discharge
                public uint BatteryAverageCurrent; 		// Reports short term average of device current drain (mA). 0..32767 for charge, 0 to -32768 for discharge
                public uint BatteryAverageInterval;		// Reports time constant (mS) of integration used in reporting BatteryAverageCurrent	
                public uint BatterymAHourConsumed; 		// Reports long-term cumulative average DISCHARGE (mAH). Reset by charging or changing the batteries. 0 to 32767 mAH  
                public uint BatteryTemperature;			// Reports Battery temp in 0.1 degree C (-3276.8 to 3276.7 degrees C)
                public uint BackupBatteryVoltage;		// Reports Reading of backup battery voltage
                public byte BatteryChemistry; 		    // See Chemistry defines above
            }                                           // ALKALINE-0x01,NICD-0x02,NIMH-0x03,LION-0x04,LIPOLY-0x05,ZINCAIR-0x06,UNKNOWN-0xFF

            [DllImport("Coredll.Dll")]
            public static extern bool GetSystemPowerStatusEx2(ref SYSTEM_POWER_STATUS_EX2 pSysPS, int StructLen, bool bUpd);
        }


        // контрол отображения состояния батареи
        private BIControl xBI;

        // разрешение мониторинга/отображения
        private bool m_EnableShow = false;

        //public BATTERY_INF(Control xP, Size s, Point loc)
        public BATT_INF(Control xP, Size s, Point loc)
        {
            this.xBI = new BIControl(xP, s, loc);
        }

        //=== Свойства
        public bool EnableShow
        {
            get { return m_EnableShow; }
            set
            {
                m_EnableShow = value;
                xBI.SuspendLayout();
                xBI.Enabled = value;
                xBI.Visible = value;
                xBI.tmShow.Enabled = value;
                if (value == true)
                    xBI.BringToFront();
                else
                    xBI.SendToBack();
                xBI.ResumeLayout();
            }
        }

        // Период обновления
        public int RefreshInt
        {
            get { return xBI.m_DelayShow; }
            set { xBI.m_DelayShow = value; }
        }

        // Основная надпись
        public BITEXT TitleMode
        {
            get { return xBI.m_TitleMode; }
            set { xBI.m_TitleMode = value; }
        }

        // UserDefined строка
        public string BIUserText
        {
            get { return xBI.m_BIText; }
            set
            {
                if (value.Length > 0)
                {
                    xBI.m_BIText = value;
                    TitleMode = BITEXT.UserDef;
                }
                else
                {
                    xBI.m_BIText = value;
                    TitleMode = BITEXT.None;
                }

            }
        }

        // Размер шрифта
        public float BIFont
        {
            set { xBI.Font = new System.Drawing.Font("Tahoma", value, System.Drawing.FontStyle.Bold); }
        }

        // Цвет фона строка
        //public Color CBIText
        //{
        //    get { return xBI.m_BIText; }
        //    set { xBI.m_BIText = value; }
        //}


        //=== Методы
        // Размер шрифта
        public void SetBIFont(string sFam, float fSize, System.Drawing.FontStyle ftStyle)
        {
            xBI.Font = new System.Drawing.Font(sFam, fSize, ftStyle);
        }

        public void SetBIFont(float fSize, System.Drawing.FontStyle ftStyle)
        {
            xBI.Font = new System.Drawing.Font("Tahoma", fSize, ftStyle);
        }

        public void SetLevels(object[][] aL)
        {
            this.xBI.lstLD.Clear();
            foreach (object[] aE in aL)
            {
                xBI.lstLD.Add(new LevelDef((int)aE[0], (string)aE[1], (Color)aE[2], (Color)aE[3]));
            }
        }



    }


}
