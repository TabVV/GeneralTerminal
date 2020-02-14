//--------------------------------------------------------------------- 
//THIS CODE AND INFORMATION ARE PROVIDED AS IS WITHOUT WARRANTY OF ANY 
//KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE 
//IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A 
//PARTICULAR PURPOSE. 
//---------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;
using System.Drawing;
using System.Reflection;
using System.Data;
using System.ComponentModel;

using PDA.Service;

namespace SkladAll
{
    // We'll inherit from DataGridTextBoxColumn, not from DataGridColumnStyle to ensure desktop compatibility.
    // Since some abstract methods are not availible on NETCF'sTypDoc DataGridColumnStyle, it'sTypDoc not possible to override them.
    // Thus attempt to run this code on desktop would fail as these abstract methods won't have implementation at runtime.
    
    public class DGCustomColumn : DataGridTextBoxColumn
    {

        #region Privates

        private StringFormat _stringFormat = null;                      // формат вывода значения в ячейке

        private DataGrid _owner = null;
        private int _columnOrdinal = -1;                                // Our ordinal in the grid.
        
        private Control _hostedControl = null;                          // Column'sTypDoc hosted control (e.g. TextBox).
        private Rectangle _bounds = Rectangle.Empty;                    // Last known bounds of hosted control.

        private bool _readOnly = false;                                 // Set if column is read only.

        private Color _alternatingBackColor = SystemColors.Window;      // Back color for odd numbered rows
        private Color _alternatingBackColor1 = SystemColors.Window;                      // Back color for odd numbered rows
        private SolidBrush _alternatingBrush = null;                                    // Brush to use for odd numbered rows
        private SolidBrush _alternatingBrush1 = null;                                    // Brush to use for odd numbered rows

        private SolidBrush _selectedBack = null;                        // фон выбранной колонки
        private SolidBrush _selectedFore = null;                        // фон выбранной колонки

        private string _sTable = "";                                    // для какой таблицы
        
        #endregion

        #region Public properties

        
        // Color for odd numbered rows. Has no effect untill set.
        public Color AlternatingBackColor
        {
            get 
            {
                return _alternatingBackColor;                                   
            }
            set
            {
                if(_alternatingBackColor != value)                                      // Setting new color?
                {
                    if (this._alternatingBrush != null)                                 // See if got brush
                    {
                        this._alternatingBrush.Dispose();                               // Yes, get rid of it.
                    }
                    
                    this._alternatingBackColor = value;                                 // Set new color

                    this._alternatingBrush = new SolidBrush(value);                     // Create new brush.

                    this.Invalidate();
                }
            }
        }
        public Color AlternatingBackColorSpec
        {
            get
            {
                return _alternatingBackColor1;
            }
            set
            {
                if (_alternatingBackColor1 != value)                                      // Setting new color?
                {
                    if (this._alternatingBrush1 != null)                                 // See if got brush
                    {
                        this._alternatingBrush1.Dispose();                               // Yes, get rid of it.
                    }

                    this._alternatingBackColor1 = value;                                 // Set new color

                    this._alternatingBrush1 = new SolidBrush(value);                     // Create new brush.

                    this.Invalidate();
                }
            }
        }

        public SolidBrush AltSolidBrush
        {
            get
            {
                return _alternatingBrush;
            }
        }
        public SolidBrush AltSolidBrushSpec
        {
            get
            {
                return _alternatingBrush1;
            }
        }
        public SolidBrush SelBackBrush
        {
            get
            {
                return _selectedBack;
            }
        }
        public SolidBrush SelForeBrush
        {
            get
            {
                return _selectedFore;
            }
        }

        // Sets horisontal aligment in the cell. We don't have separate variable for this, we'll keep it in StringFormat instead.
        public virtual HorizontalAlignment Alignment
        {
            get 
            {
                return (this.StringFormat.Alignment == StringAlignment.Center) ? HorizontalAlignment.Center :
                       (this.StringFormat.Alignment == StringAlignment.Far) ? HorizontalAlignment.Right : HorizontalAlignment.Left;
            }
            set
            {
                if (this.Alignment != value)                                    // New aligment?
                {
                    this.StringFormat.Alignment = (value == HorizontalAlignment.Center) ? StringAlignment.Center :
                                                  (value == HorizontalAlignment.Right) ? StringAlignment.Far : StringAlignment.Near;
                                                                                // Set it.
                    Invalidate();                                               // Aligment just changed, repaint.
                }
            }
        }
        
        // Determines if column is read only or not.
        public virtual bool ReadOnly
        {
            get
            {
                return this._readOnly;
            }
            set
            {
                if (this._readOnly != value)                                    // New value?
                {
                    this._readOnly = value;                                     // Yes, store it.
                    this.Invalidate();                                          // Update grid.
                }
            }
        }

        // Use this to set text formatting in the grid including aligment. 
        // Note: grid needs to be invalidated if format is changed.
        public StringFormat StringFormat
        {
            get
            {   
                if (null == _stringFormat)
                {// формат не задавали
                    _stringFormat = new StringFormat(StringFormatFlags.NoWrap);
                    this.Alignment = HorizontalAlignment.Left;
                }
                return _stringFormat;
            }
            set
            {
                _stringFormat = value;
            }
        }

        // Gets or sets null value. This values would be shown if sBarCode in the sBarCode source is null.
        // If sBarCode in the grid is set to this value, null would be pushed to sBarCode source.
        // Our base class is text oriented, so we'll use NullText for that.
        public virtual object NullValue
        {
            get 
            { 
                return this.NullText;
            }
            set 
            { 
                this.NullText = value.ToString(); 
            }
        }

        // Returns column'sTypDoc ordinal in the grid.
        public int ColumnOrdinal
        {
            get
            {
                if ((_columnOrdinal == -1) && (this.Owner != null))                     // Parent is set but ordinal is not?
                {
                    foreach (DataGridTableStyle table in this.Owner.TableStyles)        // Check all tables.
                    {
                        this._columnOrdinal = table.GridColumnStyles.IndexOf(this);     // Get our _position.

                        if (this._columnOrdinal != -1) break;                           // Exit if found.
                    }
                }

                return _columnOrdinal;    
            }
        }

        // Gets or sets DataGrid we're part of. Can be set only onece.
        public DataGrid Owner
        {
            get 
            {
                if (null == _owner)
                {
                    throw new InvalidOperationException("DataGrid owner of this ColumnStyle must be set prior to this operation.");
                }
                return _owner; 
            }
            set
            {
                if (null != _owner)
                {
                    throw new InvalidOperationException("DataGrid owner of this ColumnStyle is already set.");
                }

                _owner = value;
                this._selectedBack = new SolidBrush(_owner.SelectionBackColor);
                this._selectedFore = new SolidBrush(_owner.SelectionForeColor);
            }
        }

        // Gets hosted control.
        public Control HostedControl
        {
            get
            {
                if ((null == this._hostedControl) && (this.Owner != null))              // If not created and have owner...
                {                                                                       
                    this._hostedControl = this.CreateHostedControl();                   // Create hosted control.

                    this._hostedControl.Visible = false;                                // Hide it.
                    this._hostedControl.Name = this.HeaderText;
                    this._hostedControl.Font = this.Owner.Font;                         // Set up control'sTypDoc font to match grid'sTypDoc font.

                    this.Owner.Controls.Add(this._hostedControl);                       // Add it to grid'sTypDoc conrtols.

                    this._hostedControl.DataBindings.Add(this.GetBoundPropertyName(), Owner.DataSource, this.MappingName, true, DataSourceUpdateMode.OnValidation, this.NullValue);
                                                                                        // Set up sBarCode binding so contol would get sBarCode from sBarCode source.

                    // Now we need to hook into grid'sTypDoc horisontal scroll event so we could move hosted control as user scrolls.
                    // To do so we'll look for HScrollBar control owned by the groid. We'll grab the first one we found.

                    HScrollBar horisonal = null;                                        // Assume no ScrollBar found.
                        
                    foreach (Control c in this.Owner.Controls)                          // For each controls owned by grid...
                    {
                        if ((horisonal = c as HScrollBar) != null)                      // See if it'sTypDoc HScrollBar
                        {
                            horisonal.ValueChanged += new EventHandler(gridScrolled);
                                                                                        // Got it. Hook into ValueChanged event.
                            break;                                                      // We're done. Terminate.
                         }
                    }
                }


                return _hostedControl;
            }
        }



        // Для какой таблицы используется колонка
        public string TableInd
        {
            get
            {
                //if (this._sTable == "")
                //    this._sTable = NSI.TBD_DOC;
                return this._sTable;
            }
            set
            {
                this._sTable = value;
            }
        }




        #endregion

        #region Methids to be overriden

        // Returns m_Name of the property on hosted control we're going to bind to, e.g. "Text" on TextBox.
        protected virtual string GetBoundPropertyName()
        {
            throw new InvalidOperationException("User must override GetBoundPropertyName()");
        }

        // Creates hosted control and sets it'sTypDoc properties as needed.
        protected virtual Control CreateHostedControl()
        {
            throw new InvalidOperationException("User must override CreateHostedControl()");
        }

        #endregion

        #region Protected methods

        // Would reposition, hide and show hosted control as needed.
        protected void updateHostedControl()
        {
            if (this.Owner.CurrentRowIndex <= -1)
                return;

            Rectangle selectedBounds = this.Owner.GetCellBounds(this.Owner.CurrentCell.RowNumber, this.Owner.CurrentCell.ColumnNumber);
                                                                                    // Get selected cell bounds.
            
            // We only need to show hosted control if column is not read only, 
            // selected cell is in our column and not occluded by anything.
            
            if (!this.ReadOnly && (this.ColumnOrdinal == this.Owner.CurrentCell.ColumnNumber) &&
                 this.Owner.HitTest(selectedBounds.Left, selectedBounds.Top).Type == DataGrid.HitTestType.Cell &&
                 this.Owner.HitTest(selectedBounds.Right, selectedBounds.Bottom).Type == DataGrid.HitTestType.Cell )
            {                                                                   
                    if (selectedBounds != this._bounds)                             // See if control bounds are already set.
                    {                                                               
                        this._bounds = selectedBounds;                              // Store last bounds. Note we can't use control'sTypDoc bounds 
                                                                                    // as some controls are not resizable and would change bounds as they pleased.
                        this.HostedControl.Bounds = selectedBounds;                 // Update control bounds.
                        
                        this.HostedControl.Focus();
                        this.HostedControl.Update();                                // And update control now so it looks better visually.
                    }

                    if (!this.HostedControl.Visible)                                // If control is not yet visible...
                    {
                        this.HostedControl.Show();                                  // Show it
                        this.HostedControl.Focus();
                    }
            } 
            else if (this.HostedControl.Visible)                                    // Hosted control should not be visible. Check if it is.
            {
                this.HostedControl.Hide();                                          // Hide it.
            }

        }
        
        // We'll override cell painting - new feature introduced in NETCF V2 SP1.
        // This implementation is sutable for all controls representing sBarCode as text. Should be overriden for others.
        protected override void Paint(Graphics g, Rectangle bounds, CurrencyManager source, int rowNum, Brush backBrush, Brush foreBrush, bool alignToRight)
        {
            RectangleF textBounds;                                              // Bounds of text 
            Object cellData;                                                    // Object to show in the cell 
            //System.Data.DataRowView drv = (System.Data.DataRowView)source.List[rowNum]; 

            //source.List[rowNum].
            bool bSell = DrawBackground(g, bounds, rowNum, backBrush, 
                ((System.Data.DataRowView)source.List[rowNum]).Row);                       // Draw cell background

            bounds.Inflate(-2, -2);                                             // Shrink cell by couple pixels for text.

            textBounds = new RectangleF(bounds.X, bounds.Y, bounds.Width, bounds.Height);
                                                                                // Set text bounds.
            cellData = this.PropertyDescriptor.GetValue(source.List[rowNum]);   // Get sBarCode for this cell from sBarCode source.

            if (bSell == true)
                foreBrush = this._selectedFore;

            g.DrawString(FormatText(cellData), this.Owner.Font, foreBrush, textBounds, this.StringFormat);
                                                                                // Render contents 
            this.updateHostedControl();                                         // Update floating hosted control.
        }

        protected virtual bool DrawBackground(Graphics g, Rectangle bounds, int rowNum, Brush backBrush, System.Data.DataRow dr)
        {
            Brush background = backBrush;                                       // Use default brush by... hmm... default.
                
            if((null != background) && ((rowNum & 1) != 0) && !Owner.IsSelected(rowNum))
            {                                                                   // If have alternating brush, row is odd and not selected...
                background = _alternatingBrush;                                 // Then use alternating brush.
            }

            g.FillRectangle(background, bounds);                                // Draw cell background
            return (false);
        }

        #endregion

        // Converts sBarCode from sBarCode source to string according to formatting set.
        protected virtual String FormatText(Object cellData)
        {
            String cellText;                                                    // Formatted text.

            if ((null == cellData) || (DBNull.Value == cellData))               // See if sBarCode is null
            {                                                                   
                cellText = this.NullText;                                       // It'sTypDoc null, so set it to NullText.
            }
            else if (cellData is IFormattable)                                  // Is sBarCode IFormattable?
            {
                cellText = ((IFormattable)cellData).ToString(this.Format, this.FormatInfo);
                                                                                // Yes, format it.
            }
            else if (cellData is IConvertible)                                  // May be it'sTypDoc IConvertible?
            {
                cellText = ((IConvertible)cellData).ToString(this.FormatInfo);  // We'll take that, no problem.
            }
            else
            {
                cellText = cellData.ToString();                                 // At this point we'll give up and simply call ToString()
            }

            return cellText;                                                    
        }

        // Invalidates grid so changes can be reflected.
        protected void Invalidate() 
        {
            if (this.Owner != null)                                             // Got parent?
            {
                this.Owner.Invalidate();                                        // Repaint it.
            }
        }

        #region Private methods
        
        // Event handler for horizonta scrolling.
        private void gridScrolled(Object sender, EventArgs e)
        {
            updateHostedControl();                                              // We need to update hosted control so it would move as grid is scrolled.
        }
        
        #endregion
    }


    public class NSIAll
    {
        // типы таблиц
        [Flags]
        public enum TBLTYPE : int
        {
            NSI         = 1,                            // справочник
            BD          = 2,                            // таблица с данными
            CREATE      = 4,                            // таблица создается
            INTERN      = 8,                            // таблица для внутренних целей
            LOAD        = 16,                           // таблица загружается с сервера
            PASPORT     = 32
        }

        // коды сортировки детальных строк
        public enum TABLESORT : int
        {
            NO = 0,                            // без сортировки
            KODMC = 1,                            // по краткому коду
            NAMEMC = 2,                            // по наименованию
            RECSTATE = 3,                            // по статусу записи
            MAXDET = 4                             // максимальное значение
        }


        // режимы загрузки
        public const int LOAD_EMPTY = 0;              // загрузка незагруженного
        public const int LOAD_ANY = 1;                // загрузка обязательная

        // статус таблицы
        public const int DT_STATE_INIT = 0;           // таблица создана
        public const int DT_STATE_READ = 1;           // таблица прочитана
        public const int DT_STATE_READ_ERR = 2;       // таблица прочитана с ошибками
        public const int DT_STATE_UPDATED = 4;       // таблица прочитана с ошибками

        public const string NSI_NOT_LOAD = "NOTLOAD";   // загружаемую таблицу не грузить (для поля FLAG_LOAD)


        // результат поиска в справочнике
        public struct RezSrch
        {
            public bool bFind;
            public string sName;

            public RezSrch(string s)
            {
                bFind = false;
                sName = s;
            }
        }

        public class TableDef : INotifyPropertyChanged
        {
            private Dictionary<string, DataView>
                m_SortKeys;

            private bool m_InfChanged;

            public event PropertyChangedEventHandler PropertyChanged;

            public DataTable dt;
            public int nState;                          // статус таблицы 0 - не загружена
            public string sXML;                         // имя XML-файла
            public TBLTYPE nType;                       // тип таблицы 0 - НСИ (только чтение)
            public int nGrdStyle;                       // текущий стиль Grid
            //public TABLESORT TSort;                    // текущая сортировка
            public int TSort;                           // текущая сортировка
            public string sTSort;                       // выражение сортировки
            public string sTFilt;                       // выражение фильтра

            public int nErr;                            // код последней операции
            public DataGrid dg;                         // DataGrid
            public string sDTStat;                      // время загрузки таблицы
            public string Text;
            public int
                nCount = 0;

            public List<DataGridTableStyle> xTStyles;

            public TableDef(string TName, DataColumn[] cDef)
            {
                dt = new DataTable(TName);
                dt.Columns.AddRange(cDef);

                nState = DT_STATE_INIT;
                sXML = TName + ".xml";
                nType = TBLTYPE.LOAD | TBLTYPE.NSI;
                nErr = 0;

                nGrdStyle = -1;
                TSort = (int)TABLESORT.NO;
                sTSort = "";
                sTFilt = "";

                //LoadHost = "";
                //LoadPort = 0;
                m_InfChanged = false;
                xTStyles = new List<DataGridTableStyle>();
                m_SortKeys = new Dictionary<string, DataView>();
            }

            private void FirePCNotification(string sPropName)
            {
                if (PropertyChanged != null)
                {
                    PropertyChanged(this, new PropertyChangedEventArgs(sPropName));
                }
            }

            //public string MD5
            //{
            //    get { return m_MD5; }
            //    set
            //    {
            //        m_MD5 = value;
            //        FirePCNotification("MD5");
            //    }
            //}
            //public string LoadHost
            //{
            //    get { return m_LHost; }
            //    set
            //    {
            //        m_LHost = value;
            //        FirePCNotification("LoadHost");
            //    }
            //}
            //public int LoadPort
            //{
            //    get { return m_LPort; }
            //    set
            //    {
            //        m_LPort = value;
            //        FirePCNotification("LoadPort");
            //    }
            //}

            //public DateTime DateLoad
            //{
            //    get { return m_DateLoad; }
            //    set
            //    {
            //        m_DateLoad = value;
            //        FirePCNotification("DateLoad");
            //    }
            //}

            public bool IsChanged
            {
                get { return m_InfChanged; }
                set
                {
                    m_InfChanged = value;
                    FirePCNotification("IsChanged");
                }
            }

            // Набор дополнительных сортировок для поиска в таблице
            public bool SetAddSort(string sSort)
            {
                bool
                    isNewSort = false;
                if (!m_SortKeys.ContainsKey(sSort))
                {
                    try
                    {
                        DataView dv = new DataView(this.dt);
                        dv.Sort = sSort;
                        m_SortKeys.Add(sSort, dv);
                        isNewSort = true;
                    }
                    catch { }
                }
                return (isNewSort);
            }


        }

        // время загрузки всех
        //private float fLoadAll = 0;

        // описание таблиц
        public Dictionary<string, TableDef> DT;

        public static Dictionary<string, TableDef> DTOne;

        // путь загрузки
        public string sPathNSI;     // путь к справочникам
        public string sPathBD;     // путь к таблицам

        // загрузка одной таблицы
        public bool Read1NSI(TableDef xT, int nReg)
        {
            bool
                bWasRead = false;
            int tc1, nE = 0;
            string sPath = "";
            object dg = null;

            //xT.sDTStat = float.MinValue.ToString();
            xT.sDTStat = "0";
            if ((xT.dt != null) &&
                ((xT.nType & TBLTYPE.NSI) == TBLTYPE.NSI) &&
                ( ((xT.nType & TBLTYPE.LOAD) == TBLTYPE.LOAD)||
                  ((xT.nType & TBLTYPE.INTERN) == TBLTYPE.INTERN) || 
                   (nReg == LOAD_ANY)))   // НСИ загружаемое
            {
                if ((xT.nState == DT_STATE_INIT) || (nReg == LOAD_ANY))
                {
                    if (xT.dg != null)
                    {
                        dg = xT.dg.DataSource;
                        xT.dg.DataSource = null;
                    }

                    xT.nErr = 0;
                    tc1 = Environment.TickCount;
                    try
                    {
                        sPath = sPathNSI + xT.sXML;
                        xT.dt.BeginLoadData();
                        nE = 1;
                        xT.dt.Clear();
                        nE = 2;

                        xT.dt.ReadXml(sPath);

                        nE = 3;
                        xT.dt.EndLoadData();
                        xT.nState = DT_STATE_READ;
                        xT.sDTStat = Srv.TimeDiff(tc1, Environment.TickCount);
                        bWasRead = true;
                    }
                    catch (Exception ee)
                    {
                        xT.nErr = nE;
                        xT.nState = DT_STATE_READ_ERR;
                    }
                    if (dg != null)
                        xT.dg.DataSource = dg;
                    dg = null;
                }
            }
            return (bWasRead);
        }

        // возвращает из справочника заданное поле
        public RezSrch GetNameSPR(string iT, object[] xSearch, string sFieldName)
        {
            RezSrch 
                zRet = new RezSrch("-???-");
            DataRow 
                dr = null;
            try
            {
                dr = DT[iT].dt.Rows.Find(xSearch);
                zRet.sName = dr[sFieldName].ToString();
                zRet.bFind = true;
            }
            catch
            {
            }
            return (zRet);
        }


    }

    //=====================

}
