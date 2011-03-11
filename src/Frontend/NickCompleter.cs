// Smuxi - Smart MUltipleXed Irc
// 
// Copyright (c) 2011 Andrius Bentkus
// 
// Full GPL License: <http://www.gnu.org/licenses/gpl.txt>
// 
// This program is free software; you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation; either version 2 of the License, or
// (at your option) any later version.
// 
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with this program; if not, write to the Free Software
// Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA  02111-1307 USA

using System;
using System.Collections.Generic;
using Smuxi.Common;
using Smuxi.Engine;

namespace Smuxi.Frontend
{
    public class NickCompleter
    {
#if LOG4NET
        private static readonly log4net.ILog _Logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
#endif

        public string Text     { get; set; }
        public int    Position { get; set; }

        protected bool NickPrefix { get; set; }
        protected bool FirstWord  { get; set; }

        protected string Prefix  { get; set; }
        protected string Infix   { get; set; }
        protected string Postfix { get; set; }

        protected int CurrentResult { get; set; }
        protected UserConfig UserConfig { get; set; }
        protected IList<string> Results { get; set; }
        protected IList<string> SortedResults { get; set; }
        protected IChatView ChatView { get; set; }


        protected string CompletionChar {
            get {
                if (NickPrefix) {
                    return string.Empty;
                } else {
                    return (string)UserConfig["Interface/Entry/CompletionCharacter"];
                }
            }
        }

        protected IList<string> FilterNicks(IList<string> results)
        {
            List<string> newresults = new List<string>();

            foreach (string nick in results) {
                if (nick.Length == 1) {
                    continue;
                }
                newresults.Add(nick);
            }

            return newresults;
        }

        protected IList<string> PrioritySortedNicks(GroupChatModel gcm, IList<string> nicks)
        {
            List<string> firstPriority  = new List<string>();
            List<string> secondPriority = new List<string>();

            foreach (var msg in gcm.Messages) {
                if (msg.MessageType != MessageType.Normal) {
                    continue;
                }

                string nick = msg.MessageParts[1].ToString();

                if (!nicks.Contains(nick)) {
                    continue;
                }

                if (!msg.HasHighlight) {
                    if (!secondPriority.Contains(nick)) {
                        secondPriority.Add(nick);
                    }
                }

                if (!firstPriority.Contains(nick)) {
                    firstPriority.Add(nick);
                }
            }

            firstPriority.AddRange(secondPriority);

            foreach (string tail in nicks) {
                if (!firstPriority.Contains(tail)) {
                    firstPriority.Add(tail);
                }
            }

            return firstPriority;
        }

        public NickCompleter(IChatView chatView, UserConfig userConfig, string text, int position)
        {
            ChatView = chatView;
            UserConfig = userConfig;
            Text = text;
            Position = position;
        }

        public bool Complete()
        {
            ChatModel chat = ChatView.ChatModel;
            string text = Text;
            int position = Position;

            // return if we don't support the current ChatView
            if (!(chat is GroupChatModel) &&
                !(chat is PersonChatModel)) {
                return false;
            }

            // we have no word to read
            if (position == 0) {
                return false;
            }

            // we are pointing at space, do nothing!
            if (text[position - 1] == ' ') {
                return false;
            }


            int start;
            Infix = CurrentWord(text, position, out start);

            FirstWord = (start == 0);

            if (Infix.StartsWith("@")) {
                NickPrefix = true;
                Infix = Infix.Substring(1);
            }

            Prefix = text.Substring(0, start);
            Postfix = text.Substring(position);
            CurrentResult = 0;

#if LOG4NET
            _Logger.Debug("Completion word: " + Infix);
#endif

            if (chat is GroupChatModel) {

                GroupChatModel cp = chat as GroupChatModel;

                Results = FilterNicks(cp.PersonLookupAll(Infix));
                SortedResults = PrioritySortedNicks(cp, Results);
#if LOG4NET
                foreach (var res in Results) {
                    _Logger.Debug("Completion results: " + res);
                }

                return true;
#endif
            } else {
                PersonModel person = (chat as PersonChatModel).Person;

                if (person.IdentityName.StartsWith(Infix, StringComparison.InvariantCultureIgnoreCase)) {
                    Results = new string[] { person.IdentityName };
                    return true;
                } else {
                    return false;
                }
            }
        }

        private string CurrentWord(string word, int position, out int start)
        {
            int end = position;
            for (start = end; start > 0; start--) {
                if (start >= word.Length) {
                    continue;
                } else if (word[start] == ' ') {
                    start++;
                    break;
                }
            }
            return word.Substring(start, end - start);
        }

        public string Next(out int newPosition)
        {
            newPosition = 0;
            if (Results == null) {
                return null;
            }

            if ((bool)UserConfig["Interface/Entry/BashStyleCompletion"]) {
                if ((Results == null) && (Results.Count == 0)) {
                    return null;
                } else if (Results.Count == 1) {
                    return null;
                }

                string[] nickArray = new string[Results.Count];
                Results.CopyTo(nickArray, 0);

                string nicks = String.Join(" ", nickArray);

                ChatView.AddMessage(new MessageModel(String.Format("-!- {0}", nicks)));

                return null;
            } else {

                string ret = Prefix + (NickPrefix ? "@" : "");
                if (CurrentResult >= SortedResults.Count) {
                    ret += Infix;
                    CurrentResult = 0;
                } else {
                    ret += SortedResults[CurrentResult] + (FirstWord ? CompletionChar : string.Empty) + " ";
                    CurrentResult++;
                }
    
                newPosition = ret.Length;
                ret += Postfix;
    
                return ret;
            }
        }
    }
}
