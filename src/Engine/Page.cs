/*
 * $Id$
 * $URL$
 * $Rev$
 * $Author$
 * $Date$
 *
 * smuxi - Smart MUltipleXed Irc
 *
 * Copyright (c) 2005 Mirco Bauer <meebey@meebey.net>
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
using System.Collections.Specialized;

namespace Meebey.Smuxi.Engine
{
    public class Page : PermanentRemoteObject
    {
        private string           _Name;
        private PageType         _PageType;
        private NetworkType      _NetworkType;
        private INetworkManager  _NetworkManager;
        private StringCollection _Buffer = new StringCollection(); 
        
        public string Name {
            get {
                return _Name;
            }
        }
        
        public PageType PageType {
            get {
                return _PageType;
            }
        }
        
        public NetworkType NetworkType {
            get {
                return _NetworkType;
            }
        }
        
        public INetworkManager NetworkManager {
            get {
                return _NetworkManager;
            }
        }
        
        public StringCollection Buffer {
            get {
                return _Buffer;
            }
        }
        
        public Page(string name, PageType ptype, NetworkType ntype, INetworkManager nm)
        {
            _Name = name;
            _PageType = ptype;
            _NetworkType = ntype;
            _NetworkManager = nm;
        }
    }
}