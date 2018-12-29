namespace S22.Xmpp
{
    using System;
    using System.Collections.Concurrent;
    using System.IO;
    using System.Runtime.InteropServices;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;

    public class LogManage
    {
        private int CheckDeleteDay;
        private int DeleteFileDay;
        private ConcurrentQueue<LogEntity> LogCache;
        private Thread LogThread;
        private string RelativePath;
        private int WriteCountPer;

        private LogManage()
        {
            this.LogCache = new ConcurrentQueue<LogEntity>();
            this.WriteCountPer = 0x3e8;
            this.DeleteFileDay = 30;
            this.CheckDeleteDay = 0;
            this.Init();
        }

        public LogManage(string relativePath = "Log", int deleteFileDay = 45)
        {
            this.LogCache = new ConcurrentQueue<LogEntity>();
            this.WriteCountPer = 0x3e8;
            this.DeleteFileDay = deleteFileDay;
            this.CheckDeleteDay = 0;
            if (string.IsNullOrEmpty(relativePath))
            {
                this.RelativePath = "Log";
            }
            else
            {
                this.RelativePath = relativePath;
            }
            this.Init();
        }

        private void DeleteLogProcess()
        {
            string[] strArray = Directory.GetFiles(this.FilePath, "*.log", SearchOption.TopDirectoryOnly);
            foreach (string str in strArray)
            {
                if (File.GetCreationTime(str).AddDays((double) this.DeleteFileDay) < DateTime.Now.Date)
                {
                    File.Delete(str);
                }
            }
        }

        private void Init()
        {
            string[] strArray = this.RelativePath.Replace("//", "/").Replace(@"\", "/").Split(new char[] { '/' });
            string baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
            foreach (string str2 in strArray)
            {
                if (!string.IsNullOrEmpty(str2))
                {
                    baseDirectory = baseDirectory + "/" + str2;
                    if (!Directory.Exists(baseDirectory))
                    {
                        Directory.CreateDirectory(baseDirectory);
                    }
                }
            }
            this.LogThread = new Thread(new ThreadStart(this.LogProcess));
            this.LogThread.IsBackground = true;
            this.LogThread.Start();
        }

        private void LogProcess()
        {
            Action action = null;
            if (!Directory.Exists(this.FilePath))
            {
                Directory.CreateDirectory(this.FilePath);
            }
            while (true)
            {
                try
                {
                    int num = (this.LogCache.Count < this.WriteCountPer) ? this.LogCache.Count : this.WriteCountPer;
                    if (num > 0)
                    {
                        StringBuilder builder = new StringBuilder();
                        for (int i = 0; i < num; i++)
                        {
                            LogEntity entity;
                            if (this.LogCache.TryDequeue(out entity))
                            {
                                builder.Append(entity.LogType);
                                builder.Append(" | ");
                                builder.Append(entity.EventTimeStr);
                                builder.Append("  ");
                                builder.Append("[" + entity.ThreadNo + "]");
                                builder.Append(entity.LogContent);
                                builder.Append("\r\n");
                            }
                        }
                        using (FileStream stream = File.Open(this.FilePath + "//" + DateTime.Now.ToString("yyyyMMdd") + ".log", FileMode.Append))
                        {
                            using (StreamWriter writer = new StreamWriter(stream, Encoding.Default))
                            {
                                writer.Write(builder.ToString());
                            }
                        }
                    }
                    if (num >= this.WriteCountPer)
                    {
                        Thread.Sleep(200);
                    }
                    else
                    {
                        Thread.Sleep(0x3e8);
                        if (this.CheckDeleteDay != DateTime.Now.Day)
                        {
                            this.CheckDeleteDay = DateTime.Now.Day;
                            if (action == null)
                            {
                                action = () => this.DeleteLogProcess();
                            }
                            Task.Factory.StartNew(action);
                        }
                    }
                }
                catch (Exception exception)
                {
                    this.WriteError(exception);
                }
            }
        }

        public void WriteError(Exception ex)
        {
            if (ex != null)
            {
                this.WriteError("", ex);
            }
        }

        public void WriteError(string content, Exception ex = null)
        {
            LogEntity item = new LogEntity {
                EventTime = DateTime.Now,
                LogContent = content + " ex=" + ((ex != null) ? ex.ToString() : ""),
                LogType = LogTypeDefine.Error,
                ThreadNo = Thread.CurrentThread.ManagedThreadId
            };
            this.LogCache.Enqueue(item);
        }

        public void WriteInfo(string content,bool notCheckPrint = false)
        {
            if (CommonConfig.IsPrintLog || notCheckPrint)
            {
                LogEntity item = new LogEntity
                {
                    EventTime = DateTime.Now,
                    LogContent = content,
                    LogType = LogTypeDefine.Info,
                    ThreadNo = Thread.CurrentThread.ManagedThreadId
                };
                this.LogCache.Enqueue(item);
            }
        }

        private string FilePath
        {
            get
            {
                return (AppDomain.CurrentDomain.BaseDirectory + this.RelativePath);
            }
        }
    }
}

