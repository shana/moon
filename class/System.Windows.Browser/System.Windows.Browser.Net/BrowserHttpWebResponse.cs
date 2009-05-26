//
// System.Windows.Browser.Net.BrowserHttpWebResponse class
//
// Contact:
//   Moonlight List (moonlight-list@lists.ximian.com)
//
// Copyright (C) 2007 Novell, Inc (http://www.novell.com)
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

#if NET_2_1

using System;
using System.Globalization;
using System.IO;
using System.Net;
using System.Runtime.InteropServices;

using Mono;

namespace System.Windows.Browser.Net
{
	class BrowserHttpWebResponse : HttpWebResponse
	{
		BrowserHttpWebRequest request;
		IntPtr native;
		Stream response;
		bool aborted;

		int real_status_code;
		string status_desc;

		public BrowserHttpWebResponse (BrowserHttpWebRequest request, IntPtr native)
		{
			this.request = request;
			this.native = native;
			this.response = new MemoryStream ();
			this.aborted = false;
			Headers = new WebHeaderCollection ();

			if (native == IntPtr.Zero)
				return;

			NativeMethods.downloader_response_set_header_visitor (native, OnHttpHeader);
		}

		~BrowserHttpWebResponse ()
		{
			if (native == IntPtr.Zero)
				return;
			
			/* FIXME: Firefox will be releasing this object automatically but the managed side
			 * really doesn't know when this will happen
			 * Abort ();
			 *
			 * NativeMethods.downloader_response_free (native);
			 */
		}

		public void Abort ()
		{
			if (native == IntPtr.Zero)
				return;

			if (request != null)
				request.InternalAbort ();
			
			InternalAbort ();
		}

		internal void InternalAbort () {
			if (aborted)
				return;

			/* FIXME: Firefox will be releasing this object automatically but the managed side
			 * really doesn't know when this will happen
			 * NativeMethods.downloader_response_abort (native);
			 */
			aborted = true;
		}

		void OnHttpHeader (IntPtr name, IntPtr value)
		{
			try {
				Headers [Marshal.PtrToStringAnsi (name)] = Marshal.PtrToStringAnsi (value);
			} catch {}
		}

		public override void Close ()
		{
			response.Dispose ();
		}

		internal void Write (IntPtr buffer, int count)
		{
			byte[] data = new byte [count];
			Marshal.Copy (buffer, data, 0, count);
			response.Write (data, 0, count);
		}

		public override Stream GetResponseStream ()
		{
			response.Seek (0, SeekOrigin.Begin);
			// the stream we return must be read-only, so we wrap arround our MemoryStream
			BrowserHttpWebStreamWrapper stream = new BrowserHttpWebStreamWrapper (response);
			stream.SetReadOnly ();
			return stream;
		}

		public override long ContentLength {
			get {
				return long.Parse (Headers ["Content-Length"]);
			}
		}

		public override string ContentType {
			get { return Headers [HttpRequestHeader.ContentType]; }
		}

		public override string Method {
			get { return request.Method; }
		}

		// FIXME this is different from the original request if we followed any redirection
		public override Uri ResponseUri {
			get { return request.RequestUri; }
		}

		internal void GetStatus ()
		{
			if (real_status_code != 0)
				return;

			if (native == IntPtr.Zero)
				return;

			real_status_code = NativeMethods.downloader_response_get_response_status (native);

			// Silverlight only returns OK or NotFound - but we keep the real value for ourselves
			switch (real_status_code) {
			case 200:
			case 404:
				status_desc = NativeMethods.downloader_response_get_response_status_text (native);
				break;
			default:
				status_desc = "Requested resource was not found";
				break;
			}
		}

		// since Silverlight hides most of this data, we keep it available for the BCL
		internal int RealStatusCode {
			get {
				GetStatus ();
				return real_status_code;
			}
		}

		public override HttpStatusCode StatusCode {
			get {
				return (RealStatusCode == 200) ? HttpStatusCode.OK : HttpStatusCode.NotFound;
			}
		}

		public override string StatusDescription {
			get {
				GetStatus ();
				return status_desc;
			}
		}
	}
}

#endif
