namespace S22.Xmpp
{
    using System;
    using System.Runtime.CompilerServices;

    public class LogEntity
    {
        public DateTime EventTime { get; set; }

        public string EventTimeStr
        {
            get
            {
                DateTime eventTime = this.EventTime;
                return eventTime.ToString("yyyy-MM-dd HH:mm:ss fff");
            }
        }

        public string LogContent { get; set; }

        public string LogType { get; set; }

        public int ThreadNo { get; set; }
    }
}

