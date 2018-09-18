namespace S22.Xmpp.Extensions
{
    using S22.Xmpp;
    using S22.Xmpp.Im;
    using System;
    using System.Collections.Generic;
    using System.Threading;

    internal class ChatStateNotifications : XmppExtension, IInputFilter<Message>
    {
        public event EventHandler<ChatStateChangedEventArgs> ChatStateChanged;

        public ChatStateNotifications(XmppIm im) : base(im)
        {
        }

        public bool Input(Message stanza)
        {
            foreach (ChatState state in Enum.GetValues(typeof(ChatState)))
            {
                string str = state.ToString().ToLowerInvariant();
                if ((stanza.Data[str] != null) && (stanza.Data[str].NamespaceURI == "http://jabber.org/protocol/chatstates"))
                {
                    this.ChatStateChanged.Raise<ChatStateChangedEventArgs>(this, new ChatStateChangedEventArgs(stanza.From, state));
                }
            }
            return false;
        }

        public void SetChatState(Jid jid, ChatState state)
        {
            jid.ThrowIfNull("jid");
            Message m = new Message(jid);
            m.Type = MessageType.Chat;
            m.Data.Child(Xml.Element(state.ToString().ToLowerInvariant(),
                "http://jabber.org/protocol/chatstates"));
            im.SendMessage(m);
        }

        public override IEnumerable<string> Namespaces
        {
            get
            {
                return new string[] { "http://jabber.org/protocol/chatstates" };
            }
        }

        public override Extension Xep
        {
            get
            {
                return Extension.ChatStateNotifications;
            }
        }
    }
}

