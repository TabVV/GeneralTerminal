using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.IO;
using System.Threading;

namespace SavuSocket
{
    /// <summary>
    /// Event that raised when Server send data to client
    /// </summary>
    public delegate void MessageFromServerEventHandler(object sender, int Count);

    /// <summary>
    /// Event that raised Error
    /// </summary>
    public delegate void ErrorEventHandler(Exception sender);


    /// <summary>
    /// Socket Stream Class
    /// </summary>
    public class SocketStream
    {
        // Имя Host
        private string _serverID = null;
        // или IP-адрес
        private IPAddress _serverIP = null;

        // номер порта для клиента или прослушки
        private int _serverPort = 0;
        
        // TCP-Клиент для организации потока
        private TcpClient myClient = null;
        // поток сокета
        private Stream myStream = null;

        // Листенер для прослушки
        private TcpListener myListen = null;

        private Socket 
            _socket = null, 
            _ClientSock = null,
            _listen = null;

        private ASReadSocket _xASR = null;
        //private ASReadSocket _xASRF = null;
        private ASWriteSocket _xASW = null;

        /// <summary>
        /// Event that raised when Server send data to client
        /// </summary>
        public event MessageFromServerEventHandler MessageFromServerRecived;

        /// <summary>
        /// Event that raised when error raised
        /// </summary>
        public event ErrorEventHandler ErrorRaised;

        private bool _isConnected = false;
        private bool _isListening = false;


        // Для организации прослушки локального порта только его номер
        public SocketStream(int serverPort)
            : this("",serverPort) {}

        public SocketStream(string sHostOrIP, int serverPort)
        {
            if (sHostOrIP.Length > 0)
            {// может, в строке адрес - пробуем получить IP хоста
                try
                {
                    _serverIP = IPAddress.Parse(sHostOrIP);
                    
                }
                catch(FormatException ex)
                {// все-таки это просто имя
                    _serverIP = null;
                    _serverID = sHostOrIP;
                }
            }
            _serverPort = serverPort;
        }

        /// <summary>
        /// Создание клиент-сокета (_ClientSock) и потока (myStream)
        /// </summary>
        /// <returns>
        /// при успешном создании возвращает Stream
        /// </returns>
        public Stream Connect()
        {
            int POLL_WAIT = 1000;

            if (!_isConnected)
            {
                try
                {
                    myStream = null;
                    if (_serverIP == null)
                    {
                        myClient = new TcpClient(_serverID, _serverPort);
                        _ClientSock = myClient.Client;
                    }
                    else
                    {
                        IPEndPoint remoteEP = new IPEndPoint(_serverIP, _serverPort);
                        _ClientSock = new Socket(remoteEP.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                        _ClientSock.Connect(remoteEP);
                    }

                    if (_ClientSock.Poll(POLL_WAIT, SelectMode.SelectWrite) == true)
                    {
                        if (_ClientSock.Poll(POLL_WAIT, SelectMode.SelectError) == false)
                        {
                            myStream = new NetworkStream(_ClientSock);
                            _isConnected = true;
                            _xASR = new ASReadSocket(myStream);
                            //_xASRF = new ASReadSocket(myStream);
                            _xASW = new ASWriteSocket(myStream);
                        }
                    }

                    if (!_isConnected)
                        throw new System.Net.Sockets.SocketException(10061);
                }
                catch (Exception e)
                {
                    if (ErrorRaised != null)
                        ErrorRaised((Exception)e);
                    else
                        //throw e;
                    throw new System.Exception("Нет соединения");
                }
            }
            return (myStream);
        }


        /// <summary>
        /// Disconnect from server
        /// </summary>
        public void Disconnect()
        {
            if (_isConnected)
            {
                try
                {
                    _isConnected = false;
                    if (myStream != null)
                        myStream.Close();
                    if (myClient != null)
                    {// указывали Host и создали клиента
                        myClient.Close();
                        //_serverID = null;
                    }
                    else
                    {
                        if (_ClientSock != null)
                        {
                            //_ClientSock.Shutdown(SocketShutdown.Both);
                            _ClientSock.Close();
                            _ClientSock = null;
                        }
                        //_serverIP = null;
                    }
                    //_isConnected = false;
                }
                catch (Exception e)
                {
                    if (ErrorRaised != null)
                        ErrorRaised((Exception)e);
                    else
                        throw e;
                }
            }
        }



        // коды возврата функций асинхронного чтения/записи
        public enum ASRWERROR : int
        {
            RET_TIMEOUT = 11051,
            RET_FULLMSG = 1,
            RET_FULLBUF = 2,
            RET_ERR = 11052
        }

        //public sealed class ASReadSocketOld
        //{
        //    // откуда читаем
        //    private Stream stmS;

        //    // куда читаем
        //    private byte[] buf;
        //    private int nMaxBuf = 1024;

        //    // текущее количество в буфере (смещение)
        //    private int nLen;

        //    // код завершения чтения
        //    private ASRWERROR nErr;

        //    // для организации таймаута
        //    public AutoResetEvent autoEvent;

        //    // таймаут на чтение [-1 бесконечно]
        //    private int tOutRead;

        //    // Терминатор для сообщения
        //    //internal static byte[] baTermCom = Encoding.UTF8.GetBytes("END");
        //    private byte[] baTermCom = { 13, 10};
        //    // Длина терминатора
        //    private int nTLen = 2;

        //    // Кодировка возвращаемого сообщения
        //    private Encoding encCur = Encoding.UTF8;


        //    public ASReadSocketOld(Stream s)
        //        : this(s, Timeout.Infinite) { }

        //    public ASReadSocketOld(Stream s, int tOut)
        //    {
        //        stmS = s;
        //        tOutRead = tOut;
        //        buf = new byte[nMaxBuf - 1];
        //        nLen = 0;
        //    }


        //    public ASRWERROR BeginARead()
        //    {
        //        bool bRet,
        //            bQuit = false;
        //        int nTimeouts = 0;

        //        autoEvent = new AutoResetEvent(false);

        //        nLen = 0;

        //        stmS.BeginRead(buf, 0, 1, ASyncReadCBack, null);
        //        while (!bQuit)
        //        {
        //            bRet = autoEvent.WaitOne(tOutRead, false);
        //            if (bRet == false)
        //                nErr = ASRWERROR.RET_TIMEOUT;
        //            switch (nErr)
        //            {
        //                case ASRWERROR.RET_TIMEOUT:     // TimeOut
        //                    nTimeouts = nLen - nTimeouts;
        //                    if (nTimeouts == 0)
        //                        // за время таймаута ничего не прочитано, соединение будет разорвано
        //                        throw new System.Net.Sockets.SocketException((int)ASRWERROR.RET_TIMEOUT);
        //                    else
        //                        autoEvent.Reset();
        //                    break;
        //                case ASRWERROR.RET_FULLBUF:   // переполнение буфера
        //                    bQuit = true;
        //                    break;
        //                case ASRWERROR.RET_ERR:       // сетевая ошибка - надо переконнектиться
        //                    throw new System.Net.Sockets.SocketException((int)ASRWERROR.RET_ERR);
        //                case ASRWERROR.RET_FULLMSG:   // сообщение полностью получено
        //                    bQuit = true;
        //                    break;
        //                default:
        //                    throw new Exception("Ошибка чтения из потока");
        //            }
        //        }
        //        return (nErr);
        //    }

        //    // обратный вызов по окончании чтения или ошибке
        //    private void ASyncReadCBack(IAsyncResult iar)
        //    {
        //        bool bReadNext = false;
        //        string sErr;
        //        try
        //        {
        //            int nRec = stmS.EndRead(iar);
        //            nLen += nRec;
        //            if (FullMsg(buf, nLen))
        //                nErr = ASRWERROR.RET_FULLMSG;
        //            else
        //            {// читаем дальше, если есть куда
        //                if (nLen >= nMaxBuf)
        //                {
        //                    nErr = ASRWERROR.RET_FULLBUF;
        //                }
        //                else
        //                    bReadNext = true;
        //            }
        //            if (bReadNext == true)
        //                stmS.BeginRead(buf, nLen, 1, ASyncReadCBack, null);
        //            else
        //                autoEvent.Set();
        //        }
        //        catch (Exception e)
        //        {
        //            sErr = e.Message;
        //            nErr = ASRWERROR.RET_ERR;
        //            autoEvent.Set();
        //        }
        //    }

        //    // проверка на конец сообщения
        //    private bool FullMsg(byte[] data, int Count)
        //    {
        //        bool ret = false;

        //        if (!(data == null || Count < nTLen))
        //        {
        //            for (int i = nTLen; i >= 1; i--)
        //                if (baTermCom[nTLen - i] != data[Count - i])
        //                    return false;
        //            ret = true;
        //        }
        //        return (ret);
        //    }

        //    #region Свойства
        //    // Установить размер буфера
        //    public int BufSize
        //    {
        //        get { return (nMaxBuf); }
        //        set
        //        {
        //            if (value > 0)
        //            {
        //                nMaxBuf = value;
        //                buf = new byte[nMaxBuf];
        //            }
        //        }
        //    }

        //    // Установить таймаут
        //    public int TimeOutRead
        //    {
        //        get { return (tOutRead); }
        //        set
        //        {
        //            if ( (value >= 0) || (value == Timeout.Infinite) )
        //                tOutRead = value;
        //        }
        //    }

        //    // Установить терминатор
        //    public byte[] SetTerm
        //    {
        //        set
        //        {
        //            int nL = value.Length;
        //            if (nL > 0)
        //            {
        //                nTLen = nL;
        //                baTermCom = value;
        //            }
        //        }
        //    }

        //    // Установить кодировку
        //    public Encoding MsgEncoding
        //    {
        //        get
        //        {
        //            return (encCur);
        //        }
        //        set
        //        {
        //            encCur = value;
        //        }
        //    }

        //    // Получить строку-сообщение
        //    public string GetMsg()
        //    {
        //        return (encCur.GetString(buf, 0, (nLen - baTermCom.Length)));
        //    }

        //    #endregion

        //}

        public sealed class ASWriteSocket
        {
            // куда пишем
            private Stream stmS;

            // код завершения записи
            private ASRWERROR nErr;

            // для организации таймаута
            public AutoResetEvent autoEvent;

            // таймаут на чтение [-1 бесконечно]
            private int tOutWrite;

            // Кодировка посылаемого сообщения
            private Encoding encCur = Encoding.UTF8;


            public ASWriteSocket(Stream s)
                : this(s, Timeout.Infinite) { }

            public ASWriteSocket(Stream s, int tOut)
            {
                stmS = s;
                tOutWrite = tOut;
            }

            public ASRWERROR BeginAWrite(string s)
            {
                byte[] b = encCur.GetBytes(s);
                return (BeginAWrite(b, b.Length));
            }
                

            public ASRWERROR BeginAWrite(byte[] buf, int nLen)
            {
                bool bRet;

                autoEvent = new AutoResetEvent(false);

                stmS.BeginWrite(buf, 0, nLen, ASyncWriteCBack, null);
                bRet = autoEvent.WaitOne(tOutWrite, false);
                if (bRet == false)
                    nErr = ASRWERROR.RET_TIMEOUT;
                switch (nErr)
                {
                    case ASRWERROR.RET_TIMEOUT:     // TimeOut
                        throw new System.Net.Sockets.SocketException((int)ASRWERROR.RET_TIMEOUT);
                    case ASRWERROR.RET_ERR:       // сетевая ошибка - надо переконнектиться
                        throw new System.Net.Sockets.SocketException((int)ASRWERROR.RET_ERR);
                    case ASRWERROR.RET_FULLMSG:   // сообщение полностью записано
                        break;
                    default:
                        throw new Exception("Ошибка записи в поток");
                }
                return (nErr);
            }

            // обратный вызов по окончании чтения или ошибке
            private void ASyncWriteCBack(IAsyncResult iar)
            {
                bool bWriteNext = false;
                try
                {
                    stmS.EndWrite(iar);
                    nErr = ASRWERROR.RET_FULLMSG;
                    autoEvent.Set();
                }
                catch
                {
                    nErr = ASRWERROR.RET_ERR;
                    autoEvent.Set();
                }
            }



            #region Свойства

            // Установить таймаут
            public int TimeOutWrite
            {
                get { return (tOutWrite); }
                set
                {
                    if ((value >= 0) || (value == Timeout.Infinite))
                        tOutWrite = value;
                }
            }

            // Установить кодировку
            public Encoding MsgEncoding
            {
                get
                {
                    return (encCur);
                }
                set
                {
                    encCur = value;
                }
            }

            #endregion

        }



        //public sealed class ASReadSocketNF
        //{
        //    // откуда читаем
        //    private Stream stmS;

        //    // куда читаем
        //    private byte[] buf;
        //    private int nMaxBuf = 1024 * 16;

        //    // выходной файл
        //    private string m_FileName = "tmpnsi";
        //    private BinaryWriter xBW = null;
        //    private bool m_UseFile = false;

        //    // текущее количество в буфере (смещение)
        //    private int nLen;

        //    // код завершения чтения
        //    private ASRWERROR nErr;

        //    // для организации таймаута
        //    public AutoResetEvent autoEvent;

        //    // таймаут на чтение [-1 бесконечно]
        //    private int tOutRead;

        //    // Терминатор для сообщения
        //    private byte[] baTermCom;
        //    // Длина терминатора
        //    private int nTLen;

        //    // Кодировка возвращаемого сообщения
        //    private Encoding encCur = Encoding.UTF8;


        //    public ASReadSocketNF(Stream s)
        //        : this(s, Timeout.Infinite) { }

        //    public ASReadSocketNF(Stream s, int tOut)
        //    {
        //        stmS = s;
        //        tOutRead = tOut;
        //        buf = new byte[nMaxBuf];
        //        //nLen = 0;
        //        xBW = null;
            
        //        baTermCom = new byte[5]{ 13, 10, 0x2E, 13, 10 };
        //        nTLen = 5;
        //    }


        //    public ASRWERROR BeginARead()
        //    {
        //        bool bRet,
        //            bQuit = false;
        //        int nTimeouts = 0;

        //        autoEvent = new AutoResetEvent(false);

        //        nLen = 0;

        //        stmS.BeginRead(buf, 0, nMaxBuf, ASyncReadCBack, null);
        //        while (!bQuit)
        //        {
        //            bRet = autoEvent.WaitOne(tOutRead, false);
        //            if (bRet == false)
        //                nErr = ASRWERROR.RET_TIMEOUT;
        //            if (xBW != null)
        //                xBW.Close();
        //            switch (nErr)
        //            {
        //                case ASRWERROR.RET_TIMEOUT:     // TimeOut
        //                    nTimeouts = nLen - nTimeouts;
        //                    if (nTimeouts == 0)
        //                        // за время таймаута ничего не прочитано, соединение будет разорвано
        //                        throw new System.Net.Sockets.SocketException((int)ASRWERROR.RET_TIMEOUT);
        //                    else
        //                        autoEvent.Reset();
        //                    break;
        //                case ASRWERROR.RET_FULLBUF:   // переполнение буфера
        //                    bQuit = true;
        //                    break;
        //                case ASRWERROR.RET_ERR:       // сетевая ошибка - надо переконнектиться
        //                    throw new System.Net.Sockets.SocketException((int)ASRWERROR.RET_ERR);
        //                case ASRWERROR.RET_FULLMSG:   // сообщение полностью получено
        //                    bQuit = true;
        //                    break;
        //                default:
        //                    throw new Exception("Ошибка чтения из потока");
        //            }
        //        }
        //        return (nErr);
        //    }

        //    // обратный вызов по окончании чтения или ошибке
        //    private void ASyncReadCBack(IAsyncResult iar)
        //    {
        //        bool bReadNext = true;
        //        string sErr;
        //        try
        //        {
        //            int nRec = stmS.EndRead(iar);
        //            if (nRec > 0)
        //            {
        //                 if (xBW == null)
        //                {
        //                    OutFile = Path.GetTempFileName();
        //                    xBW = new BinaryWriter(File.Open(OutFile, FileMode.Create, FileAccess.ReadWrite));
        //                }
        //                xBW.Write(buf, 0, nRec);
        //                bReadNext = !FullMsg();
        //            }
        //            if (bReadNext == true)
        //                stmS.BeginRead(buf, 0, nMaxBuf, ASyncReadCBack, null);
        //            else
        //            {
        //                nErr = ASRWERROR.RET_FULLMSG;
        //                autoEvent.Set();
        //            }
        //        }
        //        catch (Exception e)
        //        {
        //            sErr = e.Message;
        //            nErr = ASRWERROR.RET_ERR;
        //            autoEvent.Set();
        //        }
        //    }

        //    // проверка на конец сообщения
        //    private bool FullMsg()
        //    {
        //        bool ret = false;
        //        byte[] b = (byte[])(baTermCom.Clone());

        //        if (xBW.BaseStream.Length >= nTLen)
        //        {
        //            xBW.BaseStream.Position -= nTLen;
        //            xBW.BaseStream.Read(b, 0, nTLen);
        //            for (int i = 0; i < nTLen; i++)
        //                if (baTermCom[i] != b[i])
        //                    return false;
        //            ret = true;
        //            xBW.BaseStream.Position -= nTLen;
        //            xBW.BaseStream.SetLength(xBW.BaseStream.Length - nTLen);
        //            xBW.Close();
        //        }
        //        return (ret);
        //    }

        //    #region Свойства

        //    // Установить размер буфера
        //    public int BufSize
        //    {
        //        get { return (nMaxBuf); }
        //        set
        //        {
        //            if (value > 0)
        //            {
        //                nMaxBuf = value;
        //                buf = new byte[nMaxBuf];
        //            }
        //        }
        //    }

        //    // Установить режим использования файла
        //    public bool UseFile
        //    {
        //        get { return (m_UseFile); }
        //        set
        //        {
        //            m_UseFile = value;
        //        }
        //    }

        //    // Установить имя файла
        //    public string OutFile
        //    {
        //        get { return (m_FileName); }
        //        set
        //        {
        //            m_FileName = value;
        //            UseFile = (m_FileName.Length == 0) ? false : true;
        //        }
        //    }

        //    // Установить таймаут
        //    public int TimeOutRead
        //    {
        //        get { return (tOutRead); }
        //        set
        //        {
        //            if ((value >= 0) || (value == Timeout.Infinite))
        //                tOutRead = value;
        //        }
        //    }

        //    // Установить терминатор
        //    public byte[] TermDat
        //    {
        //        get { return (baTermCom); }
        //        set
        //        {
        //            int nL = value.Length;
        //            if (nL > 0)
        //            {
        //                nTLen = nL;
        //                baTermCom = value;
        //            }
        //        }
        //    }

        //    // Установить кодировку
        //    public Encoding MsgEncoding
        //    {
        //        get
        //        {
        //            return (encCur);
        //        }
        //        set
        //        {
        //            encCur = value;
        //        }
        //    }

        //    #endregion

        //}


        public sealed class ASReadSocket
        {
            private Stream 
                stmS;                       // откуда читаем

            private byte[] 
                buf;                        // куда читаем

            private int 
                nLen,                       // текущее количество в буфере (смещение)
                nMaxBuf = 1024 * 32;

            private string 
                m_FileName = "";            // выходной файл

            private BinaryWriter 
                xBW = null;
            private long 
                m_FileLength = -1;
            private bool 
                m_UseFile = false;


            // код завершения чтения
            private ASRWERROR 
                nErr;

            // для организации таймаута
            public AutoResetEvent 
                autoEvent;

            // таймаут на чтение [-1 бесконечно]
            private int 
                tOutRead;

            // Терминатор для сообщения
            private byte[] 
                baTermCom;
            // Длина терминатора
            private int 
                nTLen;

            // Кодировка возвращаемого сообщения
            private Encoding 
                encCur = Encoding.UTF8;


            public ASReadSocket(Stream s)
                : this(s, Timeout.Infinite) { }

            public ASReadSocket(Stream s, int tOut)
            {
                stmS = s;
                tOutRead = tOut;
                buf = new byte[nMaxBuf];
                nLen = 0;
                UseFile = false;
                xBW = null;

                baTermCom = new byte[2] { 13, 10 };
                nTLen = 2;
            }

            public ASRWERROR BeginARead()
            {
                return( BeginARead(false, -2) );
            }

            public ASRWERROR BeginARead(int nTOut)
            {
                return (BeginARead(false, nTOut));
            }

            public ASRWERROR BeginARead(bool bUseFB, int nTOutR)
            {
                bool 
                    bRet,
                    bQuit = false;
                int 
                    nTimeouts = 0;

                if (nTOutR != -2)
                    TimeOutRead = nTOutR;

                autoEvent = new AutoResetEvent(false);

                nLen = 0;
                if ( bUseFB )
                    stmS.BeginRead(buf, 0, nMaxBuf, ASyncReadCBackF, null);
                else
                    stmS.BeginRead(buf, 0, 1, ASyncReadCBack, null);

                while (!bQuit)
                {
                    bRet = autoEvent.WaitOne(tOutRead, false);
                    if (bRet == false)
                        nErr = ASRWERROR.RET_TIMEOUT;
                    if (xBW != null)
                        xBW.Close();
                    switch (nErr)
                    {
                        case ASRWERROR.RET_TIMEOUT:     // TimeOut
                            nTimeouts = nLen - nTimeouts;
                            if (nTimeouts == 0)
                            // за время таймаута ничего не прочитано, соединение будет разорвано
                            {
                                //throw new System.Net.Sockets.SocketException((int)ASRWERROR.RET_TIMEOUT);
                                throw new System.Exception("Таймаут чтения!");
                            }
                            else
                            {
                                autoEvent.Reset();
                                nTimeouts = 0;
                            }
                            break;
                        case ASRWERROR.RET_FULLBUF:   // переполнение буфера
                            bQuit = true;
                            break;
                        case ASRWERROR.RET_ERR:       // сетевая ошибка - надо переконнектиться
                            //throw new System.Net.Sockets.SocketException((int)ASRWERROR.RET_ERR);
                            throw new System.Exception("Ошибка сети!");
                        case ASRWERROR.RET_FULLMSG:   // сообщение полностью получено
                            bQuit = true;
                            break;
                        default:
                            throw new Exception("Ошибка чтения из потока");
                    }
                }
                return (nErr);
            }

            // проверка на конец сообщения (чтение в буфер)
            private bool FullMsg(byte[] data, int Count)
            {
                bool ret = false;

                if (!(data == null || Count < nTLen))
                {
                    for (int i = nTLen; i >= 1; i--)
                        if (baTermCom[nTLen - i] != data[Count - i])
                            return false;
                    ret = true;
                }
                return (ret);
            }

            // обратный вызов по окончании чтения или ошибке (чтение в буфер)
            private void ASyncReadCBack(IAsyncResult iar)
            {
                bool bReadNext = false;
                string sErr;
                try
                {
                    int nRec = stmS.EndRead(iar);
                    nLen += nRec;
                    if (FullMsg(buf, nLen))
                        nErr = ASRWERROR.RET_FULLMSG;
                    else
                    {// читаем дальше, если есть куда
                        if (nLen >= nMaxBuf)
                        {
                            nErr = ASRWERROR.RET_FULLBUF;
                        }
                        else
                            bReadNext = true;
                    }
                    if (bReadNext == true)
                        stmS.BeginRead(buf, nLen, 1, ASyncReadCBack, null);
                    else
                        autoEvent.Set();
                }
                catch (Exception e)
                {
                    sErr = e.Message;
                    nErr = ASRWERROR.RET_ERR;
                    autoEvent.Set();
                }
            }

            // проверка на конец сообщения (чтение в файл)
            private bool FullMsg()
            {
                bool ret = false;
                byte[] b = (byte[])(baTermCom.Clone());

                if (xBW.BaseStream.Length >= nTLen)
                {
                    xBW.BaseStream.Position -= nTLen;
                    xBW.BaseStream.Read(b, 0, nTLen);
                    for (int i = 0; i < nTLen; i++)
                        if (baTermCom[i] != b[i])
                            return false;
                    ret = true;
                    xBW.BaseStream.Position -= nTLen;
                    m_FileLength = xBW.BaseStream.Length - nTLen;
                    xBW.BaseStream.SetLength(m_FileLength);
                    xBW.Close();
                }
                return (ret);
            }

            // обратный вызов по окончании чтения или ошибке (чтение в файл)
            private void ASyncReadCBackF(IAsyncResult iar)
            {
                bool bReadNext = true;
                string sErr;
                try
                {
                    int nRec = stmS.EndRead(iar);
                    if (nRec > 0)
                    {
                        if (xBW == null)
                        {
                            OutFile = Path.GetTempFileName();
                            xBW = new BinaryWriter(File.Open(OutFile, FileMode.Create, FileAccess.ReadWrite));
                        }
                        xBW.Write(buf, 0, nRec);
                        bReadNext = !FullMsg();
                    }
                    if (bReadNext == true)
                        stmS.BeginRead(buf, 0, nMaxBuf, ASyncReadCBackF, null);
                    else
                    {
                        nErr = ASRWERROR.RET_FULLMSG;
                        autoEvent.Set();
                    }
                }
                catch (Exception e)
                {
                    sErr = e.Message;
                    nErr = ASRWERROR.RET_ERR;
                    autoEvent.Set();
                }
            }


            #region Свойства

            // Установить размер буфера
            public int BufSize
            {
                get { return (nMaxBuf); }
                set
                {
                    if (value > 0)
                    {
                        nMaxBuf = value;
                        buf = new byte[nMaxBuf];
                    }
                }
            }

            // Установить режим использования файла
            public bool UseFile
            {
                get { return (m_UseFile); }
                set
                {
                    m_UseFile = value;
                }
            }

            // Установить имя файла
            public string OutFile
            {
                get { return (m_FileName); }
                set
                {
                    m_FileName = value;
                    UseFile = (m_FileName.Length == 0) ? false : true;
                }
            }

            // Получить длину файла
            public long FileLength
            {
                get { return (m_FileLength); }
            }


            // Установить таймаут
            public int TimeOutRead
            {
                get { return (tOutRead); }
                set
                {
                    if ((value >= 0) || (value == Timeout.Infinite))
                        tOutRead = value;
                }
            }

            // Установить терминатор
            public byte[] TermDat
            {
                get { return (baTermCom); }
                set
                {
                    int nL = value.Length;
                    if (nL > 0)
                    {
                        nTLen = nL;
                        baTermCom = value;
                    }
                }
            }

            // Установить кодировку
            public Encoding MsgEncoding
            {
                get
                {
                    return (encCur);
                }
                set
                {
                    encCur = value;
                }
            }

            // Получить строку-сообщение
            public string GetMsg()
            {
                //int n = nLen - nTLen;
                return (encCur.GetString(buf, 0, (nLen - nTLen)));


                //string s = encCur.GetString(buf, 0, (nLen - nTLen));
                //return (s);

            }

            #endregion

        }


        /// <summary>
        /// Starts the Thread that listening port (ListenPort property) for incoming messages from server
        /// </summary>
        public void ListenStart()
        {
            if (!_isListening)
            {
                ThreadPool.QueueUserWorkItem(new WaitCallback(listening));
                _isListening = true;
            }
        }

        /// <summary>
        /// Stop the Thread that listening port (ListenPort property)
        /// </summary>
        public void ListenStop()
        {
            if (_isListening)
            {
                _isListening = false;
            }
        }

        private void listening(object state)
        {
            // establish connection with client
            IPEndPoint endPoint = new IPEndPoint(IPAddress.Parse("0.0.0.0"), _serverPort);

            do
            {
                try
                {
/*
                    _listen = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

                    _listen.Blocking = true;
                    _listen.Bind(endPoint);
                    _listen.Listen(0);

                    // block here until establish a connection
                    _socket = _listen.Accept();

                    // we connected with a client, shutdown the listen socket
                    // so we won't connect with another client
                    _listen.Close();

                    // sit in this loop until connection is broken
                    // handle client commands and send back response
                    int bytesRead = 0;

                    // hold incoming data
                    byte[] buf = new byte[1024 * 10];

                    bytesRead = _socket.Receive(buf);
                    MessageFromServerRecived(buf, bytesRead);

*/


                    _listen = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

                    _listen.Blocking = true;
                    _listen.Bind(endPoint);
                    _listen.Listen(0);

                    // block here until establish a connection
                    _socket = _listen.Accept();

                    // we connected with a client, shutdown the listen socket
                    // so we won't connect with another client

                    // sit in this loop until connection is broken
                    // handle client commands and send back response
                    int bytesRead = 0;

                    // hold incoming data
                    byte[] buf = new byte[1024 * 10];

                    while (_isListening)
                    {
                        bytesRead = _socket.Receive(buf);
                        MessageFromServerRecived(buf, bytesRead);
                        bytesRead = 22;
                    }

                    _listen.Close();


                }

                catch (Exception ex)
                {
                    ErrorRaised(ex);
                }

                _socket.Shutdown(SocketShutdown.Both);

            } while (_isListening);
        }

        public void SendToLister(string text)
        {
            if (_socket != null)
            {
                NetworkStream st = new NetworkStream(_socket);
                Byte[] outStream = Encoding.UTF8.GetBytes(text);
                st.Write(outStream, 0, outStream.Length);
                st.Close();
            }
        }

        private bool _IsConnectPresent()
        {
            bool bRet = false;

            // This is how you can determine whether a socket is still connected.
            bool blockingState = _ClientSock.Blocking;
            try
            {
                byte[] tmp = new byte[1];
                _ClientSock.Blocking = false;
                _ClientSock.Send(tmp, 0,SocketFlags.None );
                bRet = true;
            }
            catch (SocketException e)
            {
                // 10035 == WSAEWOULDBLOCK
                if (e.NativeErrorCode.Equals(10035))
                    bRet = true;
            }
            catch (Exception e)
            {
                // 10035 == WSAEWOULDBLOCK
                if (e.Message == "")
                    bRet = true;
            }
            finally
            {
                _ClientSock.Blocking = blockingState;
            }

            return(bRet);
        }
        
        #region Class Properties

        /// <summary>
        /// IP address remote server
        /// </summary>
        public IPAddress ServerIP
        {
            get { return _serverIP; }
            set { _serverIP = value; }
        }

        /// <summary>
        /// remote server TCP port
        /// </summary>
        public int ServerPort
        {
            get { return _serverPort; }
            set { _serverPort = value; }
        }


        /// <summary>
        /// DNS-name of remote server
        /// </summary>
        public string ServerID
        {
            get { return _serverID; }
            set { _serverID = value; }
        }

        // текущий поток TCP-клиента
        public Stream SStream
        {
            get { return myStream; }
        }

        /// <summary>
        /// Indicates the state of connection
        /// </summary>
        public bool isConnected
        {
            get { return (_ClientSock == null)?false: _IsConnectPresent(); }
        }

        /// <summary>
        /// Indicates the state of listening
        /// </summary>
        public bool isListening
        {
            get { return _isListening; }
        }

        // Объект асинхронного чтения
        public ASReadSocket ASReadS
        {
            get { return _xASR; }
            set { _xASR = value; }
        }

        // Объект асинхронного чтения
        //public ASReadSocket ASReadSF
        //{
        //    get { return _xASRF; }
        //    set { _xASRF = value; }
        //}

        // Объект асинхронной записи
        public ASWriteSocket ASWriteS
        {
            get { return _xASW; }
            set { _xASW = value; }
        }

        #endregion

        #region old listener

        //////////////////////////////////////////////////////////////////////////
        //OLD LISTENER
        //////////////////////////////////////////////////////////////////////////


        /*private void listening(object state)
        {
            Byte[] srvStream = new Byte[1024];
            Socket _socket;
            int count;
            myListen = new TcpListener(_serverPort);
            myListen.Start();

            do
            {
                try
                {
                    if (myListen.Pending())
                    {
                        _socket = myListen.AcceptSocket();
                         count = _socket.Receive(srvStream, srvStream.Length, 0);
                         MessageFromServerRecived(srvStream, count);
                        _socket.Shutdown(SocketShutdown.Both);
                        _socket.Close();

                    }
                }
                catch (Exception ex)
                {
                    ErrorRaised(ex);
                }

            } while (_isListening == true);
            myListen.Stop();
        }*/
        #endregion
    }



}
