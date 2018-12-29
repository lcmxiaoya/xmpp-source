namespace S22.Xmpp
{
    using System;

    public class CommonConfig
    {
        private static int fileTranType;
        public static bool IsPrintLog = true;
        public static LogManage Logger = new LogManage("XmppLog_组件", 10);
        public static string LogPath;
        private static string tempFilePath;

        public static int FileTranType
        {
            get
            {
                return fileTranType;
            }
            set
            {
                fileTranType = value;
            }
        }

        public static string TempFilePath
        {
            get
            {
                return tempFilePath;
            }
            set
            {
                tempFilePath = value;
            }
        }
    }
}

