// Contact:
//   Moonlight List (moonlight-list@lists.ximian.com)
//
// Copyright 2007 Novell, Inc.
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;
using System.Threading;
using System.Windows;
using System.Windows.Media;
using System.Windows.Input;
using System.Runtime.InteropServices;
using Mono;

namespace System.Windows.Input
{
	public struct StylusPoint
	{
		float presssureFactor;
		public StylusPoint (double x, double y) : this ()
		{
			X = x;
			Y = y;
			PressureFactor = 0.5f;
		}

		public float PressureFactor {
			get { return presssureFactor; }
			set {
				if (value < 0 || value > 1)
					throw new ArgumentOutOfRangeException ("value");
				presssureFactor = value;
			}
		}

		public double X {
			get; set;
		}

		public double Y {
			get; set;
		}

		public override string ToString ()
		{
			return string.Format("[StylusPoint: PressureFactor={0}, X={1}, Y={2}]", PressureFactor, X, Y);
		}
	}
}
