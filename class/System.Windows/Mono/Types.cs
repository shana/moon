//
// Types.cs
//
// Contact:
//   Moonlight List (moonlight-list@lists.ximian.com)
//
// Copyright 2008 Novell, Inc.
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
//

using Mono;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Mono
{	
	/*
	 *  The managed equivalent of the unmanaged Types
	 *  TODO:
	 *  - Find out when to call Free.
	 */
	internal partial class Types
	{
		private IntPtr native;
		private Dictionary<Type,ManagedType> types = new Dictionary<Type,ManagedType> ();
		private object sync_object = new object ();
		
		public Types(IntPtr raw)
		{
			native = raw;
			CreateNativeTypes ();
#if SANITY
			foreach (Kind k in Enum.GetValues (typeof (Kind))) {
				Type t = KindToType (k);
				if (t == null) {
					Console.Error.WriteLine ("Kind {0} missing from Types.g.cs", k);
				}
				else {
					Kind k2 = TypeToKind (t);
					if (k2 == Kind.INVALID)
						Console.Error.WriteLine ("Type {0} missing from Types.g.cs", t);
				}
			}
#endif
		}
		
#if notyet
		private void Free ()
		{
			// TODO: How do we free the per-domain types? There's no static dtor...
			foreach (ManagedType ti in types.Values) {
				ti.gc_handle.Free ();
			}
			native = IntPtr.Zero;
		}
#endif
		
		public IntPtr Native {
			get { return native; }
		}
		
		public ManagedType Find (Type type)
		{
			ManagedType info;
			ManagedType parent;
			
			if (types.TryGetValue (type, out info))
				return info;
			
			if (type.BaseType == null || type.BaseType == typeof (object)) {
				parent = null;
			} else {
				parent = Find (type.BaseType);
			}
			
			return RegisterType (type, parent);
		}
		

		private ManagedType RegisterType (Type type, ManagedType parent)
		{
			ManagedType info;
		
			Console.WriteLine ("Types.RegisterManagedType ({0}, {1})", type == null ? "null" : type.FullName, parent == null ? "null" : parent.type.FullName);
			
			lock (sync_object) {
				info = new ManagedType ();
				info.type = type;
				info.gc_handle = GCHandle.Alloc (type);
				info.parent = parent;

				if (type.IsEnum && Enum.GetUnderlyingType (type) == typeof(int))
					info.native_handle = Kind.INT32;
				else
					info.native_handle = NativeMethods.types_register_type (native, type.FullName, GCHandle.ToIntPtr (info.gc_handle), (parent != null ? parent.native_handle : Kind.INVALID));
				
				types.Add (type, info);
			}
			
			return info;
		}
		
		public Type KindToType (Kind kind)
		{
			foreach (ManagedType type in types.Values) {
				if (type.native_handle == kind)
					return type.type;
			}
			return null;
		}
		
		public Kind TypeToKind (Type type)
		{
			ManagedType mt;
			
			if (!types.TryGetValue (type, out mt))
				return Kind.INVALID;
			
			return (Kind) mt.native_handle;
		}

		public Kind TypeToNativeKind (Type type)
		{
			while (type != typeof (object)) {
				Kind kind = TypeToKind (type);
				if (kind != Kind.INVALID && Enum.IsDefined (typeof (Kind), kind))
					return kind;
				type = type.BaseType;
			}
			return Kind.INVALID;
		}
	}
}
