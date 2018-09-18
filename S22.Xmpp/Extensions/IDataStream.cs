namespace S22.Xmpp.Extensions
{
    using System;

    internal interface IDataStream
    {
        event EventHandler<BytesTransferredEventArgs> BytesTransferred;

        event EventHandler<TransferAbortedEventArgs> TransferAborted;

        void CancelTransfer(SISession session);
        void Transfer(SISession session);
    }
}

