//
// VideoFormat.cs
//
// Contact:
//   Moonlight List (moonlight-list@lists.ximian.com)
//
// Copyright 2010 Novell, Inc.
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

namespace System.Windows.Media {
	public class VideoFormat {
		internal VideoFormat ()
		{
		}

		public VideoFormat (PixelFormatType pixelFormat,
				    int height,
				    int width,
				    float framesPerSecond)
		{
			this.framesPerSecond = framesPerSecond;
			this.height = height;
			this.width = width;
			this.stride = width * 4;
			this.pixelFormat = pixelFormat;
		}

		public float FramesPerSecond {
			get { return framesPerSecond; }
		}

		public int PixelHeight {
			get { return height; }
		}

		public int PixelWidth {
			get { return width; }
		}

		// SL4 beta api - required to build drts until we get the SL4 final drts.
		public int Width {
			get { return width; }
		}

		// SL4 beta api - required to build drts until we get the SL4 final drts.
		public int Height {
			get { return height; }
		}

		public int Stride {
			get { return stride; }
		}

		public PixelFormatType PixelFormat {
			get { return pixelFormat; }
		}

		float framesPerSecond;
		int height;
		int width;
		int stride;
		PixelFormatType pixelFormat;
	}
}

