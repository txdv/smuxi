/*
 * Smuxi - Smart MUltipleXed Irc
 *
 * Copyright (c) 2005-2011 Mirco Bauer <meebey@meebey.net>
 * Copyright (c) 2011 Tuukka Hastrup <Tuukka.Hastrup@iki.fi>
 *
 * Full GPL License: <http://www.gnu.org/licenses/gpl.txt>
 *
 * This program is free software; you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation; either version 2 of the License, or
 * (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with this program; if not, write to the Free Software
 * Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA  02111-1307 USA
 */

using System;
using System.IO;
using System.Net.Security;
using System.Xml;
using System.Text;
using System.Text.RegularExpressions;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;

using jabber;
using jabber.client;
using jabber.connection;
using jabber.protocol.client;
using jabber.protocol.iq;
using XmppMessageType = jabber.protocol.client.MessageType;

using Smuxi.Common;

namespace Smuxi.Engine
{
    [ProtocolManagerInfo(Name = "XMPP", Description = "Extensible Messaging and Presence Protocol", Alias = "jabber")]
    public class JabberProtocolManager : XmppProtocolManager
    {
        public JabberProtocolManager(Session session) : base(session)
        {
        }
    }
    
    [ProtocolManagerInfo(Name = "XMPP", Description = "Extensible Messaging and Presence Protocol", Alias = "xmpp")]
    public class XmppProtocolManager : ProtocolManagerBase
    {
#if LOG4NET
        private static readonly log4net.ILog _Logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
#endif
        private JabberClient    _JabberClient;
        private RosterManager   _RosterManager;
        private ConferenceManager _ConferenceManager;
        private FrontendManager _FrontendManager;
        private ChatModel       _NetworkChat;
        
        public override string NetworkID {
            get {
                return "XMPP";
            }
        }
        
        public override string Protocol {
            get {
                return "XMPP";
            }
        }
        
        public override ChatModel Chat {
            get {
                return _NetworkChat;
            }
        }

        public XmppProtocolManager(Session session) : base(session)
        {
            Trace.Call(session);

            _JabberClient = new JabberClient();
            _JabberClient.Resource = Engine.VersionString;
            _JabberClient.AutoLogin = true;
            _JabberClient.AutoPresence = false;
            _JabberClient.OnMessage += new MessageHandler(_OnMessage);
            _JabberClient.OnConnect += OnConnect;
            _JabberClient.OnDisconnect += OnDisconnect;
            _JabberClient.OnAuthenticate += OnAuthenticate;
            _JabberClient.OnError += OnError;
            _JabberClient.OnProtocol += OnProtocol;
            _JabberClient.OnWriteText += OnWriteText;

            _RosterManager = new RosterManager();
            _RosterManager.Stream = _JabberClient;

            _ConferenceManager = new ConferenceManager();
            _ConferenceManager.Stream = _JabberClient;
            _ConferenceManager.OnJoin += OnJoin;
            _ConferenceManager.OnLeave += OnLeave;
            _ConferenceManager.OnParticipantJoin += OnParticipantJoin;
            _ConferenceManager.OnParticipantLeave += OnParticipantLeave;
        }

        public override void Connect(FrontendManager fm, string host, int port, string username, string password)
        {
            Connect(fm, host, port, username, password, "smuxi");
        }
        
        public void Connect(FrontendManager fm, string host, int port, string username, string password, string resource)
        {
            Trace.Call(fm, host, port, username, "XXX");
            
            _FrontendManager = fm;
            Host = host;
            Port = port;
            
            // TODO: use config for single network chat or once per network manager
            _NetworkChat = new ProtocolChatModel(NetworkID, "Jabber " + host, this);
            Session.AddChat(_NetworkChat);
            Session.SyncChat(_NetworkChat);
            
            // HACK: try to lookup settings via config
            var servers = new ServerListController(Session.UserConfig);
            var serverModel = servers.GetServer(Protocol, host);
            if (serverModel != null) {
                ApplyConfig(Session.UserConfig, serverModel);
            }

            if (username.Contains("@")) {
                var user = username.Split('@')[0];
                var server = username.Split('@')[1];
                _JabberClient.NetworkHost = host;
                _JabberClient.Server = server;
                _JabberClient.User = user;
            } else {
                _JabberClient.Server = host;
                _JabberClient.User = username;
            }
            _JabberClient.Port = port;
            _JabberClient.Password = password;
            _JabberClient.Resource = resource;

            _JabberClient.Connect();
        }
        
        public override void Reconnect(FrontendManager fm)
        {
            Trace.Call(fm);
        }
        
        public override void Disconnect(FrontendManager fm)
        {
            Trace.Call(fm);
        }
        
        public override string ToString()
        {
            string result = "Jabber ";
            if (_JabberClient != null) {
                result += _JabberClient.Server + ":" + _JabberClient.Port;
            }
            
            if (!IsConnected) {
                result += " (" + _("not connected") + ")";
            }
            return result;
        }
        
        public override IList<GroupChatModel> FindGroupChats(GroupChatModel filter)
        {
            Trace.Call(filter);
            
            throw new NotImplementedException();
        }

        public override void OpenChat(FrontendManager fm, ChatModel chat)
        {
            Trace.Call(fm, chat);
            
            throw new NotImplementedException();
        }

        public override void CloseChat(FrontendManager fm, ChatModel chat)
        {
            Trace.Call(fm, chat);
            
            _ConferenceManager.GetRoom(chat.ID+"/"+_JabberClient.User).Leave("Closed");
        }

        public override void SetPresenceStatus(PresenceStatus status,
                                               string message)
        {
            Trace.Call(status, message);

            if (!IsConnected || !_JabberClient.IsAuthenticated) {
                return;
            }

            PresenceType? xmppType = null;
            string xmppShow = null;
            switch (status) {
                case PresenceStatus.Online:
                    xmppType = PresenceType.available;
                    break;
                case PresenceStatus.Away:
                    xmppType = PresenceType.available;
                    xmppShow = "away";
                    break;
                case PresenceStatus.Offline:
                    xmppType = PresenceType.unavailable;
                    break;
            }
            if (xmppType == null) {
                return;
            }

            _JabberClient.Presence(xmppType.Value, message, xmppShow,
                                   _JabberClient.Priority);
        }

        public override bool Command(CommandModel command)
        {
            bool handled = false;
            if (IsConnected) {
                if (command.IsCommand) {
                    switch (command.Command) {
                        case "help":
                            CommandHelp(command);
                            handled = true;
                            break;
                        case "msg":
                        case "query":
                            CommandMessageQuery(command);
                            handled = true;
                            break;
                        case "say":
                            CommandSay(command);
                            handled = true;
                            break;
                        case "join":
                            CommandJoin(command);
                            handled = true;
                            break;
                        case "part":
                        case "leave":
                            CommandPart(command);
                            handled = true;
                            break;
                    }
                } else {
                    _Say(command.Chat, command.Data);
                    handled = true;
                }
            } else {
                if (command.IsCommand) {
                    // commands which work even without beeing connected
                    switch (command.Command) {
                        case "help":
                            CommandHelp(command);
                            handled = true;
                            break;
                        case "connect":
                            CommandConnect(command);
                            handled = true;
                            break;
                    }
                } else {
                    // normal text, without connection
                    NotConnected(command);
                    handled = true;
                }
            }
            
            return handled;
        }

        public void CommandHelp(CommandModel cd)
        {
            MessageModel fmsg = new MessageModel();
            TextMessagePartModel fmsgti;

            fmsgti = new TextMessagePartModel();
            // TRANSLATOR: this line is used as a label / category for a
            // list of commands below
            fmsgti.Text = "[" + _("XMPP Commands") + "]";
            fmsgti.Bold = true;
            fmsg.MessageParts.Add(fmsgti);
            
            Session.AddMessageToChat(cd.Chat, fmsg);
            
            string[] help = {
            "help",
            "connect xmpp/jabber server port username passwort [resource]",
            };
            
            foreach (string line in help) { 
                cd.FrontendManager.AddTextToChat(cd.Chat, "-!- " + line);
            }
        }
        
        public void CommandConnect(CommandModel cd)
        {
            FrontendManager fm = cd.FrontendManager;
            
            string server;
            if (cd.DataArray.Length >= 3) {
                server = cd.DataArray[2];
            } else {
                NotEnoughParameters(cd);
                return;
            }
            
            int port;
            if (cd.DataArray.Length >= 4) {
                try {
                    port = Int32.Parse(cd.DataArray[3]);
                } catch (FormatException) {
                    fm.AddTextToChat(
                        cd.Chat,
                        "-!- " + String.Format(
                                    _("Invalid port: {0}"),
                                    cd.DataArray[3]));
                    return;
                }
            } else {
                NotEnoughParameters(cd);
                return;
            }
            
            string username;                
            if (cd.DataArray.Length >= 5) {
                username = cd.DataArray[4];
            } else {
                NotEnoughParameters(cd);
                return;
            }
            
            string password;
            if (cd.DataArray.Length >= 6) {
                password = cd.DataArray[5];
            } else {
                NotEnoughParameters(cd);
                return;
            }
            
            string resource;
            if (cd.DataArray.Length >= 7) {
                resource = cd.DataArray[6];
            } else {
                resource = "smuxi";
            }
            
            Connect(fm, server, port, username, password, resource);
        }
        
        public void CommandMessageQuery(CommandModel cd)
        {
            ChatModel chat = null;
            if (cd.DataArray.Length >= 2) {
                string nickname = cd.DataArray[1];
                JID jid = null;
                foreach (JID j in _RosterManager) {
                    Item item = _RosterManager[j];
                    if (item.Nickname != null &&
                        item.Nickname.Replace(" ", "_") == nickname) {
                        jid = item.JID;
                        break;
                    }
                }
                if (jid == null) {
                    jid = nickname; // TODO check validity
                }

                chat = GetChat(jid, ChatType.Person);
                if (chat == null) {
                    PersonModel person = new PersonModel(jid, nickname,
                                                         NetworkID, Protocol,
                                                         this);
                    chat = new PersonChatModel(person, jid, nickname, this);
                    Session.AddChat(chat);
                    Session.SyncChat(chat);
                }
            }
            
            if (cd.DataArray.Length >= 3) {
                string message = String.Join(" ", cd.DataArray, 2, cd.DataArray.Length-2);
                // ignore empty messages
                if (message.TrimEnd(' ').Length > 0) {
                    _Say(chat, message);
                }
            }
        }
        
        public void CommandJoin(CommandModel cd)
        {
            string jid = cd.DataArray[1];
            ChatModel chat = GetChat(jid, ChatType.Group);
            if (chat == null) {
                _ConferenceManager.GetRoom(jid+"/"+_JabberClient.User).Join();
            }
        }

        public void CommandPart(CommandModel cd)
        {
            string jid;
            if (cd.DataArray.Length >= 2)
                jid = cd.DataArray[1];
            else
                jid = cd.Chat.ID;
            ChatModel chat = GetChat(jid, ChatType.Group);
            if (chat != null) {
                _ConferenceManager.GetRoom(jid+"/"+_JabberClient.User).Leave("Part");
            }
        }

        public void CommandSay(CommandModel cd)
        {
            _Say(cd.Chat, cd.Parameter);
        }  
        
        private void _Say(ChatModel chat, string text)
        {
            if (!chat.IsEnabled) {
                return;
            }
            
            string target = chat.ID;
            if (chat.ChatType == ChatType.Person) {
                _JabberClient.Message(target, text);
            } else if (chat.ChatType == ChatType.Group) {
                _ConferenceManager.GetRoom(target+"/"+_JabberClient.User).PublicMessage(text);
                return; // don't show now. the message will be echoed back if it's sent successfully
            }
            
            MessageModel msg = new MessageModel();
            TextMessagePartModel msgPart;
            
            msgPart = new TextMessagePartModel();
            msgPart.Text = "<";
            msg.MessageParts.Add(msgPart);
            
            msgPart = new TextMessagePartModel();
            msgPart.Text = _JabberClient.User;
            //msgPart.ForegroundColor = IrcTextColor.Blue;
            msgPart.ForegroundColor = new TextColor(0x0000FF);
            msg.MessageParts.Add(msgPart);
            
            msgPart = new TextMessagePartModel();
            msgPart.Text = "> ";
            msg.MessageParts.Add(msgPart);
                
            msgPart = new TextMessagePartModel();
            msgPart.Text = text;
            msg.MessageParts.Add(msgPart);
            
            this.Session.AddMessageToChat(chat, msg);
        }
        
        void OnProtocol(object sender, XmlElement tag)
        {
            if (!DebugProtocol) {
                return;
            }

            var strWriter = new StringWriter();
            var xmlWriter = new XmlTextWriter(strWriter);
            xmlWriter.Formatting = Formatting.Indented;
            xmlWriter.Indentation = 2;
            xmlWriter.IndentChar =  ' ';
            tag.WriteTo(xmlWriter);

            DebugRead("\n" + strWriter.ToString());
        }

        void OnWriteText(object sender, string text)
        {
            if (!DebugProtocol) {
                return;
            }

            var strWriter = new StringWriter();
            var xmlWriter = new XmlTextWriter(strWriter);
            xmlWriter.Formatting = Formatting.Indented;
            xmlWriter.Indentation = 2;
            xmlWriter.IndentChar =  ' ';

            var document = new XmlDocument();
            document.LoadXml(text);
            document.WriteContentTo(xmlWriter);

            DebugWrite("\n" + strWriter.ToString());
        }

        private void _OnMessage(object sender, Message xmppMsg)
        {
            if (xmppMsg.Body == null) {
                return;
            }

            var delay = xmppMsg["delay"];
            string stamp = null;
            if (delay != null) {
                stamp = delay.Attributes["stamp"].Value;
            }
            bool display = true;

            ChatModel chat = null;
            PersonModel person = null;
            if (xmppMsg.Type != XmppMessageType.groupchat) {
                string jid = xmppMsg.From.Bare;
                var contact = _RosterManager[jid];
                string nickname = jid;
                if (contact != null && contact.Nickname != null) {
                    nickname = contact.Nickname.Replace(" ", "_");
                }
                PersonChatModel personChat = (PersonChatModel) Session.GetChat(jid, ChatType.Person, this);
                if (personChat == null) {
                    person = new PersonModel(jid, nickname, NetworkID,
                                             Protocol, this);
                    personChat = new PersonChatModel(person, jid, nickname, this);
                    Session.AddChat(personChat);
                    Session.SyncChat(personChat);
                } else {
                    person = personChat.Person;
                }
                chat = personChat;
            } else {
                string group_jid = xmppMsg.From.Bare;
                string group_name = group_jid;
                string sender_jid = xmppMsg.From.ToString();
                XmppGroupChatModel groupChat = (XmppGroupChatModel) Session.GetChat(group_jid, ChatType.Group, this);
                if (groupChat == null) {
                    // FIXME shouldn't happen?
                    groupChat = new XmppGroupChatModel(group_jid, group_name, this);
                    Session.AddChat(groupChat);
                    Session.SyncChat(groupChat);
                }
                person = groupChat.GetPerson(xmppMsg.From.Resource);
                if (person == null) {
                    // happens in case of a delayed message if the participant has left meanwhile
                    person = new PersonModel(xmppMsg.From.Resource, xmppMsg.From.Resource, 
                                             NetworkID, Protocol, this);
                }

                // XXX maybe only a Google Talk bug requires this:
                if (stamp != null) {
                    // XXX can't use > because of seconds precision :-(
                    if (stamp.CompareTo(groupChat.LatestSeenStamp) >= 0) {
                        groupChat.LatestSeenStamp = stamp;
                    } else {
                        display = false; // already seen newer delayed message
                    }
                    if (groupChat.SeenNewMessages) {
                        display = false; // already seen newer messages
                    }
                } else {
                    groupChat.SeenNewMessages = true;
                }

                chat = groupChat;
            }

            if (display) {
                var builder = CreateMessageBuilder();
                if (xmppMsg.Type != XmppMessageType.error) {
                    builder.AppendMessage(person, xmppMsg.Body);
                } else {
                    // TODO: nicer formatting
                    builder.AppendMessage(xmppMsg.Error.ToString());
                }
                var msg = builder.ToMessage();
                if (stamp != null) {
                    string format = DateTimeFormatInfo.InvariantInfo.UniversalSortableDateTimePattern.Replace(" ", "T");
                    msg.TimeStamp = DateTime.ParseExact(stamp, format, null);
                }
                Session.AddMessageToChat(chat, msg);
            }
        }

        void OnJoin(Room room)
        {
            AddPersonToGroup(room, room.Nickname);
        }

        void OnLeave(Room room, Presence presence)
        {
            var chat = Session.GetChat(room.JID.Bare, ChatType.Group, this);
            if (chat.IsEnabled)
                Session.RemoveChat(chat);
        }

        void OnParticipantJoin(Room room, RoomParticipant roomParticipant)
        {
            AddPersonToGroup(room, roomParticipant.Nick);
        }

        private void AddPersonToGroup(Room room, string nickname)
        {
            string jid = room.JID.Bare;
            var chat = (GroupChatModel) Session.GetChat(jid, ChatType.Group, this);
            // first notice we're joining a group chat is the participant info:
            if (chat == null) {
                chat = new XmppGroupChatModel(jid, jid, this);
                Session.AddChat(chat);
                Session.SyncChat(chat);
            }

            PersonModel person;
            lock(chat.UnsafePersons) {
                person = chat.GetPerson(nickname);
                if (person != null) {
                    return;
                }

                person = new PersonModel(nickname, nickname,
                                         NetworkID, Protocol, this);
                chat.UnsafePersons.Add(nickname, person);
                Session.AddPersonToGroupChat(chat, person);
            }
        }
        
        public void OnParticipantLeave(Room room, RoomParticipant roomParticipant)
        {
            string jid = room.JID.Bare;
            var chat = (GroupChatModel) Session.GetChat(jid, ChatType.Group, this);
            string nickname = roomParticipant.Nick;

            PersonModel person;
            lock(chat.UnsafePersons) {
                person = chat.GetPerson(nickname);
                if (person == null) {
                    return;
                }

                chat.UnsafePersons.Remove(nickname);
                Session.RemovePersonFromGroupChat(chat, person);
            }
        }

        void OnConnect(object sender, StanzaStream stream)
        {
            Trace.Call(sender, stream);
        }

        void OnDisconnect(object sender)
        {
            Trace.Call(sender);

            IsConnected = false;
            OnDisconnected(EventArgs.Empty);
        }

        void OnError(object sender, Exception ex)
        {
            Trace.Call(sender);

            Session.AddTextToChat(_NetworkChat, "Error: " + ex);
        }

        void OnAuthenticate(object sender)
        {
            Trace.Call(sender);

            IsConnected = true;

            Session.AddTextToChat(_NetworkChat, "Authenticated");

            // send initial presence
            SetPresenceStatus(PresenceStatus.Online, null);

            OnConnected(EventArgs.Empty);
        }

        private void ApplyConfig(UserConfig config, ServerModel server)
        {
            _JabberClient.OnInvalidCertificate -= ValidateCertificate;

            _JabberClient.AutoStartTLS = server.UseEncryption;
            if (!server.ValidateServerCertificate) {
                _JabberClient.OnInvalidCertificate += ValidateCertificate;
            }
        }

        private static bool ValidateCertificate(object sender,
                                         X509Certificate certificate,
                                         X509Chain chain,
                                         SslPolicyErrors sslPolicyErrors)
        {
            return true;
        }

        private static string _(string msg)
        {
            return Mono.Unix.Catalog.GetString(msg);
        }
    }
}
