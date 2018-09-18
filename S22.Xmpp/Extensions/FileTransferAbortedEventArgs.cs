namespace S22.Xmpp.Extensions
{
    using S22.Xmpp;
    using System;
    using System.Runtime.CompilerServices;

    [Serializable]
    public class FileTransferAbortedEventArgs : EventArgs
    {
        internal FileTransferAbortedEventArgs(FileTransfer transfer)
        {
            transfer.ThrowIfNull<FileTransfer>("transfer");
            this.Transfer = transfer;
        }

        public FileTransfer Transfer { get; private set; }
    }
}

