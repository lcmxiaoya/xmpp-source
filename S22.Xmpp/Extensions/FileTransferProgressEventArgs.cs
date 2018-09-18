namespace S22.Xmpp.Extensions
{
    using S22.Xmpp;
    using System;
    using System.Runtime.CompilerServices;

    [Serializable]
    public class FileTransferProgressEventArgs : EventArgs
    {
        internal FileTransferProgressEventArgs(FileTransfer transfer)
        {
            transfer.ThrowIfNull<FileTransfer>("transfer");
            this.Transfer = transfer;
        }

        public FileTransfer Transfer { get; private set; }
    }
}

