using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;
using System.Data;
using System.Drawing;
using System.Reflection;
using System.Xml;
using System.Xml.Serialization;
using System.IO;
using System.Threading;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Diagnostics;

using System.Collections;
using System.ComponentModel;
using Microsoft.Win32;

using PDA.OS;
using ScannerAll;

using FRACT = System.Decimal;


namespace PDA.BarCode
{
    // проверка на достаточность данных
    //public delegate bool TestBCFull(ScanVar xX);

    // накопление отсканированных данных
    public class ScanVar
    {
        // HEX-представление символа FNC1
        internal const string FNC1 = "\x1D";          // FNC1

        // сведения по одному AI и соответствующего поля из штрихкода
        public class OneFieldBC
        {
            public string Dat;              // строка данных
            public object xV;               // значение в формате, определенном типом
            public string TypeV;            // тип данных
            public string Prop;             // строковое имя поля
            public string Name;             // наименование

            public OneFieldBC(string n, string d, object x, string t, string p)
            {
                Name = n;
                Dat = d;
                xV = x;
                TypeV = t;
                Prop = p;
            }
        }


        // флаг начала обработки строки с штрихкодом
        private bool 
            bFirstField = true;

        // значение в формате, определенном типом
        private object xV;

        // все данные собраны
        //private bool m_FullData;


        // Таблица идентификаторов применения
        private static DataTable
            dtAI;

        public ScanVar():this(null){}

        public ScanVar(object d)
        {// настройка таблицы AI
            if (d is DataTable)
            {// передана таблица
                if (((DataTable)d).Rows.Count > 0)
                {
                    dtAI = (DataTable)d;
                    return;
                }
            }
            else if (d is string)
            {// имя таблицы
                if (dtAI == null)
                {
                    dtAI = DefaultAI((string)d);
                    return;
                }
            }
            if (dtAI == null)
                dtAI = DefaultAI("");
        }


        // заполнение таблицы с идентификаторами применения
        protected virtual DataTable DefaultAI(string sTName)
        {
            DataRow r;
            if (sTName.Length <= 0)
                sTName = "TNS_AI";
            DataTable dt = new DataTable(sTName);

            dt.Columns.AddRange(new DataColumn[]{
                new DataColumn("KAI", typeof(string)),          // Код идентификатора
                new DataColumn("NAME", typeof(string)),         // Наименование
                new DataColumn("TYPE", typeof(string)),         // Тип данных
                new DataColumn("MAXL", typeof(int)),            // Максимальная длина данных
                new DataColumn("VARLEN", typeof(int)),          // Признак переменной длины
                new DataColumn("DECP", typeof(int)),            // Позиция десятичной точки
                new DataColumn("PROP", typeof(string)),         // Поле
                new DataColumn("KED", typeof(string)) });       // Код единицы

            dt.PrimaryKey = new DataColumn[] { dt.Columns["KAI"] };
            dt.Columns["TYPE"].DefaultValue = "N";
            dt.Columns["DECP"].DefaultValue = 0;
            dt.Columns["VARLEN"].DefaultValue = 0;

            r = dt.NewRow();
            r["KAI"] = "00";
            r["NAME"] = "Серийный грузовой контейнерный код";
            r["TYPE"] = "C";
            r["MAXL"] = 18;
            r["PROP"] = "SSCC";
            dt.Rows.Add(r);

            r = dt.NewRow();
            r["KAI"] = "01";
            r["NAME"] = "Идентификационный номер единицы товара";
            r["TYPE"] = "C";
            r["MAXL"] = 14;
            r["PROP"] = "GTIN";
            dt.Rows.Add(r);

            r = dt.NewRow();
            r["KAI"] = "02";
            r["NAME"] = "GTIN торговых единиц, содержащихся в грузе";
            r["TYPE"] = "C";
            r["MAXL"] = 14;
            r["PROP"] = "CONTENT";
            dt.Rows.Add(r);

            dt.LoadDataRow(new object[] { "10", "Номер лота (партии, группы, пакета)",  "C", 20, 1, 0,  "LOT", "" }, true);
            dt.LoadDataRow(new object[] { "11", "Дата выработки (ГГММДД)",              "D", 6, 0, 0,   "PRODDATE", "" }, true);
            dt.LoadDataRow(new object[] { "15", "Минимальный срок годности (ГГММДД)",   "D", 6, 0, 0,   "BESTBEF", "" }, true);
            dt.LoadDataRow(new object[] { "17", "Максимальный срок годности (ГГММДД)",  "D", 6, 0, 0,   "USEBEF", "" }, true);
            dt.LoadDataRow(new object[] { "20", "Разновидность продукта",               "N", 2, 0, 0,   "VARIANT", "" }, true);
            dt.LoadDataRow(new object[] { "21", "Серийный номер",                       "C", 20, 1, 0,  "SERIAL", "" }, true);
            dt.LoadDataRow(new object[] { "23", "Номер лота  (переходный)",             "N", 19, 1, 0,  "LOTOLD", "" }, true);
            dt.LoadDataRow(new object[] { "30", "Переменное количество",                "N", 8, 1, 0,   "VARCOUNT", "" }, true);
            dt.LoadDataRow(new object[] { "37", "Количество торговых единиц  в грузе",  "N", 8, 1, 0,   "COUNT", "" }, true);
            dt.LoadDataRow(new object[] { "310", "Вес нетто, кг",                       "N", 6, 0, 1,   "NETKG", "кг" }, true);
            dt.LoadDataRow(new object[] { "330", "Вес брутто, кг",                      "N", 6, 0, 1,   "GROSSKG", "кг" }, true);
            dt.LoadDataRow(new object[] { "959", "SSCC партии продукта (на ящик)",      "N", 7, 0, 1,   "SSCC_PARTY", "" }, true);
            return (dt);
        }

        // обработка очередного поля штрихкода
        private void ProceedAI(string s)
        {
            int i,
                nL,
                nVarLen,
                nFNC1Pos,
                nPrec;
            string 
                sAI = "",
                sDat,
                sAdd,
                sType,
                sErr = "";

            DataRow dr = null;

            // управляющий FNC1 в начале пробрасывается
            if ((s.Length >= 1) && (s.Substring(0, 1) == FNC1))
                s = s.Substring(1);
            if (s.Length > 0)
            {
                if (bFirstField == true)
                {// в начале распознавания кода
                    bFirstField = false;
                    if ((s.Length >= 2) && (s.Length < 20))
                    {// Пробросить неверное определение AI=00
                        if (s.Substring(0, 2) == "00")
                            s = s.Substring(1);
                    }
                }

                try
                {
                    // длина ИП - от 2 до 4
                    for (i = 2; i < 5; i++)
                    {
                        sAI = s.Substring(0, i);
                        dr = dtAI.Rows.Find(new object[] { sAI });
                        if (dr != null)
                            break;
                    }
                    if (dr != null)
                    {
                        // строка после кода ИП
                        s = s.Substring(i);

                        // возможный доп. символ
                        // ??? если нет вобще
                        sAdd = s.Substring(0, 1);
                        // ??? если не цифра
                        try
                        {
                            nPrec = int.Parse(sAdd);
                        }
                        catch
                        {
                            nPrec = 0;
                        }

                        int nDec = (dr["DECP"] != System.DBNull.Value) ? (int)dr["DECP"] : 0;
                        nL = (dr["MAXL"] != System.DBNull.Value) ? (int)dr["MAXL"] : 0;
                        if (nL > 0)
                            nVarLen = (dr["VARLEN"] != System.DBNull.Value) ? (int)dr["VARLEN"] : 0;
                        else
                            nVarLen = 1;

                        if (sAI == "23")
                        {
                            nL = (nPrec * 2) + 1;
                            sDat = s.Substring(1, nL);
                            s = s.Substring(nL + 1);
                            //sAI += sAdd;
                        }
                        else
                        {
                            if (nVarLen >= 1)
                            {// для переменной длины данные - вся оставшаяся строка (было вначале)
                                sDat = s.Substring(0 + nDec);
                                nFNC1Pos = sDat.IndexOf(FNC1);
                                if (nFNC1Pos > 0)
                                {// есть признак завершения переменного поля
                                    if (nFNC1Pos < nL)
                                        nL = nFNC1Pos;
                                }

                                if (sDat.Length > nL)
                                {// после максимальной длины есть еще данные
                                    sDat = s.Substring(0 + nDec, nL);
                                    s = s.Substring(nL + nDec);
                                }
                                else
                                    s = "";
                            }
                            else
                            {// данные могут начинаться на символ дальше
                                sDat = s.Substring(0 + nDec, nL);
                                s = s.Substring(nL + nDec);
                            }
                            //if (nDec > 0)
                            //    sAI += sAdd;
                        }
                        if (!dicSc.ContainsKey(sAI))
                        {
                            sType = (string)dr["TYPE"];
                            switch (sType)
                            {
                                case "C":
                                    xV = sDat;
                                    break;
                                case "N":
                                    xV = long.Parse(sDat);
                                    if ((nDec > 0) && (nPrec > 0))
                                    {
                                        xV = (FRACT)((long)xV);
                                        xV = (FRACT)xV / (FRACT)(Math.Pow(10, nPrec));
                                        sType = "F";
                                    }
                                    break;
                                case "D":
                                    try
                                    {
                                        xV = DateTime.ParseExact(sDat, "yyMMdd", null);
                                    }
                                    catch
                                    {
                                        xV = null;
                                    }
                                    break;
                                default:
                                    throw new Exception("Неверный тип");
                            }
                            dicSc.Add(sAI, new OneFieldBC((string)dr["NAME"], sDat, xV, sType, (string)dr["PROP"]));
                        }
                        else
                        {
                            dicSc[sAI].Dat = sDat;
                            dicSc[sAI].xV = xV;
                        }
                    }
                    else
                        throw new Exception("Низвестный ИП");
                }
                catch (ArgumentOutOfRangeException e)
                {
                    sErr = (sAI.Length > 0) ? sAI + "-" : "";
                    throw new Exception(sErr + "Неверные данные!(" + s + ")");
                }
                catch (Exception e)
                {
                    sErr = (sAI.Length > 0) ? sAI + "-" : "";
                    throw new Exception(sErr + e.Message);
                }
            }
            if (s.Length == 0)
            {
                //if (dgTest.GetInvocationList().Length == 0)
                //if (dgTest == null)
                //    TestFullBC();
                //else
                //    dgTest(this);
            }
            else
                ProceedAI(s);
        }


        // таблица идентификаторы применения
        public static DataTable AITable
        {
            get { return dtAI; }
            set { dtAI = value; }
        }


        //public TestBCFull dgTest = null;

        /*

                public string sPstvOrig = "";   // код поставщика оригинальный
                public string sPostv = "";      // код поставщика наш

                public string sKMTOrig = "";    // код материала оригинальный
                public string sKMT = "";        // код материала наш

                public int nParty;              // партия
                public string sParty = "";      // партия


                public int nMestPal;            // количество мест на палетте

                // tmc
                public int nMest;               // количество мест
         */ 

        public Dictionary<string, OneFieldBC> dicSc = new Dictionary<string, OneFieldBC>();

        ////=====================================
        //// заполнены все реквизиты
        //public bool FullData
        //{
        //    get { return m_FullData; }
        //    set { m_FullData = value; }
        //}

        // проверка на достаточность данных
        //public virtual bool TestFullBC()
        //{
        //    if (dicSc.ContainsKey("00"))
        //        FullData = true;
        //    return(FullData);
        //}

        // присутствие в списке ключа
        //public bool IsKeyHere(string sK)
        //{
        //    return (dicSc.ContainsKey(sK));
        //}

        // разбор строки штрихкода
        public void ScanParse(string s)
        {
            bFirstField = true;
            ProceedAI(s);
        }

    }
   
}

namespace PDA.Service
{

    public partial class AppC
    {

        // пустое для Int
        public const int EMPTY_INT = -0x998877;

        // коды возврата
        public const int RC_OK = 0;
        public const int RC_QUIT = 2;
        public const int RC_NOFILE = 3;
        public const int RC_CANCEL = 5;

        // коды возврата (boolean)
        public const bool RC_OKB        = true;
        public const bool RC_CANCELB    = false;

        // коды функций
        public const int F_QUIT         = 1;    // выход из программы
        public const int F_ADD_REC      = 2;    // добавление документа
        public const int F_LOAD_DOC     = 3;    // загрузка документа
        public const int F_VIEW_DOC     = 4;    // просмотр детальных строк документа
        public const int F_CHG_REC      = 5;    // корректировка документа
        public const int F_DEL_REC      = 6;    // удаление документа
        public const int F_UPLD_DOC     = 7;    // сохранение документа
        public const int F_DEL_ALLREC   = 8;    // удаление всех документов
        public const int F_NEXTPAGE     = 20;   // следующая вкладка
        public const int F_PREVPAGE     = 21;   // предыдущая вкладка
        public const int F_HELP         = 30;   // вызов Help
        public const int F_GOFIRST      = 35;   // на 1-ю запись
        public const int F_GOLAST       = 36;   // на последнюю запись
        public const int F_MENU         = 40;   // вызов главного меню

        // команды управления курсором при редактировании
        public const int CC_CANCEL      = 0;    // отмена редактирования
        public const int CC_NEXT        = 1;    // на следующее поле
        public const int CC_PREV        = 2;    // на предыдующее поле
        public const int CC_NEXTOVER    = 3;    // на следующее или закончить

        // режимы работы с авторизацией
        public const int AVT_LOGON      = 1;    // начало сеанса
        public const int AVT_TOUT       = 2;    // после таймаута по бездействию
        public const int AVT_LOGOFF     = 5;    // завершение сеанса пользователя
        public const int AVT_PARS       = 7;    // только параметры

        // Режимы выгрузки
        internal const int UPL_CUR = 1;
        internal const int UPL_ALL = 2;
        internal const int UPL_FLT = 3;

        // Код SuperUser
        public const string SUSER = "987";
        // Общий код для авторизации
        //internal const string GUEST = "000";

        // права пользователей
        public enum USERRIGHTS : int
        {
            USER_KLAD = 1,                        // кладовщик
            USER_BOSS_SMENA = 10,                       // начальник смены
            USER_BOSS_SKLAD = 100,                      // начальник склада
            USER_ADMIN = 1000,                     // начальник смены
            USER_SUPER = 2000                      // наверное, Толик
        }

        // причины обмена
        public enum EXCHG_RSN : int
        {
            NO_EXCHG = 0,                           // в данный момент нет
            USER_COMMAND = 1,                       // вызвал пользователь
            SRV_INIT = 2                            // инициировал сервер
        }

        // режим работы DLL-формы
        public enum APPMODE : int
        {
            NET_BLANKS = 1,
            LOCAL_BLANKS = 2,
            NOBLANKS = 3
        }


        public struct VerRet
        {
            public int nRet;
            public Control cWhereFocus;
        }
        public delegate VerRet VerifyEditFields();



        // список Control для редактирования
        public class EditListC_Old : List<Control>
        {
            private int
                m_CurI;

            private AppC.VerifyEditFields 
                dgVer;

            private Control
                m_CtrlkBtwn = null,
                m_Cur = null;


            public VerRet VV()
            {
                VerRet v;
                v.nRet = AppC.RC_OK;
                v.cWhereFocus = null;
                return (v);
            }


            public EditListC_Old():base()
            {
                dgVer = new VerifyEditFields(VV);
            }


            public EditListC_Old(AppC.VerifyEditFields dgx)
                : base()
            {
                dgVer = dgx;
            }

            private void CreateFict(Control xC)
            {
                Fict4Next = new TextBox();
                Fict4Next.SuspendLayout();
                Fict4Next.Name = String.Format("TMP_Ed{0}", DateTime.Now.Ticks / 100000);
                Fict4Next.Visible = false;
                Fict4Next.Enabled = true;
                Fict4Next.Parent = xC.Parent;
                Fict4Next.ResumeLayout();
            }

            // добавить с список доступных контроловв ввода/редактирования
            public void AddC(Control xC)
            {
                AddC(xC, true);
            }

            public void AddC(Control xC, bool bEn)
            {
                xC.Enabled = bEn;
                base.Add(xC);
                if (Fict4Next == null)
                {
                    CreateFict(xC);
                }
            }

            // сделать указанный контрол текущим
            public int SetCur(Control xC)
            {
                m_Cur = xC;
                m_CurI = base.FindIndex(IsSame);
                xC.Focus();
                return (m_CurI);
            }

            // сделать указанный по индексу контрол текущим
            public Control SetCur(int i)
            {
                m_Cur = base[i];
                m_CurI = i;
                m_Cur.Focus();
                return (m_Cur);
            }

            // текущий 
            public Control Current
            {
                get {return m_Cur;}
                set { m_Cur = value;}
            }

            // Фиктивный для переходов
            public Control Fict4Next
            {
                get { return m_CtrlkBtwn; }
                set { m_CtrlkBtwn = value; }
            }


            // определить текущий контрол среди списка
            public Control WhichCur()
            {
                Control xC = null;
                for(int i = 0; i < base.Count; i++) 
                {
                    //if (base[i].Focused)
                    if (this[i].Focused)
                    {
                        //m_Cur = xC = base[i];
                        m_Cur = xC = this[i];
                        m_CurI = i;
                        break;
                    }
                }
                //if ((xC == null) && (m_CurI >= 0))
                //{
                //    xC = SetCur(m_CurI);
                //}
                return (xC);
            }


            // определить и установить текущий контрол
            public Control WhichSetCur()
            {
                Control xC = WhichCur();
                if (xC == null)
                {
                    //SetCur(base.FindIndex(IsNextOrPrev));
                    SetCur(this.FindIndex(IsNextOrPrev));
                    xC = Current;
                }
                return (xC);
            }


            private bool IsSame(Control x)
            {
                return ((x == m_Cur) ? true : false);
            }

            private bool IsNextOrPrev(Control x)
            {
                if (x == null)
                    x = m_Cur;
                return (x.Enabled);
            }

            private int TryMove(int i, bool bBack)
            {
                int
                    nRet = 0;
                return (nRet);
            }


            // попытка перехода на следующее поле при редактировании
            public bool TryNext(int nCommand)
            {
                int i = -1;
                bool bRet = AppC.RC_OKB;

                // можно попробовать сначала определить текущий, т.к.
                // он мог измениться нестандартным способом
                while ((!Current.Focused) && (i < base.Count))
                {
                    if (WhichCur() == null)
                    {// определить текущий не удалось
                        if (i == base.Count - 1)
                            return (AppC.RC_CANCELB);
                        else
                        {
                            if (base[++i].Enabled)
                            {
                                SetCur(i);
                                return (bRet);
                            }
                        }
                    }
                }

                // отработают все Valid
                //Current.Parent.Focus();
                Fict4Next.Focus();

                if (nCommand == AppC.CC_PREV)
                {// переход на предыдующий
                    i = (m_CurI > 0) ? base.FindLastIndex(m_CurI - 1, m_CurI, IsNextOrPrev) : -1;
                    if (i == -1)
                        i = base.FindLastIndex(base.Count - 1, base.Count, IsNextOrPrev);
                }
                else if ((nCommand == AppC.CC_NEXT) ||
                         (nCommand == AppC.CC_NEXTOVER))
                {
                    i = base.FindIndex(m_CurI + 1, IsNextOrPrev);
                    if (i == -1)
                    {
                        if (nCommand == AppC.CC_NEXTOVER)
                        {// следующего нет, это последнее поле
                            AppC.VerRet vRet = dgVer();
                            if (vRet.nRet == AppC.RC_OK)
                                return (AppC.RC_CANCELB);
                            if (vRet.cWhereFocus != null)
                            {
                                SetCur(vRet.cWhereFocus);
                                return (AppC.RC_OKB);
                            }
                        }
                        i = base.FindIndex(0, m_CurI, IsNextOrPrev);
                        if (i < 0)
                            //i = 0;
                            i = m_CurI;
                    }
                }

                if (i >= 0)
                    SetCur(i);
                else
                    bRet = AppC.RC_CANCELB;

                return (bRet);
            }



            public void EditIsOver()
            {
                for (int i = 0; i < base.Count; i++)
                    base[i].Enabled = false;
            }

            public void EditIsOver(Control x4Focus)
            {
                x4Focus.Focus();
                for (int i = 0; i < base.Count; i++)
                    base[i].Enabled = false;
            }

            public void EditIsOverEx(Control x4Focus)
            {
                for (int i = 0; i < base.Count; i++)
                {
                    if (base[i] != x4Focus)
                    {
                        base[i].Enabled = false;
                    }
                }
            }

            public void SetAvail(Control xC, bool bAvail)
            {
                int i = base.IndexOf(xC);
                if (i >= 0)
                {
                    bool bMayChange = true;
                    if ((xC == m_Cur) && (bAvail == false))
                        // пытаемся запретить текущий
                        bMayChange = TryNext(AppC.CC_NEXT);
                    if (bMayChange)
                        base[i].Enabled = bAvail;
                }
            }

        }


    }


    static class TimeSync
    {
        static ManualResetEvent manualEventWork = new ManualResetEvent(false);
        static volatile Boolean noRespond = false;
        static UdpClient client = new UdpClient();

        public static UInt32 seconds;
        public static String server = "bmkmail";
        public static Int32 port = 123;
        public static Int32 timeout = 15000;
        public static Int32 timeOffset = 3600;
        public static DateTime NewTime;

        private static DateTime nullTime = new DateTime(1900, 1, 1);

        #region SystemTime WinAPI
        private struct SYSTEMTIME
        {
            public ushort wYear;
            public ushort wMonth;
            public ushort wDayOfWeek;
            public ushort wDay;
            public ushort wHour;
            public ushort wMinute;
            public ushort wSecond;
            public ushort wMilliseconds;
        }

        [DllImport("coredll.dll")]
        private extern static uint SetSystemTime(ref SYSTEMTIME lpSystemTime);
        #endregion

        /// <summary>
        /// Синхронизировать системное время с NTP сервером
        /// </summary>
        /// <returns><code>True</code>-если синхронизация времени успешна</returns>
        static public Boolean Sync()
        {
            DateTime dt = GetTime();
            if (TimeNotNull(dt))
            {
                TimeSpan ts = DateTime.Now.ToUniversalTime() - dt;//DateTime.Now.Subtract(dt);
                if (Math.Abs(ts.TotalSeconds) > timeOffset)
                {
                    NewTime = dt;
                    return SetSystemTime(dt);
                }
            }
            return false;
        }

        /// <summary>
        /// Синхронизировать системное время с NTP сервером
        /// </summary>
        /// <param m_Name="serv">Сервер NTP - ip адрес или имя хоста</param>
        /// <param m_Name="prt">Порт для соединения с NTP сервером</param>
        /// <param m_Name="tmout">Таймаут ожидания</param>
        /// <param m_Name="offset">Количество секунд на кторое должно отличаться время что бы произошла синхронизация</param>
        /// <returns><code>True</code>-если синхронизация времени успешна или синхронизация не требуется</returns>
        static public Boolean Sync(String serv, Int32 prt, Int32 tmout, Int32 offset)
        {
            DateTime dt = GetTime(serv, prt, tmout);
            if (TimeNotNull(dt))
            {
                NewTime = dt;
                TimeSpan ts = DateTime.Now.ToUniversalTime() - dt;//DateTime.Now.Subtract(dt);
                if (Math.Abs(ts.TotalSeconds) > offset)
                {
                    return SetSystemTime(dt);
                }
                else
                    return true;
            }
            else
                return false;
        }

        /// <summary>
        /// Синхронизировать системное время с NTP сервером
        /// </summary>
        /// <param m_Name="serv">Сервер NTP - ip адрес или имя хоста</param>
        /// <returns><code>True</code>-если синхронизация времени успешна</returns>
        static public Boolean Sync(String serv)
        {
            DateTime dt = GetTime(serv);
            if (TimeNotNull(dt))
            {
                NewTime = dt;
                return SetSystemTime(dt);
            }
            return false;
        }

        #region Асинхронные дубликаты методов Sync
        /// <summary>
        /// Синхронизировать системное время с NTP сервером
        /// </summary>
        static public void SyncAsync()
        {
            new Thread(Synca).Start();
        }

        /// <summary>
        /// Синхронизировать системное время с NTP сервером
        /// </summary>
        /// <param m_Name="serv">Сервер NTP - ip адрес или имя хоста</param>
        static public void SyncAsync(String serv)
        {
            server = serv;
            new Thread(Synca).Start();
        }

        static public void SyncAsync(String serv, Int32 offset)
        {
            server = serv;
            timeOffset = offset;
            new Thread(Synca).Start();
        }

        /// <summary>
        /// Синхронизировать системное время с NTP сервером
        /// </summary>
        /// <param m_Name="serv">Сервер NTP - ip адрес или имя хоста</param>
        /// <param m_Name="prt">Порт для соединения с NTP сервером</param>
        /// <param m_Name="tmout">Таймаут ожидания</param>
        /// <param m_Name="offset">Количество секунд на кторое должно отличаться время что бы произошла синхронизация</param>
        static public void SyncAsync(String serv, Int32 prt, Int32 tmout, Int32 offset)
        {
            server = serv;
            port = prt;
            timeout = tmout;
            timeOffset = offset;
            new Thread(Synca).Start();
        }
        #endregion

        /// <summary>
        /// Устанавливает системное время
        /// </summary>
        /// <param m_Name="dt">Дата и время которые необходимо установить</param>
        /// <returns>True - если успешно установлено системное время</returns>
        public static Boolean SetSystemTime(DateTime dt)
        {
            SYSTEMTIME st;
            st.wDay = (ushort)dt.Day;
            st.wMonth = (ushort)dt.Month;
            st.wYear = (ushort)dt.Year;
            st.wDayOfWeek = (ushort)dt.DayOfWeek;
            st.wHour = (ushort)dt.Hour;
            st.wMinute = (ushort)dt.Minute;
            st.wSecond = (ushort)dt.Second;
            st.wMilliseconds = (ushort)dt.Millisecond;
            return SetSystemTime(ref st) > 0 ? true : false;
        }

        /// <summary>
        /// Получить время от NTP сервера
        /// </summary>
        /// <returns>Текущее время, если не произошло ошибки, иначе 1900.1.1</returns>
        static public DateTime GetTime()
        {
            Thread work = new Thread(GetTimeSeconds);
            Thread timer = new Thread(Timer);
            manualEventWork.Reset();
            seconds = 0;
            noRespond = false;
            work.Start();
            timer.Start();
            manualEventWork.WaitOne();
            if (noRespond)
            {
                work.Abort();
                client.Close();
                client = new UdpClient();
            }
            else
            {
                timer.Abort();
                timer.Join();
            }

            work.Join();

            return (new DateTime(1900, 1, 1)).AddSeconds(seconds);
        }
        /// <summary>
        /// Получить время от NTP сервера
        /// </summary>
        /// <param m_Name="srv">Сервер NTP - ip адрес или имя хоста</param>
        /// <param m_Name="prt">Порт для обращения к серверу</param>
        /// <param m_Name="tmout">Таймаут</param>
        /// <returns>Текущее время, если не произошло ошибки, иначе 1900.1.1</returns>
        static public DateTime GetTime(String srv, Int32 prt, Int32 tmout)
        {
            server = srv;
            port = prt;
            timeout = tmout;
            return GetTime();
        }
        /// <summary>
        /// Получить время от NTP сервера
        /// </summary>
        /// <param m_Name="srv">Сервер NTP - ip адрес или имя хоста</param>
        /// <returns>Текущее время, если не произошло ошибки, иначе 1900.1.1</returns>
        static public DateTime GetTime(String srv)
        {
            server = srv;
            return GetTime();
        }
        static private void Timer()
        {
            Thread.Sleep(timeout);
            noRespond = true;
            manualEventWork.Set();
        }
        static private Boolean TimeNotNull(DateTime dt)
        {
            if (dt.CompareTo(nullTime) == 0)
                return false;
            else
                return true;
        }

        static private void Synca()
        {
            Sync();
        }

        static private void GetTimeSeconds()
        {
            try
            {
                seconds = 0;
                // 0x1B == 0b11011 == NTP version 3, client - see RFC 2030
                byte[] ntpPacket = new byte[] { 0x1B, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                                   0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                                   0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                                   0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                                   0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                                   0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 };

                IPAddress[] addressList = Dns.GetHostEntry(server).AddressList;


                if (addressList.Length == 0)
                {
                    // error
                    return;
                }
                IPEndPoint ep = new IPEndPoint(addressList[0], port);
                if (ep != null)
                {
                    client.Connect(ep);
                    client.Send(ntpPacket, ntpPacket.Length);
                    byte[] data = client.Receive(ref ep);
                    // receive date sBarCode is at offset 32
                    // Data is 64 bits - first 32 is seconds - we'll toss the fraction of a second
                    // it is not in an endian order, so we must rearrange
                    byte[] endianSeconds = new byte[4];
                    endianSeconds[0] = (byte)(data[32 + 3] & (byte)0x7F); // turn off MSB (some servers set it)
                    endianSeconds[1] = data[32 + 2];
                    endianSeconds[2] = data[32 + 1];
                    endianSeconds[3] = data[32 + 0];
                    seconds = BitConverter.ToUInt32(endianSeconds, 0);
                }
                manualEventWork.Set();
            }
            catch (ThreadAbortException)
            {
                Debug.WriteLine("Не удалось получить время");
            }
            catch
            {
                Debug.WriteLine("Не удалось соединиться с сервером");
            }
        }
    }


    public partial class Srv
    {

        // запись объекта в XML-формате
        public static int WriteXMLObj(System.Type xType, object x, string s)
        {
            return (WriteXMLObj(xType, null, x, s, false));
        }

        // запись объекта в XML-формате
        public static int WriteXMLObjTxt(System.Type xType, object x, string s)
        {
            return (WriteXMLObj(xType, null, x, s, true));
        }

        // запись объекта в XML-формате
        public static int WriteXMLObj(Type xType, Type[] aTypes, object x, string s, bool bTxt)
        {
            int ret = AppC.RC_OK;
            FileStream fs = null;
            XmlWriter writer;

            try
            {
                fs = new FileStream(s, FileMode.Create);
                XmlSerializer serializer = (aTypes == null) ? new XmlSerializer(xType) :
                    new XmlSerializer(xType, aTypes);

                XmlWriterSettings xWS = new XmlWriterSettings();
                xWS.Encoding = Encoding.Unicode;
                if (bTxt)
                {
                    xWS.Indent = true;
                }
                writer = XmlTextWriter.Create(fs, xWS);
                serializer.Serialize(writer, x);
                writer.Close();
            }
            catch (Exception ex)
            {
                string se = ex.Message;
                ret = AppC.RC_NOFILE;
            }
            finally
            {
                if (fs != null)
                    fs.Close();
            }
            return (ret);
        }

        public static int ReadXMLObj(System.Type xType, out object x, string s)
        {
            return(ReadXMLObj(xType, out x, s, false));
        }

        // чтение сохраненного объекта из XML
        public static int ReadXMLObj(System.Type xType, out object x, string s, bool bStrAsXML)
        {
            int ret = AppC.RC_OK;
            FileStream fs = null;
            XmlSerializer serializer = null;
            XmlReader xmlR;

            x = null;
            try
            {
                serializer = new XmlSerializer(xType);
                if (bStrAsXML)
                {// sSig и есть XML
                    xmlR = XmlReader.Create(new StringReader(s));
                }
                else
                {// sSig и есть XML
                    fs = new FileStream(s, FileMode.Open);
                    xmlR = XmlReader.Create(fs);
                }
                x = serializer.Deserialize(xmlR);
                xmlR.Close();
            }
            catch (Exception ex)
            {
                string ms = ex.Message;
                ret = AppC.RC_NOFILE;
            }
            finally
            {
                if (fs != null)
                    fs.Close();
            }

            return (ret);
        }

        public static string Byte2Hex(byte[] aB)
        {
            string s = "";
            for (int i = 0; i < aB.Length; i++)
            {
                s += aB[i].ToString("X2");
            }
            return (s);
        }

        public static byte[] Hex2Byte(string s)
        {
            string s1b;
            int j = 0;
            byte[] aB = new byte[s.Length / 2];
            for (int i = 0; i < s.Length; i = i + 2)
            {
                s1b = s.Substring(i, 2);
                aB[j++] = byte.Parse(s1b, System.Globalization.NumberStyles.HexNumber);
            }
            return (aB);
        }

        // перевод строки в число с произвольной дробной частью
        public static FRACT Str2VarDec(string sN)
        {
            int
                nDec,
                nSt = 0;
            FRACT
                ret = 0;

            try
            {
                nSt = int.Parse(sN.Substring(0, 1));
                sN = sN.Substring(1);
                ret = (FRACT)(int.Parse(sN));
                if (nSt > 0)
                {
                    nDec = 1;
                    while (nSt > 0)
                    {
                        nDec = nDec * 10;
                        nSt--;
                    }
                    ret = ret / nDec;
                }
            }
            catch
            {
            }

            return (ret);
        }


        public static object[] AppVerDT()
        {
            Version
                version = Assembly.GetExecutingAssembly().GetName().Version;
            var 
                buildDate = new DateTime(2000, 1, 1).AddDays(version.Build).AddSeconds(version.Revision * 2);
            string
                sVer = String.Format("{0}.{1}", version.Major, version.Minor);
            object[]
                ret = new object[] {
                    String.Format("{0} {1}", sVer, buildDate.ToString("dd.MM.yyyy")),
                    sVer,
                    buildDate
                    };
            return (ret);
        }


        // контрольная сумма по модулю 10
        public static string CheckSumModul10(string sBarCode)
        {
            int
                i, fak, sum;
            string
                r;


            sum = 0;
            fak = sBarCode.Length;
            for (i = 0; i < sBarCode.Length; i++)
            {
                if ((fak % 2) == 0)
                {
                    sum += int.Parse(sBarCode[i].ToString());
                }
                else
                {
                    sum += (int.Parse(sBarCode[i].ToString()) * 3);
                }
                fak--;
            }
            if ((sum % 10) == 0)
                r = sBarCode + "0";
            else
                r = sBarCode + (10 - (sum % 10)).ToString();
            return (r);
        }

        public class ExprAct
        {
            public string ExpCode;

            public ExprDll.Expr ExprVal;
            public ExprDll.Action ActionVal;
            public ExprAct(ExprDll.Expr e, ExprDll.Action a)
            {
                ExprVal = e;
                ActionVal = a;
            }
        }




        //public static void LoadInterCode(out ExprDll.Expr xG, Dictionary<string, Srv.ExprAct> xD,
        //     SkladAll.NSIAll.TableDef t)
        //{
        //    string
        //        sBCode,
        //        sCurBlk = "";
        //    ExprDll.Action
        //        xAct;

        //    xG = new ExprDll.Expr();
        //    if (t.nState == SkladAll.NSIAll.DT_STATE_READ)
        //    {
        //        xD.Clear();
        //        foreach (DataRow dr in t.dt.Rows)
        //        {
        //            try
        //            {
        //                sCurBlk = (string)dr["KD"];
        //                sBCode = (string)dr["MD"];

        //                using (System.IO.StreamWriter sw = new System.IO.StreamWriter(@"\PspCode.txt"))
        //                {
        //                    sw.Write(sBCode);
        //                    sw.Close();
        //                }

        //                xAct = xG.Parse(sBCode);
        //                xG.run.AddModule(xAct);
        //                xD.Add(sCurBlk, new Srv.ExprAct(xG, xAct));
        //            }
        //            catch (Exception ex)
        //            {
        //                Srv.ErrorMsg(ex.Message, sCurBlk + "-трансляция! ", false);
        //            }
        //        }
        //    }

        //}


        public static void LoadInterCode(out ExprDll.Expr xG, Dictionary<string, Srv.ExprAct> xD,
             SkladAll.NSIAll.TableDef t)
        {
            string
                sBCode,
                sCurBlk;
            ExprDll.Action
                xAct;

            xG = new ExprDll.Expr();
            if (t.nState == SkladAll.NSIAll.DT_STATE_READ)
            {
                xD.Clear();
                foreach (DataRow dr in t.dt.Rows)
                {
                    if ((dr["KD"] is string) && (dr["MD"] is string))
                    {
                        sCurBlk = (string)dr["KD"];
                        sBCode = (string)dr["MD"];
                        try
                        {
                            //using (System.IO.StreamWriter sw = new System.IO.StreamWriter(@"\PspCode.txt"))
                            //{
                            //    sw.Write(sBCode);
                            //    sw.Close();
                            //}
                            xAct = xG.Parse(sBCode);
                            xG.run.AddModule(xAct);
                            xD.Add(sCurBlk, new Srv.ExprAct(xG, xAct));
                        }
                        catch (Exception ex)
                        {
                            Srv.ErrorMsg(ex.Message, sCurBlk + "-трансляция! ", false);
                        }
                    }
                }
            }

        }


        // для произвольного обмена параметрами
        public class ExchangeContext
        {
            // заголовок формы
            public static string sHeadLine;

            // происхождение и цель обмена
            public static AppC.EXCHG_RSN ExchgReason = AppC.EXCHG_RSN.NO_EXCHG;

            // команда для обмена
            public static string CMD_EXCHG;

            // код бланка
            public static string sBlankCode;
            // параметры-контролы бланка в XML-формате
            public static string sBlankParsXML;

            public static string sPrinterSTC;
            public static string sPrinterMOB;
            public static int FlagDetailRows;
            public static DataRow dr4Prn = null;

            // текущий обработчик событий
            public static ExprAct xEA = null;

            // текущий режим работы
            public AppC.APPMODE AMode;

            public ExchangeContext() { }

            public ExchangeContext(ExchangeContext x) 
            {
                AMode = x.AMode;
                //BlankCode = x.BlankCode;
                //CMD_EXCHG = x.CMD_EXCHG;
                //dr4Prn = x.dr4Prn;
                //ExchgReason = x.ExchgReason;
                //FlagDetailRows = x.FlagDetailRows;
                //sBlankParsXML = x.sBlankParsXML;
                //sHeadLine = x.sHeadLine;
                //sPrinterMOB = x.sPrinterMOB;
                //sPrinterSTC = x.sPrinterSTC;
                //xEA = x.xEA;
            }
        }


        // упрощенный ввод даты
        public static string SimpleDateTime(string sD)
        {
            return( SimpleDateTime(sD, DateTime.Now) );
        }

        // упрощенный ввод даты
        public static string SimpleDateTime(string sD, DateTime dCurr)
        {
            string sRet = sD;
            try
            {
                DateTime d = DateTime.ParseExact(sD, "dd.MM.yy", null);
            }
            catch
            {
                string sCur = dCurr.ToString("dd.MM.yy");
                try
                {
                    string[] aS = sD.Split(new char[] { '.' });
                    if (aS.Length == 1)
                    {// только день, месяц и год по умолчанию
                        //sRet = String.Format("{0:D2}", aS[0]) + sCur.Substring(2);
                        sRet = aS[0].PadLeft(2, '0') + sCur.Substring(2);
                    }
                    else if (aS.Length == 2)
                    {// день и месяц, год по умолчанию
                        //sRet = String.Format("{0:D2}.{1:D2}", aS[0], aS[1]) + sCur.Substring(5);
                        sRet = aS[0].PadLeft(2, '0') + "." + aS[1].PadLeft(2, '0') + sCur.Substring(5);
                    }
                    else if (aS.Length == 3)
                    {
                        if (aS[2].Length > 2)
                            aS[2] = aS[2].Substring(2);
                        //sRet = String.Format("{0:D2}.{1:D2}.{2:D2}", aS[0], aS[1], aS[2]);
                        sRet = aS[0].PadLeft(2, '0') + "." + aS[1].PadLeft(2, '0') + "." + aS[2].PadLeft(2, '0');
                    }
                    else
                        sRet = sCur;

                }
                catch { sRet = sCur; }
            }
            return (sRet);
        }


        // интервал времени в секундах
        public static string TimeDiff(int t1, int t2)
        {
            TimeSpan tsDiff = new TimeSpan(0, 0, 0, 0, t2 - t1);
            return ( TimeDiff(t1, t2, 3) );
        }


        // интервал времени в секундах
        public static string TimeDiff(int t1, int t2, int nDec)
        {
            TimeSpan tsDiff = new TimeSpan(0, 0, 0, 0, t2 - t1);
            return ( Math.Round(tsDiff.TotalSeconds, nDec).ToString());
        }

        // обработчик клавиш для текущей функции
        public delegate bool CurrFuncKeyHandler(object nF, KeyEventArgs e, ref CurrFuncKeyHandler kh);

        public class HelpShow
        {
            
            internal const int 
                HELPLINES = 19,                             // строк в окне Help
                SHOW_MANY = 1,
                SHOW_ONE = 2,
                MStrHeight = 15;
            
            internal const string
                HelpStr = "Enter-дальше     Esc-закрыть",    // подсказка в нижней строке
                EmpArr  = "    ",
                RArrow  = "  ->",
                LArrow  = "<-  ";

            private System.Windows.Forms.Panel 
                m_Panel;
            private System.Windows.Forms.Label 
                m_HelpStr = null;
            private System.Windows.Forms.TextBox 
                m_MainHelp;

            private Control
                xParent = null,
                xBeforeFocused = null;

            private object 
                xInf;                                       // текущая инфо

            private bool 
                m_IsArray;                                  // тип представлеия: массив или список
                
            private int
                m_WinMode = SHOW_MANY,
                nMaxLines = HELPLINES,                      // текущий размер окна вывода в строках
                nHelpInd = 0;                               // текущий индекс вывода

            private System.Single
                m_ssFSize = 9F;

            
            private CurrFuncKeyHandler
                ehThis = null;                              // обработчик клавиш для текущей функции

            public HelpShow() : this(null) { }

            public HelpShow(Control xP)
            {
                //CreateHelpPanel();
                Rectangle
                    screen = Screen.PrimaryScreen.Bounds;
                if ((screen.Height == 240) && (screen.Width == 320))
                    nMaxLines = 14;
                else
                    nMaxLines = HELPLINES;

                xParent = xP;
                CreateHelpPanel(screen, nMaxLines, 15);
                ehThis = new CurrFuncKeyHandler(HelpKeyDown);
                m_WinMode = SHOW_MANY;
            }

            public HelpShow(Control xP, Rectangle screen, int nMaxLinesInf, System.Single FontSize, int HelpStringHeight)
            {
                xParent = xP;
                nMaxLines = nMaxLinesInf;          // текущий размер окна вывода в строках
                m_ssFSize = FontSize;
                CreateHelpPanel(screen, nMaxLines, HelpStringHeight);
                ehThis = new CurrFuncKeyHandler(HelpKeyDown);
                m_WinMode = SHOW_ONE;
            }

            //private Panel CreateHelpPanel()
            //{
            //    const int 
            //        nHStrHeight = 15;
            //    Rectangle 
            //        screen = Screen.PrimaryScreen.Bounds;

            //    if ((screen.Height == 240) && (screen.Width == 320))
            //        nMaxLines = 14;
            //    else
            //        nMaxLines = HELPLINES;

            //    //m_HelpStr = new Label();
            //    // 
            //    // Строка подсказки
            //    // 
            //    AjustHelpString(screen, nHStrHeight);

            //    // 
            //    // Окно вывода
            //    // 
            //    //m_MainHelp = new TextBox();
            //    //m_MainHelp.BackColor = System.Drawing.Color.Lavender;
            //    //m_MainHelp.Font = new System.Drawing.Font("Courier New", 9F, System.Drawing.FontStyle.Regular);
            //    //m_MainHelp.Location = new System.Drawing.Point(2, 3);
            //    //m_MainHelp.Multiline = true;
            //    //m_MainHelp.Name = "m_MainHelp";
            //    ////m_MainHelp.Size = new System.Drawing.Size(236, 296);
            //    //m_MainHelp.Size = new System.Drawing.Size(screen.Width - 4, screen.Height - nHStrHeight - 9);
            //    //m_MainHelp.TabIndex = 0;
            //    //m_MainHelp.WordWrap = false;

            //    AjustMainHelp(screen, nHStrHeight);

            //    // 
            //    // pnHelp
            //    // 
            //    m_Panel = new Panel();
            //    m_Panel.SuspendLayout();
            //    m_Panel.Name = "pnHelp";
            //    m_Panel.Controls.Add(m_HelpStr);
            //    m_Panel.Controls.Add(m_MainHelp);
            //    m_Panel.BackColor = System.Drawing.Color.MediumBlue;
            //    //m_Panel.Size = new System.Drawing.Size(240, 320);

            //    m_Panel.Size = new System.Drawing.Size(screen.Width, screen.Height);
            //    //m_Panel.Location = new Point((PanelRect.Width - this.Width) / 2,
            //    //    (PanelRect.Height - this.Height) / 2);
            //    m_Panel.Location = new Point(0, 0);

            //    m_Panel.Visible = false;

            //    // 
            //    // Подсказка
            //    // 
            //    //m_HelpStr.Name = "m_HelpStr";
            //    //m_HelpStr.ForeColor = System.Drawing.Color.WhiteSmoke;

            //    //m_HelpStr.Size = new System.Drawing.Size((int)(m_Panel.Width * 0.8), nHStrHeight);
            //    //m_HelpStr.Location = new System.Drawing.Point((screen.Width - m_HelpStr.Width) / 2,
            //    //    screen.Height - nHStrHeight - (nHStrHeight / 2));

            //    //m_HelpStr.Text = HelpStr;
            //    //m_HelpStr.TextAlign = System.Drawing.ContentAlignment.TopCenter;

            //    m_Panel.ResumeLayout();

            //    return (m_Panel);
            //}

            // основное окно вывода
            private void AjustMainHelp(Rectangle screen, int HelpStringHeight)
            {
                // 
                // Окно вывода
                // 
                m_MainHelp = new TextBox();

                m_MainHelp.BackColor = System.Drawing.Color.Lavender;
                m_MainHelp.Font = new System.Drawing.Font("Courier New", m_ssFSize, System.Drawing.FontStyle.Regular);
                m_MainHelp.Location = new System.Drawing.Point(2, 3);
                m_MainHelp.Multiline = true;
                m_MainHelp.Name = "m_MainHelp";
                //m_MainHelp.Size = new System.Drawing.Size(236, 296);
                //m_MainHelp.Size = new System.Drawing.Size(screen.Width - 4, screen.Height - HelpStringHeight - (int)m_ssFSize);
                m_MainHelp.Size = new System.Drawing.Size(screen.Width - 4, screen.Height - HelpStringHeight - 6);
                m_MainHelp.TabIndex = 0;
                m_MainHelp.WordWrap = false;

                m_MainHelp.TextAlign = HorizontalAlignment.Left;
            }

            // строка подсказки
            private void AjustHelpString(Rectangle screen, int nHStrHeight)
            {
                if (nHStrHeight > 0)
                {
                    m_HelpStr = new Label();

                    // 
                    // Подсказка
                    // 
                    m_HelpStr.Name = "m_HelpStr";
                    m_HelpStr.ForeColor = System.Drawing.Color.WhiteSmoke;

                    m_HelpStr.Size = new System.Drawing.Size((int)(m_Panel.Width * 0.99), nHStrHeight);
                    m_HelpStr.Location = new System.Drawing.Point((screen.Width - m_HelpStr.Width) / 2,
                        screen.Height - nHStrHeight - (nHStrHeight / 2));

                    m_HelpStr.Text = HelpStr;
                    m_HelpStr.TextAlign = System.Drawing.ContentAlignment.TopCenter;
                }
            }

            // панель помощи
            private Panel CreateHelpPanel(Rectangle screen, int nMaxLines, int HelpStringHeight)
            {
                // 
                // pnHelp
                // 
                m_Panel = new Panel();
                m_Panel.SuspendLayout();
                m_Panel.Name = "pnHelp";
                m_Panel.BackColor = System.Drawing.Color.MediumBlue;

                m_Panel.Size = new System.Drawing.Size(screen.Width, screen.Height);
                m_Panel.Location = screen.Location;
                m_Panel.Visible = false;

                // 
                // Окно вывода
                // 
                AjustMainHelp(screen, HelpStringHeight);
                m_Panel.Controls.Add(m_MainHelp);

                if (HelpStringHeight > 0)
                {
                    // 
                    // Подсказка
                    // 
                    AjustHelpString(screen, HelpStringHeight);
                    m_Panel.Controls.Add(m_HelpStr);
                    m_HelpStr.BringToFront();
                }
                m_Panel.ResumeLayout();
                return (m_Panel);
            }

            public void ShowInfo(object xI, ref CurrFuncKeyHandler khCurr)
            {
                ShowInfo(null, null, xI, ref khCurr);
            }


            public void ShowInfo(Control xPrnt, Control xBF, object xI, ref CurrFuncKeyHandler khCurr)
            {
                bool
                    bIs1st,
                    bIsLast;

                if (xPrnt != null)
                    xParent = xPrnt;
                if (xBF != null)
                    xBeforeFocused = xBF;
                xInf = xI;

                xParent.Controls.Add(m_Panel);
                m_Panel.SuspendLayout();
                nHelpInd = 0;

                if (xInf.GetType().IsArray == true)
                {
                    m_IsArray = true;
                    m_MainHelp.Text = ((string[])xInf)[0];
                    bIs1st = true;
                    bIsLast = (((string[])xInf).Length == nHelpInd + 1) ? true : false;
                }
                else
                {
                    m_IsArray = false;
                    m_MainHelp.Text = NextInfPart(out bIs1st, out bIsLast);
                }
                //m_HelpStr.Text = (!bIs1st) ? LArrow : "  " + HelpStr;
                //m_HelpStr.Text += (!bIsLast) ? RArrow : "  ";
                FullHelpLine(bIs1st, bIsLast);

                m_Panel.Visible = true;
                m_Panel.BringToFront();

                m_Panel.ResumeLayout();

                khCurr += ehThis;
            }

            private string NextInfPart(out bool IsFirstPart, out bool IsLastPart)
            {
                string
                    sRet = "";
                int
                    nStart = (nHelpInd * nMaxLines),
                    nEnd;

                if (nStart >= ((List<string>)xInf).Count)
                {
                    nHelpInd = 0;
                    nStart = 0;
                }
                nEnd = Math.Min(((List<string>)xInf).Count, nStart + nMaxLines);

                for (int i = nStart; i < nEnd; i++)
                    sRet += ((List<string>)xInf)[i] + "\r\n";
                if (sRet.Length > 0)
                    sRet = sRet.Remove(sRet.Length - 2, 2);

                IsFirstPart = (nStart == 0) ? true : false;
                IsLastPart = (nEnd < ((List<string>)xInf).Count) ? false : true;

                return (sRet);
            }

            private void FullHelpLine(bool bIs1st, bool bIsLast)
            {
                if (m_HelpStr != null)
                {
                    m_HelpStr.Text = (!bIs1st) ? LArrow : EmpArr;
                    m_HelpStr.Text += HelpStr;
                    m_HelpStr.Text += (!bIsLast) ? RArrow : EmpArr;
                }
            }

            private bool HelpKeyDown(object nF, KeyEventArgs e, ref CurrFuncKeyHandler kh)
            {
                int 
                    nFunc = (int)nF;
                bool
                    bKeyHandled = true,
                    bCloseHelp = false;
                bool
                    bIs1st,
                    bIsLast;
                string 
                    sH = "";

                if (nFunc > 0)
                {
                    bCloseHelp = true;
                    if (nFunc != PDA.Service.AppC.F_HELP)
                        bKeyHandled = false;
                }
                else
                {
                    switch (e.KeyValue)
                    {
                        case W32.VK_TAB:
                            m_MainHelp.WordWrap = !m_MainHelp.WordWrap;
                            break;
                        case W32.VK_ESC:
                            bCloseHelp = true;
                            break;
                        case W32.VK_ENTER:
                            nHelpInd++;
                            if (m_IsArray)
                            {
                                if (nHelpInd == ((string[])xInf).Length)
                                {
                                    nHelpInd = 0;
                                }
                                sH = ((string[])xInf)[nHelpInd];
                                bIs1st = (nHelpInd == 0) ? true : false;
                                bIsLast = (((string[])xInf).Length == nHelpInd + 1) ? true : false;
                            }
                            else
                                sH = NextInfPart(out bIs1st, out bIsLast);

                            //m_HelpStr.Text = (!bIs1st) ? LArrow : "  ";
                            //m_HelpStr.Text += HelpStr;
                            //m_HelpStr.Text += (!bIsLast) ? RArrow : "  ";
                            FullHelpLine(bIs1st, bIsLast);

                            m_MainHelp.Text = sH;
                            break;
                        default:
                            bKeyHandled = false;
                            break;
                    }
                }
                if (m_WinMode == SHOW_ONE) 
                {
                    bCloseHelp = true;
                    bKeyHandled = false;
                }

                if (bCloseHelp == true)
                {
                    StopShow(ref kh);
                }

                e.Handled = bKeyHandled;
                return (bKeyHandled);
            }

            public void StopShow(ref CurrFuncKeyHandler kh)
            {

                if (m_Panel.Visible)
                {
                    m_Panel.SuspendLayout();
                    m_Panel.Visible = false;
                    m_Panel.SendToBack();
                    if (xParent != null)
                        xParent.Controls.Remove(m_Panel);
                    m_Panel.ResumeLayout();
                    kh -= ehThis;
                    if (xBeforeFocused != null)
                        xBeforeFocused.Focus();
                    xBeforeFocused = null;
                }
            }

            public Control PanelParent
            {
                get { return xParent; }
                set { xParent = value; }
            }

            // куда вернуть фокус
            public Control PreviosControl
            {
                get { return xBeforeFocused; }
                set { xBeforeFocused = value; }
            }

            public Control ThisPanel
            {
                get { return m_Panel; }
            }


        }


        public class PicShow
        {
            // типы картинок
            public enum PICTYPE : int
            {
                BMP = 1,
                JPG = 2,
                PNG = 3,
                OTHERS = 4
            }

            // типы картинок
            public enum PICSRCTYPE : int
            {
                FILE = 1,
                FOLDER = 2,
                BASE64 = 3
            }

            private System.Windows.Forms.Panel
                m_Panel;
            private System.Windows.Forms.PictureBox
                m_MainPic;
            private Bitmap
                m_BitmapCurr = null;

            private Control
                xParent = null,
                m_FocusAfter = null;


            // входные параметры
            private string[]
                m_ListPics = null;
            PICSRCTYPE
                m_pSrcType = PICSRCTYPE.BASE64;
            PICTYPE
                ptType = PICTYPE.OTHERS;

            private int
                m_MaxPic = 0,
                m_PicInd = 0;                               // текущий индекс вывода

            private byte[]
                m_PicArray = null;


            private Srv.CurrFuncKeyHandler
                ehThis = null;                              // обработчик клавиш для текущей функции

            public PicShow() : this(null) { }

            public PicShow(Control xP)
            {
                Rectangle
                    screen = Screen.PrimaryScreen.Bounds;

                xParent = xP;
                CreatePicPanel(screen);
                ehThis = new Srv.CurrFuncKeyHandler(HelpKeyDown);

            }

            public PicShow(Control xP, Rectangle screen)
            {
                xParent = xP;
                CreatePicPanel(screen);
                ehThis = new Srv.CurrFuncKeyHandler(HelpKeyDown);
            }


            // панель картинки
            private Panel CreatePicPanel(Rectangle screen)
            {
                m_Panel = new Panel();
                m_Panel.SuspendLayout();
                m_Panel.Name = "pnPicture";
                m_Panel.BackColor = System.Drawing.Color.MediumBlue;
                m_Panel.Size = new System.Drawing.Size(screen.Width, screen.Height);
                m_Panel.Location = screen.Location;
                m_Panel.Visible = false;

                // 
                // Окно вывода картинки
                // 
                m_MainPic = new PictureBox();
                m_MainPic.Name = "m_MainPic";
                m_MainPic.SizeMode = PictureBoxSizeMode.StretchImage;
                //m_MainPic.TabIndex = 0;
                m_MainPic.Size = new System.Drawing.Size(screen.Width, screen.Height);
                m_MainPic.Location = new System.Drawing.Point(0, 0);

                m_Panel.Controls.Add(m_MainPic);

                m_Panel.ResumeLayout();
                return (m_Panel);
            }

            public void ShowInfo(object xI, ref Srv.CurrFuncKeyHandler khCurr)
            {
                ShowInfo(xI, ref khCurr, PICSRCTYPE.FILE, null, null);
            }


            public void ShowInfo(object xI, ref Srv.CurrFuncKeyHandler khCurr, PICSRCTYPE PicSrc, Control xPrnt, Control xBF)
            {

                if (xPrnt != null)
                    xParent = xPrnt;
                if (xBF != null)
                    m_FocusAfter = xBF;

                m_pSrcType = PicSrc;

                if ((xParent != null) && (!xParent.Controls.Contains(m_Panel)))
                    xParent.Controls.Add(m_Panel);

                m_Panel.SuspendLayout();
                m_PicInd = 0;

                if (xI.GetType().IsArray == true)
                {
                    m_ListPics = (string[])xI;
                    m_MaxPic = m_ListPics.Length;
                }
                else
                {
                    m_ListPics = new string[] { (string)xI };
                    m_MaxPic = 1;
                }
                ShowPic(m_ListPics[m_PicInd], m_pSrcType);

                m_Panel.Visible = true;
                m_Panel.BringToFront();

                m_Panel.ResumeLayout();

                khCurr += ehThis;
            }


            private void ShowPic(string sFile, PICSRCTYPE PicSrc)
            {
                IntPtr
                    ipHBM = IntPtr.Zero;

                if (m_BitmapCurr != null)
                    m_BitmapCurr.Dispose();

                if (PicSrc == PICSRCTYPE.BASE64)
                {
                    using (MemoryStream ms = new MemoryStream())
                    {
                        m_PicArray = Convert.FromBase64String(sFile);
                        ms.Write(m_PicArray, 0, m_PicArray.Length);
                        m_BitmapCurr = new Bitmap(ms);
                    }
                }
                else if (PicSrc == PICSRCTYPE.FILE)
                {
                    sFile = sFile.ToUpper();
                    ptType = PICTYPE.OTHERS;

                    if (sFile.IndexOf("BMP", StringComparison.CurrentCulture) > 0)
                        ptType = PICTYPE.BMP;
                    else if (sFile.IndexOf("JPG", StringComparison.CurrentCulture) > 0)
                        ptType = PICTYPE.JPG;
                    else if (sFile.IndexOf("PNG", StringComparison.CurrentCulture) > 0)
                        ptType = PICTYPE.PNG;

                    m_BitmapCurr = new Bitmap(sFile);
                    using (MemoryStream ms = new MemoryStream())
                    {
                        m_BitmapCurr.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
                        m_PicArray = ms.ToArray();
                    }
                }
                m_MainPic.Image = m_BitmapCurr;
            }

            private bool HelpKeyDown(object nF, KeyEventArgs e, ref Srv.CurrFuncKeyHandler kh)
            {
                int
                    nFunc = (int)nF;
                bool
                    bKeyHandled = true,
                    bCloseHelp = false;

                if (nFunc > 0)
                {
                    bCloseHelp = true;
                    if (nFunc != PDA.Service.AppC.F_HELP)
                        bKeyHandled = false;
                }
                else
                {
                    switch (e.KeyValue)
                    {
                        case W32.VK_ESC:
                            bCloseHelp = true;
                            break;
                        case W32.VK_ENTER:
                            m_PicInd++;
                            if (m_PicInd == m_MaxPic)
                            {
                                if (m_MaxPic == 1)
                                {
                                    bCloseHelp = true;
                                    break;
                                }
                                m_PicInd = 0;
                            }
                            ShowPic(m_ListPics[m_PicInd], m_pSrcType);
                            //bCloseHelp = true;
                            break;
                        default:
                            bKeyHandled = false;
                            break;
                    }
                }

                if (bCloseHelp == true)
                {
                    StopShow(ref kh);
                }

                e.Handled = bKeyHandled;
                return (bKeyHandled);
            }

            public void StopShow(ref Srv.CurrFuncKeyHandler kh)
            {

                if (m_Panel.Visible)
                {
                    m_Panel.SuspendLayout();
                    m_Panel.Visible = false;
                    m_Panel.SendToBack();
                    if (xParent != null)
                        xParent.Controls.Remove(m_Panel);
                    m_Panel.ResumeLayout();
                    kh -= ehThis;
                    if (m_FocusAfter != null)
                        m_FocusAfter.Focus();
                    m_FocusAfter = null;
                }
            }

            public Control PanelParent
            {
                get { return xParent; }
                set { xParent = value; }
            }

            // куда вернуть фокус
            public Control PreviosControl
            {
                get { return m_FocusAfter; }
                set { m_FocusAfter = value; }
            }

            public Control ThisPanel
            {
                get { return m_Panel; }
            }

            public byte[] PicArray
            {
                get { return m_PicArray; }
            }

        }


        // Поиск активного Control на вкладке
        public static Control GetPageControl(TabPage page, int nWhatFind)
        {
            Control
                xLast = null;

            foreach (Control ctl in page.Controls)
            {
                xLast = ctl;
                if (nWhatFind == 0)
                {// найти первый
                    if (ctl.TabIndex == 0)
                        return ctl;
                }
                else
                {// найти текущий
                    if (ctl.Focused)
                        return ctl;
                }
            }
            if (xLast != null)
                xLast.Focus();
            return (xLast);
        }


        // запись одного значения
        public static bool WriteRegInfo(string Key, string KeyValue, string sAppInf)
        {
            try
            {
                string sKeyPath = @"HKEY_CURRENT_USER\Software\OAOSP\" + sAppInf;
                Registry.SetValue(sKeyPath, Key, KeyValue);
                return true;
            }
            catch { return false; }
        }


        [DllImport("coredll.dll", CharSet = CharSet.Auto)]
        extern static void MessageBeep(uint BeepType);

        public static void PlayMelody(uint nSoundType)
        {
#if DOLPH7850
            Thread thScan;
            thScan = new Thread(new ThreadStart(W32.BeepByKey));
            thScan.Start();
#else
            MessageBeep(nSoundType);
#endif
        }

        public static void ErrorMsg(string sE)
        {
            MessageBox.Show(sE, "Ошибка!");
        }
        public static void ErrorMsg(string sE, bool bSound)
        {
            PlayMelody(W32.MB_3GONG_EXCLAM);
            MessageBox.Show(sE, "Ошибка!");
        }
        public static void ErrorMsg(string sE, string sH, bool bSound)
        {
            PlayMelody(W32.MB_3GONG_EXCLAM);
            MessageBox.Show(sE, sH);
        }

        // нажали цифру или нет
        public static bool IsDigKey(KeyEventArgs e, ref int nNum)
        {
            bool bRet = AppC.RC_OKB;
            switch (e.KeyValue)
            {
                case W32.VK_D0:
                    nNum = 0;
                    break;
                case W32.VK_D1:
                    nNum = 1;
                    break;
                case W32.VK_D2:
                    nNum = 2;
                    break;
                case W32.VK_D3:
                    nNum = 3;
                    break;
                case W32.VK_D4:
                    nNum = 4;
                    break;
                case W32.VK_D5:
                    nNum = 5;
                    break;
                case W32.VK_D6:
                    nNum = 6;
                    break;
                case W32.VK_D7:
                    nNum = 7;
                    break;
                case W32.VK_D8:
                    nNum = 8;
                    break;
                case W32.VK_D9:
                    nNum = 9;
                    break;
                default:
                    bRet = AppC.RC_CANCELB;
                    break;
            }
            return (bRet);
        }

        private static byte IsArrow(KeyEventArgs e)
        {
            byte ret = 0;
            switch (e.KeyValue)
            {
                case 56:                // Up
                    ret = 38;
                    break;
                case 50:                // Down
                    ret = 40;
                    break;
                case 52:                // Left
                    ret = 37;
                    break;
                case 54:                // Right
                    ret = 39;
                    break;
                case 53:
                    ret = 13;
                    break;
            }
            return (ret);
        }

        // Разбор команды от сервера
        public static Dictionary<string, string> SrvAnswerParParse(string sC)
        {
            return( SrvAnswerParParse(sC, new char[] { ';' }));
        }


        // Разбор команды от сервера
        public static Dictionary<string, string> SrvAnswerParParse(string sC, char[] aC)
        {
            int j = 0;
            string k, v;
            Dictionary<string, string> dicSrvComm = new Dictionary<string, string>();

            string[] saPars = sC.Split(aC);
            for (int i = 0; i < saPars.Length; i++)
            {
                if (saPars[i] != "")
                {
                    j = saPars[i].IndexOf('=');
                    if (j > 0)
                    {
                        k = saPars[i].Substring(0, j).Trim();
                        try
                        {
                            v = saPars[i].Substring(j + 1).Trim();
                        }
                        catch { v = ""; }
                        dicSrvComm.Add(k, v);
                    }
                }
            }
            return (dicSrvComm);
        }

        public class Collect4Show<T> : IEnumerable<T>
        {
            // направления перемещения по коллекции
            public enum DIR_MOVE
            {
                FORWARD,
                BACK
            }

            // The enumerator definition.
            class IntrEnumr : IEnumerator<T>
            {
                private int
                    _position;
                private Collect4Show<T>
                    _collection;
                private bool
                    disposed = false;

                //private T 
                //    current;

                public IntrEnumr(Collect4Show<T> coll)
                {
                    System.Threading.Monitor.Enter(coll._ArrayItemsT.SyncRoot);
                    this._position = -1;
                    this._collection = coll;
                }

                public T Current
                {
                    get
                    {
                        try
                        {
                            return _collection._ArrayItemsT[_position];
                        }
                        catch (IndexOutOfRangeException)
                        {
                            throw new InvalidOperationException();
                        }
                    }
                }

                object IEnumerator.Current
                {
                    get { return Current; }
                }

                public bool MoveNext()
                {
                    if (_position < _collection._ArrayItemsT.Length)
                        _position++;
                    return (_position < _collection._ArrayItemsT.Length);
                }

                public void Reset()
                {
                    _position = -1;
                }

                public void Dispose()
                {
                    Dispose(true);
                    // This object will be cleaned up by the Dispose method.
                    // Therefore, you should call GC.SupressFinalize to
                    // take this object off the finalization queue
                    // and prevent finalization code for this object
                    // from executing a second time.
                    GC.SuppressFinalize(this);
                }

                // Dispose(bool disposing) executes in two distinct scenarios.
                // If disposing equals true, the method has been called directly
                // or indirectly by a user's code. Managed and unmanaged resources
                // can be disposed.
                // If disposing equals false, the method has been called by the
                // runtime from inside the finalizer and you should not reference
                // other objects. Only unmanaged resources can be disposed.
                private void Dispose(bool disposing)
                {
                    // Check to see if Dispose has already been called.
                    if (!this.disposed)
                    {
                        // If disposing equals true, dispose all managed
                        // and unmanaged resources.
                        if (disposing)
                        {
                            // Dispose managed resources.
                            try
                            {
                                //current = default(T);
                                _position = _collection._ArrayItemsT.Length;
                            }
                            finally
                            {
                                System.Threading.Monitor.Exit(_collection._ArrayItemsT.SyncRoot);
                            }
                        }
                        // Call the appropriate methods to clean up
                        // unmanaged resources here.
                        // If disposing is false,
                        // only the following code is executed.

                        // Note disposing has been done.
                        disposed = true;
                    }
                }

                ~IntrEnumr()
                {
                    // Do not re-create Dispose clean-up code here.
                    // Calling Dispose(false) is optimal in terms of
                    // readability and maintainability.
                    Dispose(false);
                }



                public int CurrPos
                {
                    get { return _position; }
                    set { _position = value; }
                }


            }



            private T[]
                _ArrayItemsT;
            private IntrEnumr
                _xIntrEnumr;

            public Collect4Show(T[] arr)
            {
                _ArrayItemsT = new T[arr.Length];
                for (int i = 0; i < arr.Length; i++)
                    _ArrayItemsT[i] = arr[i];
                _xIntrEnumr = new IntrEnumr(this);
            }

            public IEnumerator<T> GetEnumerator()
            {
                return _xIntrEnumr;
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }

            public T MoveEx(DIR_MOVE d)
            {
                if (_ArrayItemsT.Length == 0)
                    return (default(T));

                if (d == DIR_MOVE.FORWARD)
                {// двигаемся вперед
                    if (_xIntrEnumr.CurrPos == _ArrayItemsT.Length - 1)
                        _xIntrEnumr.CurrPos = 0;
                    else
                        _xIntrEnumr.CurrPos++;
                }
                else
                {// двигаемся назад
                    if (_xIntrEnumr.CurrPos == 0)
                        _xIntrEnumr.CurrPos = _ArrayItemsT.Length - 1;
                    else
                        _xIntrEnumr.CurrPos--;
                }
                return ((T)_xIntrEnumr.Current);
            }

            public int Count
            {
                get { return _ArrayItemsT.Length; }
            }

            public object Current
            {
                get 
                {
                    object z = null;
                    if ((_xIntrEnumr.CurrPos >= 0) && (_xIntrEnumr.CurrPos < Count))
                        z = ((T)_xIntrEnumr.Current);
                    return z;
                }
            }
            public int CurrIndex
            {
                get { return _xIntrEnumr.CurrPos; }
                set 
                {
                    if ((value < Count) && (value >= 0))
                        _xIntrEnumr.CurrPos = value;
                }
            }


        }

    }


    public class FuncDic
    {
        // описание нажатой клавиши
        //public struct KeySearch
        private struct KeySearch
        {
            public KeySearch(int nK, Keys kM)
            {
                nKVal = nK;
                kMod = kM;
            }
            public int nKVal;
            public Keys kMod;
        }

        // описание одной функции
        //public class FuncDef
        public class FuncDef
        {
            // описание для Help не указывается
            public FuncDef(int nF, string sKD) : this(nF, sKD, "") { }

            public FuncDef(int nF, string sKD, string sH)
            {
                nFuncCode = nF;
                sKDef = sKD;
                sHelp = sH;
            }

            public FuncDef() { }

            // код функции
            public int nFuncCode;
            // обозначение комбинации
            public string sKDef;
            // описание для Help
            public string sHelp;
        }

        // для вывода в XML
        //public struct FuncRec
        public struct FuncRec
        {
            public FuncDef fD;

            [XmlElement(ElementName = "KeyValue")]
            public int nKeyV;

            [XmlElement(ElementName = "KeyModifier")]
            public string sKeyM;

        }

        private Dictionary<KeySearch, FuncDef> 
            lFuncs;
        private string 
            sFileName = "KeyMap.xml";
        private bool 
            m_Loaded = false;
        private List<string> 
            m_HelpArr;

        private FuncRec[] aFR;

        public FuncDic() : this("") { }

        public FuncDic(string sFileKM)
        {
            lFuncs = new Dictionary<KeySearch, FuncDef>();
            if (sFileKM.Length > 0)
                sFileName = sFileKM;
            if (RestoreKMap(sFileName) != AppC.RC_OK)
            {
                Loaded = false;
                //SetDefaultFunc();
            }
            else
            {
                Loaded = true;
            }
            SetDefaultHelp();
        }

        // построение Help-экрана из описания
        public void SetDefaultHelp()
        {
            int nCurLen,
                nMaxLen = 0;
            List<string> lK = new List<string>();

            m_HelpArr = new List<string>();
            foreach (KeyValuePair<KeySearch, FuncDef> kp in lFuncs)
            {
                if ((kp.Value.sHelp.Length > 0) && (kp.Value.sKDef.Length > 0))
                {
                    m_HelpArr.Add( kp.Value.sHelp );
                    lK.Add( kp.Value.sKDef );
                    nCurLen = lK[lK.Count - 1].Length;
                    if (nCurLen > nMaxLen)
                        nMaxLen = nCurLen;
                }
            }
            for (int i = 0; i < lK.Count; i++)
                m_HelpArr[i] = lK[i].PadRight(nMaxLen) + m_HelpArr[i];
        }

        // построение списка функций по умолчанию
        public void SetDefaultFunc()
        {
            AddNewFunc(W32.VK_F1,       Keys.None,      AppC.F_HELP,        "F1",       "");
            AddNewFunc(W32.VK_F2,       Keys.None,      AppC.F_UPLD_DOC,    "F2",       " - выгрузить");
            AddNewFunc(W32.VK_F3,       Keys.None,      AppC.F_LOAD_DOC,    "F3",       " - загрузить");
            AddNewFunc(W32.VK_F9_PC,    Keys.None,      AppC.F_MENU,        "F9",       " - меню");
            AddNewFunc(W32.VK_ESC,      Keys.Shift,     AppC.F_QUIT,        "SFT-Esc",  " - выход");
            AddNewFunc(W32.VK_TAB,      Keys.None,      AppC.F_NEXTPAGE,    "TAB",      " - вкладка вперед");
            AddNewFunc(W32.VK_TAB,      Keys.Shift,     AppC.F_PREVPAGE,    "SFT-TAB",  " - вкладка назад");
        }


        // флаг загрузки из файла
        public bool Loaded
        {
            get { return m_Loaded; }
            set { m_Loaded = value; }
        }

        // очистка всего списка
        public void Clear()
        {
            lFuncs.Clear();
        }

        // установка новой функции, все альтернативные способы вызова удаляются
        public void SetNewFunc(int nKV, Keys kM, int nFCode, string sKDef, string sFHelp)
        {
            NewFunc(new KeySearch(nKV, kM), new FuncDef(nFCode, sKDef, sFHelp), 1);
        }

        // добавление новой функции, все альтернативные способы вызова сохраняются
        public void AddNewFunc(int nKV, Keys kM, int nFCode, string sKDef, string sFHelp)
        {
            NewFunc(new KeySearch(nKV, kM), new FuncDef(nFCode, sKDef, sFHelp), 0);
        }

        // добавление новой функции
        // nDelOther > 0 - удалить все существующие комбинации клавиш для функции
        private void NewFunc(KeySearch fK, FuncDef fD, int nDelOther)
        {
            FuncDef
                defOld = null;

            if (lFuncs.ContainsKey(fK))
            {// существующая комбинация будет удалена
                defOld = lFuncs[fK];
                lFuncs.Remove(fK);
            }

            // все клавиши для такой же функции
            List<KeySearch> ks = new List<KeySearch>();
            foreach (KeyValuePair<KeySearch, FuncDef> kp in lFuncs)
            {
                if (kp.Value.nFuncCode == fD.nFuncCode)
                    ks.Add(kp.Key);
            }

            if (nDelOther > 0)
            {// удалить все существующие комбинации клавиш для функции
                foreach (KeySearch x in ks)
                    lFuncs.Remove(x);
            }
            else
            {// добавление новой комбинации, описание предыдущих комбинаций очищается
                if ((fD.sHelp.Length > 0) && (fD.sKDef.Length > 0))
                    foreach (KeySearch x in ks)
                        lFuncs[x].sHelp = "";
                else
                    if (defOld != null)
                        fD = defOld;
            }
            lFuncs.Add(fK, fD);
        }

        // получить код функции для заданной комбинации клавиш
        public int TryGetFunc(KeyEventArgs e)
        {
            int ifunc;
            FuncDef xFD;
            KeySearch ks = new KeySearch(e.KeyValue, e.Modifiers);

            if (lFuncs.TryGetValue(new KeySearch(e.KeyValue, e.Modifiers), out xFD))
                ifunc = xFD.nFuncCode;
            else
                ifunc = -1;
            return (ifunc);
        }

        // получить описание функции для заданного кода
        public FuncDef TryGetFuncDef(int FuncCode)
        {
            FuncDef
                ret = null;

            foreach (KeyValuePair<KeySearch, FuncDef> kp in lFuncs)
            {
                if (kp.Value.nFuncCode == FuncCode)
                {
                    ret = kp.Value;
                    break;
                }
            }
            return (ret);
        }

        // получить описание комбинации клавиш для заданного кода
        public string TryGetFuncKeys(int FuncCode)
        {
            string
                ret = "";

            foreach (KeyValuePair<KeySearch, FuncDef> kp in lFuncs)
            {
                if (kp.Value.nFuncCode == FuncCode)
                {
                    ret = kp.Value.sKDef;
                    break;
                }
            }
            return (ret);
        }


        public int SaveKMap()
        {
            return (SaveKMap(sFileName));
        }

        // сохранить все текущие коды функций и комбинации клавиш
        public int SaveKMap(string sFName)
        {
            int i,
                k = 0;

            k = lFuncs.Count;
            aFR = new FuncRec[k];
            i = 0;
            foreach (KeyValuePair<KeySearch, FuncDef> kp in lFuncs)
            {
                aFR[i].nKeyV = kp.Key.nKVal;
                aFR[i].sKeyM = kp.Key.kMod.ToString();
                aFR[i].fD = kp.Value;
                i++;
            }
            k = Srv.WriteXMLObjTxt(typeof(FuncRec[]), aFR, sFName);
            return (k);
        }

        //public int RestoreKMap()
        //{
        //    return (RestoreKMap(sFileName));
        //}

        // загрузить коды функций и комбинации клавиш
        public int RestoreKMap(string sFName)
        {
            int i,
                r = AppC.RC_NOFILE;
            object aF;
            Keys k;


            if (File.Exists(sFName))
            {
                r = Srv.ReadXMLObj(typeof(FuncRec[]), out aF, sFName);
                if (r == AppC.RC_OK)
                {
                    aFR = (FuncRec[])aF;
                    i = 0;
                    lFuncs.Clear();
                    for (i = 0; i < aFR.Length; i++)
                    {
                        k = (aFR[i].sKeyM == Keys.Control.ToString()) ? Keys.Control :
                            (aFR[i].sKeyM == Keys.Shift.ToString()) ? Keys.Shift :
                            (aFR[i].sKeyM == Keys.Alt.ToString()) ? Keys.Alt : Keys.None;
                        try
                        {
                            lFuncs.Add(new KeySearch(aFR[i].nKeyV, k), aFR[i].fD);
                        }
                        catch
                        {
                            Srv.ErrorMsg(String.Format("Ошибка описания функции {0}", aFR[i].fD.nFuncCode));
                        }
                    }
                }
                else
                    Srv.ErrorMsg("Файл переназначения клавиш не загружен!\nИспользуются стандартные...", "Ошибка !", true);
            }
            return (r);
        }

        // установить массив Help
        public void SetFHelp(List<string> aH)
        {
            m_HelpArr = aH;
        }

        // получить массив Help
        public List<string> GetFHelp()
        {
            return (m_HelpArr);
        }


    }


    public class BindingListView<T> : IBindingListView, ITypedList
    {
        #region Fields

        private IList _innerList;
        private ListSortDescriptionCollection _sortDescriptions;
        private int[] _sortIndices;
        private int[] _filterIndices;
        private DataTable _filterTable;
        private string _currentFilterExpression = string.Empty;
        private PropertyDescriptorCollection _properties;

        #endregion

        #region Constructors

        /// <summary>
        /// Creates a new instance with an empty <see cref="InnerList"/>.
        /// </summary>
        public BindingListView() : this(new ArrayList()) { }

        /// <summary>
        /// Creates a new instance.
        /// </summary>
        /// <param m_Name="list">The <see cref="InnerList"/> to be used.</param>
        public BindingListView(IList list)
        {
            _innerList = list;
            RemoveSort();

            InitializeFiltering();
        }

        #endregion

        #region Public interface

        /// <summary>
        /// Gets the contained <see cref="IList"/> actually
        /// holding the sBarCode.
        /// </summary>
        public IList InnerList
        {
            get { return _innerList; }
        }

        /// <summary>
        /// Raises the <see cref="ListChanged"/> event with <see cref="ListChangedType.Reset"/>.
        /// </summary>
        public void RaiseListChanged()
        {
            OnListChanged(new ListChangedEventArgs(ListChangedType.Reset, 0));
        }

        /// <summary>
        /// Raises the <see cref="ListChanged"/> event with the specified arguments.
        /// </summary>
        /// <param m_Name="args">Event arguments.</param>
        public void RaiseListChanged(ListChangedEventArgs args)
        {
            OnListChanged(args);
        }

        #endregion

        #region Protected interface

        /// <summary>
        /// Raises the <see cref="ListChanged"/> event.
        /// </summary>
        /// <param m_Name="args">Event arguments.</param>
        protected virtual void OnListChanged(ListChangedEventArgs args)
        {
            if (ListChanged != null)
                ListChanged(this, args);
        }

        #endregion

        #region Privates

        private void InitializeFiltering()
        {
            _properties = ListBindingHelper.GetListItemProperties(typeof(T));
            _filterTable = new DataTable("FilterTable");
            foreach (PropertyDescriptor property in _properties)
            {
                Type colType = property.PropertyType;
                if (colType.IsGenericType && colType.GetGenericTypeDefinition().Equals(typeof(Nullable<>)))
                    colType = colType.GetGenericArguments()[0];
                _filterTable.Columns.Add(property.Name, colType);
            }
        }

        #endregion

        #region IBindingListView Member

        /// <summary>
        /// Sorts the sBarCode source based on the given <see cref="ListSortDescriptionCollection"/>.
        /// </summary>
        /// <param m_Name="sorts">
        /// The <see cref="ListSortDescriptionCollection"/> containing 
        /// the sorts to apply to the sBarCode source.
        /// </param>
        public void ApplySort(ListSortDescriptionCollection sorts)
        {
            _sortDescriptions = sorts;
            _sortIndices = new int[_innerList.Count];
            object[] items = new object[_innerList.Count];
            for (int i = 0; i < _sortIndices.Length; i++)
            {
                _sortIndices[i] = i;
                items[i] = _innerList[i];
            }
            Array.Sort(items, _sortIndices, new GenericComparer(sorts));
            this.Filter = _currentFilterExpression;
        }

        /// <summary>
        /// Gets or sets the filter to be used to exclude items from the collection 
        /// of items returned by the sBarCode source.
        /// </summary>
        public string Filter
        {
            get { return _currentFilterExpression; }
            set
            {
                _filterIndices = null;
                _currentFilterExpression = string.Empty;
                object val = DBNull.Value;

                if (!String.IsNullOrEmpty(value))
                {
                    DataFilter dataFilter = new DataFilter(value, _filterTable);
                    List<int> filteredIndices = new List<int>();

                    int count = this.Count;
                    int propertiesCount = _properties.Count;
                    DataRow row = _filterTable.NewRow();
                    for (int i = 0; i < Count; i++)
                    {
                        object item = this[i];
                        for (int j = 0; j < propertiesCount; j++)
                        {
                            val = _properties[j].GetValue(item);
                            if (val == null)
                                row[j] = DBNull.Value;
                            else
                                row[j] = val;
                        }


                        if (dataFilter.Invoke(row))
                            filteredIndices.Add(i);
                    }

                    _filterIndices = filteredIndices.ToArray();
                    _currentFilterExpression = value;
                }

                OnListChanged(new ListChangedEventArgs(ListChangedType.Reset, 0));
            }
        }

        /// <summary>
        /// Removes the current filter applied to the sBarCode source.
        /// </summary>
        public void RemoveFilter()
        {
            this.Filter = string.Empty;
        }

        /// <summary>
        /// Gets the collection of sort descriptions currently applied to the sBarCode source.
        /// </summary>
        public ListSortDescriptionCollection SortDescriptions
        {
            get { return _sortDescriptions; }
        }

        /// <summary>
        /// Gets a value indicating whether the sBarCode source supports advanced sorting.
        /// </summary>
        public bool SupportsAdvancedSorting
        {
            get { return true; }
        }

        /// <summary>
        /// Gets a value indicating whether the sBarCode source supports filtering.
        /// </summary>
        public bool SupportsFiltering
        {
            get { return true; }
        }

        #endregion

        #region IBindingList Member

        /// <summary>
        /// Occurs when the list changes or an item in the list changes.
        /// </summary>
        public event ListChangedEventHandler ListChanged;

        /// <summary>
        /// Gets whether you can update items in the list.
        /// </summary>
        public bool AllowEdit
        {
            get { return true; }
        }

        /// <summary>
        /// Gets whether you can add items to the list using <see cref="AddNew()"/>.
        /// </summary>
        public bool AllowNew
        {
            get { return false; }
        }

        /// <summary>
        /// Gets whether you can remove items from the list, using 
        /// <see cref="Remove"/> or <see cref="RemoveAt"/>.
        /// </summary>
        public bool AllowRemove
        {
            get { return true; }
        }

        /// <summary>
        /// Sorts the list based on a PropertyDescriptor and a ListSortDirection.
        /// </summary>
        /// <param m_Name="property">The <see cref="PropertyDescriptor"/> to sort by.</param>
        /// <param m_Name="direction">One of the <see cref="ListSortDirection"/> values.</param>
        public void ApplySort(PropertyDescriptor property, ListSortDirection direction)
        {
            ListSortDescription listSortDescription = new ListSortDescription(property, direction);
            ListSortDescription[] listSortDescriptions = new ListSortDescription[] { listSortDescription };
            ListSortDescriptionCollection sorts = new ListSortDescriptionCollection(listSortDescriptions);

            ApplySort(sorts);
        }

        /// <summary>
        /// Gets whether the items in the list are sorted.
        /// </summary>
        public bool IsSorted
        {
            get { return _sortDescriptions.Count > 0; }
        }

        /// <summary>
        /// Removes any sort applied using <see cref="ApplySort(PropertyDescriptor, ListSortDirection)"/>.
        /// </summary>
        public void RemoveSort()
        {
            _sortDescriptions = new ListSortDescriptionCollection();
            _sortIndices = null;
        }

        /// <summary>
        /// Gets the direction of the sort.
        /// </summary>
        public ListSortDirection SortDirection
        {
            get { return _sortDescriptions.Count == 1 ? _sortDescriptions[0].SortDirection : ListSortDirection.Ascending; ; }
        }

        /// <summary>
        /// Gets the <see cref="PropertyDescriptor"/> that is being used for sorting.
        /// </summary>
        public PropertyDescriptor SortProperty
        {
            get { return _sortDescriptions.Count == 1 ? _sortDescriptions[0].PropertyDescriptor : null; }
        }

        /// <summary>
        /// Gets whether a <see cref="ListChanged"/> event is raised when the 
        /// list changes or an item in the list changes.
        /// </summary>
        public bool SupportsChangeNotification
        {
            get { return true; }
        }

        /// <summary>
        /// Gets whether the list supports searching using the <see cref="Find"/> method.
        /// </summary>
        public bool SupportsSearching
        {
            get { return false; }
        }

        /// <summary>
        /// Gets whether the list supports sorting.
        /// </summary>
        public bool SupportsSorting
        {
            get { return true; }
        }

        #region Not Implemented

        /// <summary>
        /// Adds the <see cref="PropertyDescriptor"/> to the indexes used for searching.
        /// NOT IMPLEMENTED!
        /// </summary>
        /// <param m_Name="property">
        /// The <see cref="PropertyDescriptor"/> to add to the indexes used for searching.
        /// </param>
        public void AddIndex(PropertyDescriptor property)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        /// <summary>
        /// Adds a new item to the list.
        /// NOT IMPLEMENTED!
        /// </summary>
        /// <returns>The item added to the list.</returns>
        public object AddNew()
        {
            throw new Exception("The method or operation is not implemented.");
        }

        /// <summary>
        /// Returns the _position of the row that has the given <see cref="PropertyDescriptor"/>.
        /// NOT IMPLEMENTED!
        /// </summary>
        /// <param m_Name="property">The <see cref="PropertyDescriptor"/> to search on.</param>
        /// <param m_Name="key">The value of the property parameter to search for.</param>
        /// <returns>The _position of the row that has the given <see cref="PropertyDescriptor"/>.</returns>
        public int Find(PropertyDescriptor property, object key)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        /// <summary>
        /// Removes the <see cref="PropertyDescriptor"/> from the indexes used for searching.
        /// NOT IMPLEMENTED!
        /// </summary>
        /// <param m_Name="property">
        /// The <see cref="PropertyDescriptor"/> to remove from the indexes used for searching.
        /// </param>
        public void RemoveIndex(PropertyDescriptor property)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        #endregion

        #endregion

        #region IList Member

        /// <summary>
        /// Adds an item to the <see cref="IList"/>.  
        /// </summary>
        /// <param m_Name="value">The instance to add to the <see cref="IList"/>.</param>
        /// <returns>The position into which the new element was inserted.</returns>
        public int Add(object value)
        {
            if (value != null && !typeof(T).IsAssignableFrom(value.GetType()))
                throw new ArgumentException("Given instance doesn't match needed type.");

            int result = _innerList.Add(value);
            OnListChanged(new ListChangedEventArgs(ListChangedType.ItemAdded, result));
            return result;
        }

        /// <summary>
        /// Removes all items from the <see cref="IList"/>.  
        /// </summary>
        public void Clear()
        {
            _innerList.Clear();
            OnListChanged(new ListChangedEventArgs(ListChangedType.Reset, 0));
        }

        /// <summary>
        /// Determines whether the <see cref="IList"/> contains a specific value.
        /// </summary>
        /// <param m_Name="value">The instance to locate in the <see cref="IList"/>.</param>
        /// <returns>true if the instance is found in the <see cref="IList"/>; otherwise, false.</returns>
        public bool Contains(object value)
        {
            return _innerList.Contains(value);
        }

        /// <summary>
        /// Determines the _position of a specific item in the <see cref="IList"/>. 
        /// </summary>
        /// <param m_Name="value">The instance to locate in the <see cref="IList"/>.</param>
        /// <returns>The _position of value if found in the list; otherwise, -1.</returns>
        public int IndexOf(object value)
        {
            return _innerList.IndexOf(value);
        }

        /// <summary>
        /// Inserts an item to the <see cref="IList"/> at the specified _position.
        /// </summary>
        /// <param m_Name="_position">The zero-based _position at which value should be inserted.</param>
        /// <param m_Name="value">The instance to insert into the <see cref="IList"/>.</param>
        public void Insert(int index, object value)
        {
            if (value != null && !typeof(T).IsAssignableFrom(value.GetType()))
                throw new ArgumentException("Given instance doesn't match needed type.");

            _innerList.Insert(index, value);
            OnListChanged(new ListChangedEventArgs(ListChangedType.ItemAdded, index));
        }

        /// <summary>
        /// Gets a value indicating whether the <see cref="IList"/> has a fixed size. 
        /// </summary>
        public bool IsFixedSize
        {
            get { return _innerList.IsFixedSize; }
        }

        /// <summary>
        /// Gets a value indicating whether the <see cref="IList"/> is read-only.
        /// </summary>
        public bool IsReadOnly
        {
            get { return _innerList.IsReadOnly; }
        }

        /// <summary>
        /// Removes the first occurrence of a specific object from the <see cref="IList"/>. 
        /// </summary>
        /// <param m_Name="value">The instance to remove from the <see cref="IList"/>.</param>
        public void Remove(object value)
        {
            int index = IndexOf(value);
            _innerList.Remove(value);
            OnListChanged(new ListChangedEventArgs(ListChangedType.ItemDeleted, index));
        }

        /// <summary>
        /// Removes the <see cref="IList"/> item at the specified _position.
        /// </summary>
        /// <param m_Name="_position">The zero-based _position of the item to remove.</param>
        public void RemoveAt(int index)
        {
            _innerList.RemoveAt(index);
            OnListChanged(new ListChangedEventArgs(ListChangedType.ItemDeleted, index));
        }

        /// <summary>
        /// Gets or sets the element at the specified _position.
        /// </summary>
        /// <param m_Name="_position">The zero-based _position of the element to get or set.</param>
        /// <returns>The element at the specified _position.</returns>
        object IList.this[int index]
        {
            get
            {
                return this[index];
            }
            set
            {
                this[index] = (T)value;
            }
        }

        public T this[int index]
        {
            get
            {
                if (_filterIndices != null)
                    index = _filterIndices[index];

                if (_sortIndices != null && index < _sortIndices.Length)
                    index = _sortIndices[index];

                return (T)_innerList[index];
            }
            set
            {
                _innerList[index] = value;
                OnListChanged(new ListChangedEventArgs(ListChangedType.ItemChanged, index));
            }
        }

        #endregion

        #region ICollection Member

        /// <summary>
        /// Copies the elements of the <see cref="ICollection"/> to an <see cref="Array"/>, 
        /// starting at a particular <see cref="Array"/> _position.
        /// </summary>
        /// <param m_Name="array">
        /// The one-dimensional <see cref="Array"/> that is the destination of the elements copied from 
        /// <see cref="ICollection"/>. The <see cref="Array"/> must have zero-based indexing.
        /// </param>
        /// <param m_Name="_position">The zero-based _position in array at which copying begins.</param>
        public void CopyTo(Array array, int index)
        {
            _innerList.CopyTo(array, index);
        }

        /// <summary>
        /// Gets the m_NumCode of elements contained in the <see cref="ICollection"/>. 
        /// </summary>
        public int Count
        {
            get { return _filterIndices == null ? _innerList.Count : _filterIndices.Length; }
        }

        /// <summary>
        /// Gets a value indicating whether access to the <see cref="ICollection"/> is 
        /// synchronized (thread safe). 
        /// </summary>
        public bool IsSynchronized
        {
            get { return _innerList.IsSynchronized; }
        }

        /// <summary>
        /// Gets an object that can be used to synchronize access to the <see cref="ICollection"/>. 
        /// </summary>
        public object SyncRoot
        {
            get { return _innerList.SyncRoot; }
        }

        #endregion

        #region IEnumerable Member

        /// <summary>
        /// Returns an enumerator that iterates through a collection.
        /// </summary>
        /// <returns>
        /// An <see cref="IEnumerator"/> object that can be used to iterate 
        /// through the collection.
        /// </returns>
        public IEnumerator GetEnumerator()
        {
            for (int i = 0; i < this.Count; i++)
                yield return this[i];
        }

        #endregion

        #region ITypedList Member

        /// <summary>
        /// Returns the <see cref="PropertyDescriptorCollection"/> that represents the 
        /// properties on each item used to bind sBarCode.
        /// </summary>
        /// <param m_Name="listAccessors">
        /// An array of <see cref="PropertyDescriptor"/> objects to find in the collection 
        /// as bindable. This can be a null reference.
        /// </param>
        /// <returns>The <see cref="PropertyDescriptorCollection"/> that represents the 
        /// properties on each item used to bind sBarCode.</returns>
        public PropertyDescriptorCollection GetItemProperties(PropertyDescriptor[] listAccessors)
        {
            return ListBindingHelper.GetListItemProperties(typeof(T));
        }

        /// <summary>
        /// Returns the m_Name of the list.
        /// </summary>
        /// <param m_Name="listAccessors">
        /// An array of <see cref="PropertyDescriptor"/> objects, for which the list 
        /// m_Name is returned. This can be a null reference.
        /// </param>
        /// <returns>The m_Name of the list.</returns>
        public string GetListName(PropertyDescriptor[] listAccessors)
        {
            return this.GetType().Name;
        }

        #endregion
    }


    /// <summary>
    /// Public Wrapper for the internal DataExpression class in the .Net framework.
    /// The purpose of this class is to test if single <see cref="DataRow"/>sSig match
    /// a given filter expression.
    /// </summary>
    internal class DataFilter
    {
        #region Fields

        private static Type _internalDataFilterType;
        private static ConstructorInfo _constructorInfo;
        private static MethodInfo _methodInvokeInfo;
        private object _internalDataFilter;

        #endregion

        #region Constructors

        static DataFilter()
        {
            _internalDataFilterType = typeof(DataTable).Assembly.GetType("System.Data.DataExpression");
            //_constructorInfo = _internalDataFilterType.GetConstructor(
            //    BindingFlags.NonPublic | BindingFlags.Instance, 
            //    null, 
            //    CallingConventions.Any, 
            //    new Type[] { typeof(DataTable), typeof(string) }, 
            //    null);
            _constructorInfo = _internalDataFilterType.GetConstructor(
                BindingFlags.NonPublic | BindingFlags.Instance,
                null,
                new Type[] { typeof(DataTable), typeof(string) },
                null);
            _methodInvokeInfo = _internalDataFilterType.GetMethod("Invoke", BindingFlags.Public | BindingFlags.Instance, null, new Type[] { typeof(DataRow), typeof(DataRowVersion) }, null);
        }

        /// <summary>
        /// Creates a new instance.
        /// </summary>
        /// <param m_Name="expression">Filter expression string.</param>
        /// <param m_Name="dataTable"><see cref="DataTable"/> of the rows to be tested.</param>
        public DataFilter(string expression, DataTable dataTable)
        {
            try
            {
                _internalDataFilter = _constructorInfo.Invoke(new object[] { dataTable, expression });
            }
            catch (System.Reflection.TargetInvocationException invocEx)
            {
                throw invocEx.InnerException;
            }
        }

        #endregion

        #region Public interface

        /// <summary>
        /// Tests whether a single <see cref="DataRow"/> matches the filter expression.
        /// </summary>
        /// <param m_Name="row"><see cref="DataRow"/> to be tested.</param>
        /// <returns>True if the row matches the filter expression, otherwise false.</returns>
        public bool Invoke(DataRow row)
        {
            return Invoke(row, DataRowVersion.Default);
        }

        /// <summary>
        /// Tests whether a single <see cref="DataRow"/> matches the filter expression.
        /// </summary>
        /// <param m_Name="row"><see cref="DataRow"/> to be tested.</param>
        /// <param m_Name="version">The row version to use.</param>
        /// <returns>True if the row matches the filter expression, otherwise false.</returns>
        public bool Invoke(DataRow row, DataRowVersion version)
        {
            return (bool)_methodInvokeInfo.Invoke(_internalDataFilter, new object[] { row, version });
        }

        #endregion
    }

    /// <summary>
    /// Implementation of the <see cref="IComparer"/> interface which
    /// compares according to a given <see cref="ListSortDescriptionCollection"/>.
    /// </summary>
    internal class GenericComparer : IComparer
    {
        #region Fields

        private ListSortDescriptionCollection _sortDescriptions;

        #endregion

        #region Constructors

        /// <summary>
        /// Creates a new instance.
        /// </summary>
        /// <param m_Name="sortDescriptions">
        /// The <see cref="ListSortDescriptionCollection"/> which should be
        /// used as the bassi for comparison.
        /// </param>
        public GenericComparer(ListSortDescriptionCollection sortDescriptions)
        {
            _sortDescriptions = sortDescriptions;
        }

        #endregion

        #region IComparer Member

        /// <summary>
        /// Compares two objects and returns a value indicating whether one is less 
        /// than, equal to, or greater than the other.
        /// </summary>
        /// <param m_Name="x">The first object to compare.</param>
        /// <param m_Name="y">The second object to compare.</param>
        /// <returns></returns>
        public int Compare(object x, object y)
        {
            for (int i = 0; i < _sortDescriptions.Count; i++)
            {
                PropertyDescriptor propertyDescriptor = _sortDescriptions[i].PropertyDescriptor;

                object valueX = propertyDescriptor.GetValue(x);
                object valueY = propertyDescriptor.GetValue(y);

                bool xIsNull = valueX == DBNull.Value || valueX == null;
                bool yIsNull = valueY == DBNull.Value || valueY == null;

                int result;
                if (xIsNull)
                {
                    if (yIsNull)
                        result = 0;
                    else
                        result = -1;
                }
                else
                {
                    if (yIsNull)
                        result = 1;
                    else
                    {
                        IComparable comparableX = valueX as IComparable;
                        IComparable comparableY = valueY as IComparable;

                        result = comparableX.CompareTo(comparableY);
                    }
                }

                if (result != 0)
                    return _sortDescriptions[i].SortDirection == ListSortDirection.Ascending ? result : -result;
            }

            return 0;
        }

        #endregion
    }




}


namespace SkladAll
{
}
namespace Unused
{
    // сериализация Dictionary
    //[XmlRoot("Dictionary")]
    //public class SortedListXML<TKey, TValue> : SortedList<TKey, TValue>, IXmlSerializable
    //{
    //    #region IXmlSerializable Members

    //    public System.Xml.Schema.XmlSchema GetSchema()
    //    {
    //        return null;
    //    }

    //    public void ReadXml(System.Xml.XmlReader reader)
    //    {
    //        XmlSerializer keySerializer = new XmlSerializer(typeof(TKey));
    //        XmlSerializer valueSerializer = new XmlSerializer(typeof(TValue));
    //        bool wasEmpty = reader.IsEmptyElement;
    //        reader.Read();
    //        if (wasEmpty)
    //            return;
    //        while (reader.NodeType != System.Xml.XmlNodeType.EndElement)
    //        {
    //            reader.ReadStartElement("item");
    //            reader.ReadStartElement("key");
    //            TKey key = (TKey)keySerializer.Deserialize(reader);
    //            reader.ReadEndElement();
    //            reader.ReadStartElement("value");
    //            TValue value = (TValue)valueSerializer.Deserialize(reader);
    //            reader.ReadEndElement();
    //            this.Add(key, value);
    //            reader.ReadEndElement();
    //            reader.MoveToContent();
    //        }
    //        reader.ReadEndElement();
    //    }

    //    public void WriteXml(System.Xml.XmlWriter writer)
    //    {
    //        XmlSerializer keySerializer = new XmlSerializer(typeof(TKey));
    //        XmlSerializer valueSerializer = new XmlSerializer(typeof(TValue));
    //        foreach (TKey key in this.Keys)
    //        {
    //            writer.WriteStartElement("item");
    //            writer.WriteStartElement("key");
    //            keySerializer.Serialize(writer, key);
    //            writer.WriteEndElement();
    //            writer.WriteStartElement("value");
    //            TValue value = this[key];
    //            valueSerializer.Serialize(writer, value);
    //            writer.WriteEndElement();
    //            writer.WriteEndElement();
    //        }
    //    }

    //    #endregion
    //}

}
