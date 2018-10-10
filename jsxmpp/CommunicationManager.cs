namespace jsxmpp
{
    using Newtonsoft.Json;
    using S22.Xmpp;
    using S22.Xmpp.Client;
    using S22.Xmpp.Core;
    using S22.Xmpp.Extensions;
    using S22.Xmpp.Im;
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Runtime.InteropServices;
    using System.Threading;
    using System.Xml;

    public class CommunicationManager : IDisposable
    {
        private bool m_boolSendFileResult;
        private Dictionary<string, ServiceRequestParam> m_dictionarySending = new Dictionary<string, ServiceRequestParam>();
        private Dictionary<string, Availability> m_dictionaryState = new Dictionary<string, Availability>();
        private FileTransferMonitor m_fileHandler;
        private int m_intPort = 0x1466;
        private Hashtable m_keyAndSource = new Hashtable();
        private object m_lock = new object();
        private object m_lockConection = new object();
        private ServiceRequestHandler m_reqHandler;
        private ServiceResponseHandler m_respHandler;
        private int m_sendErrorTimes = 0;
        private string m_stringDomain = string.Empty;
        private string m_stringPassword = string.Empty;
        private string m_stringRecvFilePath = string.Empty;
        private string m_stringResource = string.Empty;
        private string m_stringUserName = string.Empty;
        private Thread m_threadCheck;
        private DateTime m_threadCheck_lastRunTime = DateTime.Now;
        private Thread m_threadPresence;
        /// <summary>
        /// 自发自收线程
        /// </summary>
        private Thread m_threadSelfCheck;
        private Thread m_threadWatch;
        private XmppClient m_xmppClient;
        private int ReconnectTimes = 0;
        /// <summary>
        /// 是否处于已连接状态
        /// </summary>
        private bool IsConnecting = false;

        public CommunicationManager(string domain, string username, string resource, string password, string recvFilePath, int port = 0x1466)
        {
            try
            {
                this.m_stringDomain = domain;
                this.m_stringPassword = password;
                this.m_stringRecvFilePath = recvFilePath;
                this.m_stringResource = resource;
                this.m_stringUserName = username;
                this.m_intPort = port;
                this.connect();
            }
            catch (Exception exception)
            {
                CommonConfig.Logger.WriteError("初始化过程出现异常", exception);
            }
            if (this.m_threadCheck == null)
            {
                this.m_threadCheck = new Thread(new ThreadStart(this.CheckConnection));
                this.m_threadCheck.IsBackground = true;
            }
            try
            {
                this.m_threadCheck.Start();
                CommonConfig.Logger.WriteInfo("检测状态线程已运行");
            }
            catch
            {
            }
            if (this.m_threadPresence == null)
            {
                this.m_threadPresence = new Thread(new ThreadStart(this.CheckPresence));
                this.m_threadPresence.IsBackground = true;
            }
            try
            {
                this.m_threadPresence.Start();
                CommonConfig.Logger.WriteInfo("心跳线程已运行");
            }
            catch
            {
            }
            try
            {
                if (this.m_threadSelfCheck == null)
                {
                    this.m_threadSelfCheck = new Thread(new ThreadStart(this.SendForSelf));
                    this.m_threadSelfCheck.IsBackground = true;
                }
                this.m_threadSelfCheck.Start();
                CommonConfig.Logger.WriteInfo("自发自收线程启动");
            }
            catch (Exception ex)
            {
            }
            if (this.m_threadWatch == null)
            {
                this.m_threadWatch = new Thread(new ThreadStart(this.CheckWatch));
                this.m_threadWatch.IsBackground = true;
            }
            try
            {
                this.m_threadWatch.Start();
                CommonConfig.Logger.WriteInfo("守护线程已运行");
            }
            catch
            {
            }
        }

        private void CheckConnection()
        {
            while (true)
            {
                Thread.Sleep(0x1f40);
                this.m_threadCheck_lastRunTime = DateTime.Now;
                CommonConfig.Logger.WriteInfo("进入检测连接状态");
                try
                {
                    if (!this.IsXmppOK)
                    {
                        CommonConfig.Logger.WriteInfo(string.Format("连接状态异常，关闭连接：IsHasRosterOnline:{0},Connected:{1},IsConnecting:{2}", (m_xmppClient!=null?this.m_xmppClient.IsHasRosterOnline.ToString():""), (m_xmppClient != null ? this.m_xmppClient.Connected.ToString():"false"), this.IsConnecting));
                        
                        //CommonConfig.Logger.WriteInfo(string.Format("连接状态异常，关闭连接：IsHasRosterOnline:{0},Connected:{1},m_sendErrorTimes:{2}", this.m_xmppClient.IsHasRosterOnline, this.m_xmppClient.Connected, this.m_sendErrorTimes));
                        if (this.ReconnectTimes > 2)
                        {
                            CommonConfig.Logger.WriteInfo("连续重连两次不成功，暴力退出！");
                        }
                        this.Close();
                        Thread.Sleep(0x3e8);
                        this.ReconnectTimes++;
                        this.connect();
                    }
                }
                catch (Exception exception)
                {
                    CommonConfig.Logger.WriteError("定时检测连接状态任务出错", exception);
                }
                CommonConfig.Logger.WriteInfo("退出检测连接状态");
            }
        }

        private void CheckPresence()
        {
            while (true)
            {
                Thread.Sleep(0xea60);
                CommonConfig.Logger.WriteInfo("进入定时发送在线心跳线程方法");
                bool flag = false;
                if (this.m_xmppClient != null)
                {
                    try
                    {
                        if (this.IsXmppOK)
                        {
                            flag = true;
                        }
                    }
                    catch (Exception exception)
                    {
                        CommonConfig.Logger.WriteError("发送定时检测心跳出错。", exception);
                    }
                }
                if (!flag)
                {
                    CommonConfig.Logger.WriteInfo("未连接状态或m_xmppClient对象为空，不发送检测心跳信息。");
                }
            }
        }

        private void CheckWatch()
        {
            while (true)
            {
                Thread.Sleep(0x7530);
                CommonConfig.Logger.WriteInfo("进入守护线程方法");
                try
                {
                    if (DateTime.Now > this.m_threadCheck_lastRunTime.AddMinutes(5.0))
                    {
                        CommonConfig.Logger.WriteInfo("检测线程挂死，重启中");
                        if (this.m_threadCheck != null)
                        {
                            try
                            {
                                this.m_threadCheck.Abort();
                            }
                            catch (Exception exception1)
                            {
                                Exception exception = exception1;
                                CommonConfig.Logger.WriteError("退出检测线程：" + exception.ToString(), null);
                            }
                            this.m_threadCheck = null;
                        }
                        this.m_threadCheck = new Thread(new ThreadStart(this.CheckConnection));
                        this.m_threadCheck.IsBackground = true;
                        this.m_threadCheck.Start();
                        CommonConfig.Logger.WriteInfo("完成重启检测线程");
                    }
                }
                catch (Exception exception2)
                {
                    CommonConfig.Logger.WriteInfo("重启检测线程过程出错：" + exception2.ToString());
                }
                CommonConfig.Logger.WriteInfo("退出守护线程方法");
            }
        }

        private void client_StatusChanged(object sender, StatusEventArgs e)
        {
            string key = e.Jid.Node + "@" + e.Jid.Domain;
            if (!this.m_dictionaryState.ContainsKey(key))
            {
                this.m_dictionaryState.Add(key, e.Status.Availability);
            }
            else
            {
                this.m_dictionaryState[key] = e.Status.Availability;
            }
        }

        private void Close()
        {
            m_sendForSelfErrorTimes = 0;
            lock (this.m_lockConection)
            {
                IsConnecting = false;
                if (this.m_xmppClient != null)
                {
                    try
                    {
                        CommonConfig.Logger.WriteInfo("正在关闭当前连接。。。");
                        this.m_xmppClient.StatusChanged -= new EventHandler<StatusEventArgs>(this.client_StatusChanged);
                        this.m_xmppClient.IqRequestEvents -= new EventHandler<S22.Xmpp.Im.IqEventArgs>(this.m_xmppClient_IqRequestEvents);
                        this.m_xmppClient.IqResponseEvents -= new EventHandler<S22.Xmpp.Im.IqEventArgs>(this.m_xmppClient_IqResponseEvents);
                        this.m_xmppClient.FileTransferProgress -= new EventHandler<FileTransferProgressEventArgs>(this.m_xmppClient_FileTransferProgress);
                        this.m_xmppClient.FileTransferAborted -= new EventHandler<FileTransferAbortedEventArgs>(this.m_xmppClient_FileTransferAborted);
                        this.m_xmppClient.Close();
                        CommonConfig.Logger.WriteInfo("完成关闭当前连接。。。");
                    }
                    catch (Exception exception)
                    {
                        CommonConfig.Logger.WriteError("关闭连接过程出现错误。", exception);
                    }
                }
                this.m_sendErrorTimes = 0;
                this.m_xmppClient = null;
            }
        }

        public bool connect()
        {
            lock (this.m_lockConection)
            {
                if (this.m_xmppClient == null)
                {
                    try
                    {
                        this.m_xmppClient = new XmppClient(this.m_stringDomain, this.m_stringUserName, this.m_stringPassword, this.m_intPort, true, null);
                        this.m_xmppClient.SubscriptionRequest = new SubscriptionRequest(this.onSubscriptionRequest);
                        this.m_xmppClient.StatusChanged += new EventHandler<StatusEventArgs>(this.client_StatusChanged);
                        this.m_xmppClient.IqRequestEvents += new EventHandler<S22.Xmpp.Im.IqEventArgs>(this.m_xmppClient_IqRequestEvents);
                        this.m_xmppClient.IqResponseEvents += new EventHandler<S22.Xmpp.Im.IqEventArgs>(this.m_xmppClient_IqResponseEvents);
                        this.m_xmppClient.FileTransferRequest = new FileTransferRequest(this.OnFileTransferRequest);
                        this.m_xmppClient.FileTransferProgress += new EventHandler<FileTransferProgressEventArgs>(this.m_xmppClient_FileTransferProgress);
                        this.m_xmppClient.FileTransferAborted += new EventHandler<FileTransferAbortedEventArgs>(this.m_xmppClient_FileTransferAborted);
                        this.m_xmppClient.Tls = false;
                        CommonConfig.TempFilePath = AppDomain.CurrentDomain.BaseDirectory + "httpFileDown";
                        if (!Directory.Exists(CommonConfig.TempFilePath))
                        {
                            Directory.CreateDirectory(CommonConfig.TempFilePath);
                        }
                        CommonConfig.LogPath = AppDomain.CurrentDomain.BaseDirectory + "log";
                        if (!Directory.Exists(CommonConfig.LogPath))
                        {
                            Directory.CreateDirectory(CommonConfig.LogPath);
                        }
                        this.m_xmppClient.Connect(this.m_stringResource);
                        IsConnecting = true;
                        this.ReconnectTimes = 0;
                    }
                    catch (Exception exception)
                    {
                        CommonConfig.Logger.WriteError("连接过程出错", exception);
                    }
                }
            }
            return this.m_xmppClient.Connected;
        }

        public void disconnected()
        {
            try
            {
                this.Close();
            }
            catch (Exception ex)
            {
                CommonConfig.Logger.WriteError("disconnected过程出错", ex);
            }
        }

        public void Dispose()
        {
            CommonConfig.Logger.WriteInfo("释放Dispose组件");
            if (this.m_threadWatch != null)
            {
                try
                {
                    this.m_threadWatch.Abort();
                }
                catch
                {
                }
            }
            if (this.m_threadCheck != null)
            {
                try
                {
                    this.m_threadCheck.Abort();
                }
                catch
                {
                }
            }
            if (this.m_threadPresence != null)
            {
                try
                {
                    this.m_threadPresence.Abort();
                }
                catch
                {
                }
            }
            if (this.m_threadSelfCheck != null)
            {
                try
                {
                    this.m_threadSelfCheck.Abort();
                }
                catch
                {
                }
            }
            this.m_threadWatch = null;
            this.m_threadCheck = null;
            this.m_threadPresence = null;
            this.m_threadSelfCheck = null;
            try
            {
                this.Close();
            }
            catch (Exception ex)
            {
                CommonConfig.Logger.WriteError("Dispose过程出错", ex);
            }
        }

        private void fileTransferCallback(bool accepted, FileTransfer transfer)
        {
            if (!accepted)
            {
                CommonConfig.Logger.WriteInfo("拒绝接收文件：" + transfer.Name);
                Monitor.Enter(this.m_lock);
                Monitor.Pulse(this.m_lock);
                Monitor.Exit(this.m_lock);
                if (this.m_fileHandler != null)
                {
                    this.m_fileHandler.onRefused();
                }
            }
        }

        public int getConnectionStatus(string toUserJID)
        {
            int length = -1;
            length = toUserJID.IndexOf("/");
            if (length > 0)
            {
                toUserJID = toUserJID.Substring(0, length);
            }
            if (this.m_dictionaryState.ContainsKey(toUserJID))
            {
                Availability availability = this.m_dictionaryState[toUserJID];
                switch (availability)
                {
                    case Availability.Online:
                    case Availability.Away:
                    case Availability.Chat:
                        return 2;
                }
                return (int)availability;
            }
            return 0;
        }

        private int getDataCrc(string jsonData)
        {
            int num = 0;
            for (int i = 0; i < jsonData.Length; i++)
            {
                num = (0x1f * num) + jsonData[i];
            }
            return num;
        }

        public string getSeqId()
        {
            return Guid.NewGuid().ToString();
        }

        private void m_xmppClient_FileTransferAborted(object sender, FileTransferAbortedEventArgs e)
        {
            CommonConfig.Logger.WriteInfo("接收文件过程出现异常：" + e.Transfer.Name);
            Monitor.Enter(this.m_lock);
            this.m_boolSendFileResult = false;
            Monitor.Pulse(this.m_lock);
            Monitor.Exit(this.m_lock);
        }

        private void m_xmppClient_FileTransferProgress(object sender, FileTransferProgressEventArgs e)
        {
            if ((e.Transfer.Transferred / e.Transfer.Size) == 1L)
            {
                this.m_boolSendFileResult = true;
                Monitor.Enter(this.m_lock);
                Monitor.Pulse(this.m_lock);
                Monitor.Exit(this.m_lock);
                CommonConfig.Logger.WriteInfo("文件已发送完成100%");
            }
        }

        /// <summary>
        /// 接收到消息请求
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void m_xmppClient_IqRequestEvents(object sender, S22.Xmpp.Im.IqEventArgs e)
        {
            string innerText = e.IqInfo.Data["dreq"]["cnt"].InnerText;
            string message = "业务平台未实现，未处理结果";
            int num = 1;
            ServiceRequestParam requestParam = null;
            try
            {
                string iqID = "";
                if (e != null && e.IqInfo != null)
                    iqID = e.IqInfo.Id;
                CommonConfig.Logger.WriteInfo("接收到的数据推送，IqID=："+ iqID);
                requestParam = JsonConvert.DeserializeObject<ServiceRequestParam>(XmlHelper.ResumeChar(innerText));
                if (requestParam != null)
                {
                    if (this.m_reqHandler != null && requestParam.serviceId != m_serviceIdForSelf)
                    {
                        if (!this.m_keyAndSource.Contains(e.IqInfo.Id))
                        {
                            this.m_keyAndSource.Add(e.IqInfo.Id, requestParam.source);
                        }
                        this.m_reqHandler.execute(e.Jid.ToString(), requestParam, e.IqInfo.Id, "SYNC");
                        return;
                    }
                    else if (requestParam.serviceId == m_serviceIdForSelf)   //直接返回成功
                    {
                        CommonConfig.Logger.WriteInfo("接收到自发自收心跳");
                        ServiceResponseData responseData = new ServiceResponseData
                        {
                            seqId = requestParam.seqId,
                            message = "",
                            resultCode = 0,
                            serviceId = m_serviceIdForSelf
                        };
                        this.responseService(e.Jid.ToString(), responseData, e.IqInfo.Id, "", true);
                    }
                    else
                    {
                        //直接回复，返回失败，未实现
                        ServiceResponseData responseData = new ServiceResponseData
                        {
                            seqId = (requestParam != null) ? requestParam.seqId : "0",
                            message = message,
                            resultCode = num
                        };
                        this.responseService(e.Jid.ToString(), responseData, e.IqInfo.Id, "", true);
                    }
                }
            }
            catch (Exception exception)
            {
                CommonConfig.Logger.WriteError("接收到消息返回给业务系统出错", exception);
                message = exception.Message;
                num = 0x65;
            }
        }

        private void m_xmppClient_IqResponseEvents(object sender, S22.Xmpp.Im.IqEventArgs e)
        {
            string innerText = e.IqInfo.Data["dreq"]["cnt"].InnerText;
            if (this.m_respHandler != null)
            {
                ServiceResponseData responseData = JsonConvert.DeserializeObject<ServiceResponseData>(XmlHelper.ResumeChar(innerText));
                this.m_respHandler.execute(e.Jid.ToString(), responseData);
            }
        }

        private string OnFileTransferRequest(FileTransfer transfer)
        {
            if (this.m_fileHandler != null)
            {
                return this.m_fileHandler.onRequest(transfer);
            }
            string name = transfer.Name;
            if (!string.IsNullOrEmpty(this.m_stringRecvFilePath))
            {
                name = this.m_stringRecvFilePath + "//" + name;
            }
            CommonConfig.Logger.WriteInfo("接收到文件：" + name);
            return name;
        }

        private bool onSubscriptionRequest(Jid from)
        {
            return true;
        }

        public void RegisterFileRequestHandler(FileTransferMonitor fileHandler)
        {
            this.m_fileHandler = fileHandler;
        }

        public void RegisterRequestHandler(ServiceRequestHandler reqHandler)
        {
            this.m_reqHandler = reqHandler;
        }

        public void RegisterResponseHandler(ServiceResponseHandler respHandler)
        {
            this.m_respHandler = respHandler;
        }

        public int requestService(string toUserJID, ServiceRequestParam requestParam, int mode, bool bCrc)
        {
            ServiceResponseData responseData = new ServiceResponseData();
            return this.syncRequestService(toUserJID, requestParam, mode, bCrc, -1, ref responseData);
        }

        public int responseService(string toUserJID, ServiceResponseData responseData, string id, string mode, bool bCrc)
        {
            if (!(((responseData == null) || (responseData.attributes == null)) || responseData.attributes.Contains("AUTH_CODE")))
            {
                responseData.attributes.Add("AUTH_CODE", "");
            }
            if (string.IsNullOrEmpty(responseData.source))
            {
                object obj2 = this.m_keyAndSource[id];
                if (obj2 != null)
                {
                    responseData.source = obj2.ToString();
                    this.m_keyAndSource.Remove(id);
                }
            }
            string jsonData = JsonConvert.SerializeObject(responseData);
            int crc = 0;
            if (bCrc)
            {
                crc = this.getDataCrc(jsonData);
            }
            try
            {
                if (IsXmppOK)
                {
                    IqType result = IqType.Result;
                    this.m_xmppClient.IqResponseJieShun(result, id, jsonData, crc, responseData.resultCode, mode, toUserJID, this.m_stringUserName + "@" + this.m_stringDomain + "/" + this.m_stringResource, null);
                    return 1;
                }
            }
            catch (Exception exception)
            {
                CommonConfig.Logger.WriteError("响应数据错误", exception);
            }
            return 0;
        }

        public void SendBeat(string toUserJID)
        {
            if (this.m_xmppClient != null)
            {
                this.m_xmppClient.IqRequestBeatJieShun(toUserJID);
            }
        }

        public bool sendFile(string toUserJID, FileDescription desc, int type)
        {
            try
            {
                if (this.IsXmppOK)
                {
                    CommonConfig.FileTranType = type;
                    CommonConfig.Logger.WriteInfo("等待发送文件，等待锁定资源" + desc.fileName);
                    Monitor.Enter(this.m_lock);
                    CommonConfig.Logger.WriteInfo("发送文件开始，锁定资源" + desc.fileName);
                    this.m_boolSendFileResult = false;
                    try
                    {
                        Stopwatch stopwatch = new Stopwatch();
                        stopwatch.Start();
                        this.m_xmppClient.InitiateFileTransfer(toUserJID, desc.fileName, JsonConvert.SerializeObject(desc), new Action<bool, FileTransfer>(this.fileTransferCallback));
                        Monitor.Wait(this.m_lock, 0x4e20);
                        CommonConfig.Logger.WriteInfo(string.Concat(new object[] { "发送文件完成，结果:", this.m_boolSendFileResult.ToString(), ",耗时：", stopwatch.ElapsedMilliseconds }));
                        stopwatch.Stop();
                        return this.m_boolSendFileResult;
                    }
                    finally
                    {
                        CommonConfig.Logger.WriteInfo("发送文件结束，释放资源" + desc.fileName);
                        Monitor.Exit(this.m_lock);
                    }
                }
            }
            catch (Exception exception)
            {
                CommonConfig.Logger.WriteError("发送文件过程出错。", exception);
            }
            return false;
        }

        public void setShowLog(bool bShow = true)
        {
            CommonConfig.IsPrintLog = bShow;
        }

        public void subScribe(string strJID, string strAlias = null, string strGroups = null)
        {
            if (!string.IsNullOrEmpty(strJID))
            {
                strJID = strJID.Split(new char[] { '/' })[0];
                Jid jid = new Jid(strJID);
                string[] groups = null;
                if (!string.IsNullOrEmpty(strGroups))
                {
                    groups = new string[] { strGroups };
                }
                try
                {
                    this.m_xmppClient.AddContact(jid, strAlias, groups);
                }
                catch (Exception exception)
                {
                    CommonConfig.Logger.WriteError("添加好友失败", exception);
                }
            }
        }

        public int syncRequestService(string toUserJID, ServiceRequestParam requestParam, int mode, bool bCrc, int timeout, ref ServiceResponseData responseData)
        {
            string jsonData = JsonConvert.SerializeObject(requestParam);
            int crc = 0;
            int result = 1;
            if (bCrc)
            {
                crc = this.getDataCrc(jsonData);
            }
            string str2 = "SYNC";
            if (mode == 0)
            {
                str2 = "NOTIFY";
            }
            try
            {
                Stopwatch stopwatch = new Stopwatch();
                stopwatch.Start();
                if ((mode != 0) && (timeout < 0x1388))
                {
                    timeout = 0x1388;
                }

                responseData = new ServiceResponseData();
                responseData.resultCode = result;

                if (this.IsXmppOK)
                {
                    Iq iq = this.m_xmppClient.IqRequestJieShun(toUserJID, jsonData, crc, str2, IqType.Get, timeout, requestParam.seqId);
                    stopwatch.Stop();
                    CommonConfig.Logger.WriteInfo("syncRequestService发送完成，耗时：" + stopwatch.ElapsedMilliseconds);
                    string str3 = iq.Data.Attributes["type"].Value;

                    if (str3.ToLower().Equals("error"))
                    {
                        responseData.message = iq.Data.InnerXml;
                        return result;
                    }
                    XmlElement data = iq.Data;
                    if (data["dres"] != null)
                    {
                        if (data["dres"]["rc"] != null)
                        {
                            int.TryParse(data["dres"]["rc"].InnerText, out result);
                        }
                        if (data["dres"]["cnt"] != null)
                        {
                            responseData = JsonConvert.DeserializeObject<ServiceResponseData>(data["dres"]["cnt"].InnerText);
                        }
                        else
                        {
                            responseData.message = data.InnerXml;
                        }
                    }
                    this.m_sendErrorTimes = 0;
                    return result;
                }
                responseData.resultCode = 0x194;
                CommonConfig.Logger.WriteInfo(string.Format("未连接状态不发送数据，IsHasRosterOnline:{0},Connected:{1},m_sendErrorTimes:{2}", this.m_xmppClient.IsHasRosterOnline, this.m_xmppClient.Connected, this.m_sendErrorTimes));
            }
            catch (Exception exception)
            {
                if (mode != 0)
                {
                    this.m_sendErrorTimes++;
                    CommonConfig.Logger.WriteError("发送数据过程出错。超时时间：" + timeout, exception);
                }
                else
                {
                    CommonConfig.Logger.WriteError("发送数据过程出错", exception);
                }
            }
            return result;
        }

        public bool IsXmppOK
        {
            get
            {
                if (!((this.m_xmppClient != null) && this.m_xmppClient.Connected && IsConnecting))
                {
                    return false;
                }
                return true;
            }
        }

        /// <summary>
        /// 自发自收对应的心跳标志
        /// </summary>
        private string m_serviceIdForSelf = "BeatServiceIdForSelf";
        /// <summary>
        /// 自发自收连续发送失败次数
        /// </summary>
        private int m_sendForSelfErrorTimes = 0;
        private void SendForSelf()
        {
            while (true)
            {
                //1分钟检测一次
                Thread.Sleep(15 * 1000);
                try
                {
                    CommonConfig.Logger.WriteInfo("开始自发自收");
                    string selfJID = m_stringUserName + "@" + m_stringDomain + "/" + m_stringResource;
                    ServiceRequestParam request = new ServiceRequestParam();
                    request.serviceId = m_serviceIdForSelf;
                    request.source = DateTime.Now.ToString("HHmmssfff");
                    ServiceResponseData response = new ServiceResponseData();
                    response.resultCode = 1;
                    int result = syncRequestService(selfJID, request, 1, false, 5000, ref response);
                    if (response.resultCode == 0)
                    {
                        m_sendForSelfErrorTimes = 0;
                    }
                    else
                    {
                        m_sendForSelfErrorTimes++;
                    }
                    CommonConfig.Logger.WriteInfo("完成自发自收，m_sendForSelfErrorTimes=" + m_sendForSelfErrorTimes);
                }
                catch (Exception ex)
                {
                    m_sendForSelfErrorTimes++;
                    CommonConfig.Logger.WriteError("自发自收过程失败", ex);
                }

                try
                {
                    if (m_sendForSelfErrorTimes >= 3)  //连续3次失败
                    {
                        CommonConfig.Logger.WriteInfo("自发自收失败次数超过3次，准备重连");
                        m_sendForSelfErrorTimes = 0;
                        //重连。。。
                        this.Close();
                    }
                }
                catch (Exception ex1)
                {
                    CommonConfig.Logger.WriteError("自发自收超过3次，关闭连接过程出错", ex1);
                }
            }
        }
    }
}

