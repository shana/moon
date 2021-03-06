/*
 * SystemParameters.cs.
 *
 * Contact:
 *   Moonlight List (moonlight-list@lists.ximian.com)
 *
 * Copyright 2008 Novell, Inc. (http://www.novell.com)
 *
 * See the LICENSE file included with the distribution for details.
 * 
 */

using System;

namespace System.Windows
{
	public static class SystemParameters
	{
		[MonoTODO]
		public static bool HighContrast {
			get { return false; }
		}

		public static int WheelScrollLines {
			get {
				Console.WriteLine ("System.Windows.SystemParameters.get_WheelScrollLines: NIEX");
				throw new NotImplementedException ();
			}
		}
	}
}
