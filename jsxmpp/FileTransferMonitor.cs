namespace jsxmpp
{
    using S22.Xmpp.Extensions;
    using System;

    public class FileTransferMonitor
    {
        public virtual void notifyProgress(int percent)
        {
        }

        public virtual void onCancelled()
        {
        }

        public virtual void onError()
        {
        }

        public virtual void onFinished()
        {
        }

        public virtual void onRefused()
        {
        }

        public virtual string onRequest(FileTransfer transfer)
        {
            return transfer.Name;
        }

        public virtual void onTimeout()
        {
        }
    }
}

