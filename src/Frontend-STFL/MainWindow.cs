/*
 * $Id: MainWindow.cs 192 2007-04-22 11:48:12Z meebey $
 * $URL: svn+ssh://svn.qnetp.net/svn/smuxi/smuxi/trunk/src/Frontend-GNOME/MainWindow.cs $
 * $Rev: 192 $
 * $Author: meebey $
 * $Date: 2007-04-22 13:48:12 +0200 (Sun, 22 Apr 2007) $
 *
 * Smuxi - Smart MUltipleXed Irc
 *
 * Copyright (c) 2007 Mirco Bauer <meebey@meebey.net>
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
using System.Reflection;
using Mono.Unix;
using Smuxi.Common;
using Smuxi.Engine;
using Stfl;

namespace Smuxi.Frontend.Stfl
{
    public class MainWindow : Form
    {
#if LOG4NET
        private static readonly log4net.ILog _Logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
#endif
        private StflUI          _UI;
        private Entry           _Entry;
        private ChatViewManager _ChatViewManager;
        
        public ChatViewManager ChatViewManager {
            get {
                return _ChatViewManager;
            }
        }
        
        public StflUI UI {
            get {
                return _UI;
            }
        }

        public string InputLabel {
            get {
                return this["input_label_text"];
            }
            set {
                this["input_label_text"] = String.Format("{0} ", value);
            }
        }

        public string NavigationLabel {
            get {
                return this["navigation_label_text"];
            }
            set {
                this["navigation_label_text"] = value;
            }
        }

        public MainWindow() : base(null, "MainWindow.stfl")
        {
            _ChatViewManager = new ChatViewManager(this);
            _Entry = new Entry(this, _ChatViewManager);
            _UI = new StflUI(_ChatViewManager);
    	    Assembly asm = Assembly.GetExecutingAssembly();
    	    _ChatViewManager.Load(asm);
    	}
    }
}
