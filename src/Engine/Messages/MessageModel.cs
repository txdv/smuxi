/*
 * $Id: Config.cs 100 2005-08-07 14:54:22Z meebey $
 * $URL: svn+ssh://svn.qnetp.net/svn/smuxi/smuxi/trunk/src/Engine/Config.cs $
 * $Rev: 100 $
 * $Author: meebey $
 * $Date: 2005-08-07 16:54:22 +0200 (Sun, 07 Aug 2005) $
 *
 * smuxi - Smart MUltipleXed Irc
 *
 * Copyright (c) 2005-2006 Mirco Bauer <meebey@meebey.net>
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
using System.Collections.Generic;
using System.Runtime.Serialization;
using Smuxi.Common;

namespace Smuxi.Engine
{
    [Serializable]
    public class MessageModel : ISerializable
    {
        private DateTime                _TimeStamp;
        private IList<MessagePartModel> _MessageParts;

        public DateTime TimeStamp {
            get {
                return _TimeStamp;
            }
        }
        
        public IList<MessagePartModel> MessageParts {
            get {
                return _MessageParts;
            }
        }
        
        public MessageModel()
        {
            _TimeStamp    = DateTime.UtcNow;
            _MessageParts = new List<MessagePartModel>();
        }
        
        public MessageModel(string text) : this()
        {
            _MessageParts.Add(new TextMessagePartModel(null, null, false, false, false, text));
        }

        protected MessageModel(SerializationInfo info, StreamingContext ctx)
        {
            SerializationReader sr = SerializationReader.GetReader(info);
            SetObjectData(sr);
        }
        
        protected virtual void SetObjectData(SerializationReader sr)
        {
            _TimeStamp    = sr.ReadDateTime();
            _MessageParts = sr.ReadList<MessagePartModel>();
        }
        
        protected virtual void GetObjectData(SerializationWriter sw)
        {
            sw.Write(_TimeStamp);
            //sw.Write((ICollection<MessagePartModel>) _MessageParts);
            sw.Write(_MessageParts);
        }
        
        public virtual void GetObjectData(SerializationInfo info, StreamingContext ctx) 
        {
            SerializationWriter sw = SerializationWriter.GetWriter(); 
            GetObjectData(sw);
            sw.AddToInfo(info);
        }
    }
}