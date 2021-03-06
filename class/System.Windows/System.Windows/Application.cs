//
// Application.cs
//
// Contact:
//   Moonlight List (moonlight-list@lists.ximian.com)
//
// Copyright 2008, 2010 Novell, Inc.
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

using Mono;
using Mono.Xaml;
using System;
using System.IO;
using System.IO.IsolatedStorage;
using System.Linq;
using System.Reflection;
using System.Security;
using System.Windows.Controls;
using System.Windows.Resources;
using System.Windows.Interop;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Resources;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Markup;
using System.ComponentModel;

namespace System.Windows {

	public partial class Application : INativeDependencyObjectWrapper {
		//
		// Application instance fields
		//
		Mono.EventHandlerList event_list;
		UIElement root_visual;
		SilverlightHost host;

		GetDefaultStyleCallback get_default_style;
		ConvertSetterValuesCallback convert_setter_values;
		ConvertKeyframeValueCallback convert_keyframe_value;
		GetResourceCallback get_resource;
		ApplicationLifetimeObjectsCollection lifetime_objects;

		bool free_mapping;

		static Application ()
		{
			ReinitializeStaticData ();
		}

		internal Application (IntPtr raw, bool dropref)
		{
			NativeHandle = raw;
			if (dropref)
				NativeMethods.event_object_unref (raw);

			get_default_style = new GetDefaultStyleCallback (get_default_style_cb_safe);
			convert_setter_values = new ConvertSetterValuesCallback (convert_setter_values_cb_safe);
			convert_keyframe_value = new ConvertKeyframeValueCallback (convert_keyframe_value_cb_safe);
			get_resource = new GetResourceCallback (get_resource_cb_safe);

			NativeMethods.application_register_callbacks (NativeHandle, get_default_style, convert_setter_values, get_resource, convert_keyframe_value);

			if (Current == null) {
				Current = this;

				SynchronizationContext context = new System.Windows.Threading.DispatcherSynchronizationContext ();
				SynchronizationContext.SetSynchronizationContext (context);
			} else {
				root_visual = Current.root_visual;
			}

			lifetime_objects = new ApplicationLifetimeObjectsCollection ();

			var handler = UIANewApplication;
			if (handler != null)
				handler (this, EventArgs.Empty);
		}

		public Application () : this (SafeNativeMethods.application_new (), true)
		{
		}

		internal void Terminate ()
		{
			if (Deployment.Current.XapDir == null)
				return;

			for (int i = 0; i < ApplicationLifetimeObjects.Count; i++) {
				IApplicationLifetimeAware asvc = ApplicationLifetimeObjects[i] as IApplicationLifetimeAware;
				if (asvc != null)
					asvc.Exiting();
			}
			
			if (Exit != null)
				Exit (this, EventArgs.Empty);

			for (int i = 0; i < ApplicationLifetimeObjects.Count; i++) {
				IApplicationLifetimeAware asvc = ApplicationLifetimeObjects[i] as IApplicationLifetimeAware;
				if (asvc != null)
					asvc.Exited();
			}

			// note: this loop goes in reverse order
			for (int i = ApplicationLifetimeObjects.Count - 1; i >= 0; i--) {
				IApplicationService svc = ApplicationLifetimeObjects[i] as IApplicationService;
				if (svc != null)
					svc.StopService ();
			}
			
			try {
				Directory.Delete (Deployment.Current.XapDir, true);
				Deployment.Current.XapDir = null;
			} catch {
			}

			foreach (KeyValuePair<Assembly, ResourceDictionary> kv in assemblyToGenericXaml)
				kv.Value.Clear ();
			assemblyToGenericXaml.Clear ();
			root_visual = null;
			Application.Current = null;

			ReinitializeStaticData ();
		}

		~Application ()
		{
			Free ();
		}

		internal void Free ()
		{
			if (free_mapping) {
				free_mapping = false;
				NativeDependencyObjectHelper.FreeNativeMapping (this);
			}
		}
		
		internal Mono.EventHandlerList EventList {
			get {
				if (event_list == null)
					event_list = new Mono.EventHandlerList ();
				return event_list;
			}
		}
		
		internal void RegisterEvent (int eventId, Delegate managedHandler, UnmanagedEventHandler nativeHandler)
		{
			if (managedHandler == null)
				return;
			
			int token = Events.AddHandler (this, eventId, nativeHandler);
			
			EventList.AddHandler (eventId, token, managedHandler, nativeHandler);
		}

		internal void UnregisterEvent (int eventId, Delegate managedHandler)
		{
			UnmanagedEventHandler nativeHandler = EventList.RemoveHandler (eventId, managedHandler);
			
			if (nativeHandler == null)
				return;
			
			Events.RemoveHandler (this, eventId, nativeHandler);
		}
		
		static void ReinitializeStaticData ()
		{
			// reinitialize these static lists so we don't inherit things from the previous application
			xmlns_definitions = new Dictionary<XmlnsDefinitionAttribute, Assembly> ();

			imported_namespaces = new List<string> ();

			ImportXamlNamespace ("clr-namespace:System.Windows;assembly:System.Windows.dll");
			ImportXamlNamespace ("clr-namespace:System.Windows.Controls;assembly:System.Windows.dll");
		}				

		public bool IsRunningOutOfBrowser {
			get {
				return NativeMethods.application_is_running_out_of_browser (NativeHandle);
			}
		}

		public InstallState InstallState {
			get {
				return NativeMethods.application_get_install_state (NativeHandle);
			}
		}

		public bool Install ()
		{
			return Install (false);
		}

		bool Install (bool unattended)
		{
			// note: user-initiated check is done in unmanaged code
			if (!NativeMethods.application_install (NativeHandle, unattended))
				return false;

			// once installed the default quota for isolated storage is augmented to 25MB (instead of 1MB)
			// note: this applies only to the browser (not desktop) assemblies (it won't compile otherwise)
#if !NET_3_0
			const long OutOfBrowserQuota = 25 * IsolatedStorage.DefaultQuota;
			if (IsolatedStorage.Quota < OutOfBrowserQuota)
				IsolatedStorage.Quota = OutOfBrowserQuota;
#endif
			return true;
		}

		public IList ApplicationLifetimeObjects {
			get { return lifetime_objects; }
		}


		Dictionary<Assembly, ResourceDictionary> assemblyToGenericXaml = new Dictionary<Assembly, ResourceDictionary>();

		void convert_keyframe_value_cb_safe (Kind kind, IntPtr property_ptr, IntPtr original, out Value converted)
		{
			try {
				convert_keyframe_value_cb (kind, property_ptr, original, out converted);
			} catch (Exception ex) {
				converted = default (Value);
				try {
					Console.WriteLine ("Moonlight: Unhandled exception in apply_object_key_frame_cb_safe: {0}", ex);
				} catch {
				}
			}
		}
		
		void convert_keyframe_value_cb (Kind kind, IntPtr property_ptr, IntPtr original, out Value converted)
		{
			Type type = Deployment.Current.Types.KindToType (kind);
			if (type != null)
				Types.Ensure (type);
			
			DependencyProperty property = DependencyProperty.Lookup (property_ptr);
			if (property == null) {
				Console.WriteLine ("Moonlight Error: Property couldn't be looked up");
				converted = Value.Empty;
				return;
			}
			
			object o = Value.ToObject (null, original);
			if (o == null) {
				Console.WriteLine ("Moonlight Error: Object was null");
				converted = Value.Empty;
				return;
			}
			
			o = MoonlightTypeConverter.ConvertObject (property, o, null, true);
			
			if (o == null) {
				Console.WriteLine ("Moonlight Error: Converted to null");
				converted = Value.Empty;
				return;
			}

			// This is freed in native code
			converted = Value.FromObject (o);
		}
		
		IntPtr get_default_style_cb_safe (IntPtr type_info_ptr)
		{
			try {
				return get_default_style_cb (type_info_ptr);
			} catch (Exception ex) {
				try {
					Console.WriteLine ("Moonlight: Unhandled exception in Application.get_default_style_cb_safe: {0}", ex);
				} catch {
				}
			}
			return IntPtr.Zero;
		}

		IntPtr get_default_style_cb (IntPtr type_info_ptr)
		{
			ManagedTypeInfo type_info = (ManagedTypeInfo)Marshal.PtrToStructure (type_info_ptr, typeof (ManagedTypeInfo));
			Type type = null;

			type = Deployment.Current.Types.KindToType (type_info.Kind);

			if (type == null) {
				Console.Error.WriteLine ("failed to lookup native kind {0}", type_info.Kind);
				return IntPtr.Zero;
			}

			Style s = GetGenericXamlStyleFor (type);
			return s == null ? IntPtr.Zero : s.native;
		}

		void convert_setter_values_cb_safe (IntPtr style_ptr)
		{
			try {
				convert_setter_values_cb (style_ptr);
			} catch (Exception ex) {
				try {
					Console.WriteLine ("Moonlight: Unhandled exception in Application.convert_setter_values_cb: {0}", ex);
				} catch {
				}
			}
		}
		
		void convert_setter_values_cb (IntPtr style_ptr)
		{
			Style style = NativeDependencyObjectHelper.FromIntPtr(style_ptr) as Style;
			style.ConvertSetterValues ();
		}

		public void CheckAndDownloadUpdateAsync ()
		{
			NativeMethods.application_check_and_download_update_async (NativeHandle);
		}

		internal Style GetGenericXamlStyleFor (Type type)
		{
			ResourceDictionary rd = null;

			if (assemblyToGenericXaml.ContainsKey (type.Assembly)) {
				rd = assemblyToGenericXaml[type.Assembly];
			}
			else {
				Console.WriteLine ("trying to load: /{0};component/themes/generic.xaml",
						   type.Assembly.GetName().Name);

				StreamResourceInfo info = null;

				try {
					info = GetResourceStream (new Uri (string.Format ("/{0};component/themes/generic.xaml",
											  type.Assembly.GetName().Name), UriKind.Relative));
				}
				catch {
					Console.WriteLine ("no generic.xaml for assembly {0}", type.Assembly.GetName().Name);
				}
				
				if (info != null) {
					using (StreamReader sr = new StreamReader (info.Stream)) {
						string generic_xaml = sr.ReadToEnd();
						string resource_base = NativeMethods.dependency_object_get_resource_base (NativeHandle);
						ManagedXamlLoader loader = new ManagedXamlLoader (type.Assembly, resource_base, Deployment.Current.Surface.Native, PluginHost.Handle);

						try {
							rd = loader.CreateObjectFromString (generic_xaml, false) as ResourceDictionary;
						}
						catch (Exception e) {
							Console.WriteLine ("failed generic.xaml parsing:");
							Console.WriteLine (e);
						}
					}
				}

				if (rd == null)
					// create an empty one so we don't fall into this block again for this assembly
					rd = new ResourceDictionary();

				assemblyToGenericXaml[type.Assembly] = rd;
			}

			return rd[type.FullName] as Style;
		}

		public static void LoadComponent (object component, Uri resourceLocator)
		{			
			if (component == null)
				throw new ArgumentNullException ("component");

			if (resourceLocator == null)
				throw new ArgumentNullException ("resourceLocator");

			StreamResourceInfo sr = GetResourceStream (resourceLocator);

			// Does not seem to throw.
			if (sr == null)
				return;

			using (var v = Value.FromObject (component)) {
				// XXX still needed for the app.surface reference when creating the ManagedXamlLoader
				// Application app = component as Application;
	
				string xaml = new StreamReader (sr.Stream).ReadToEnd ();
				Assembly loading_asm = component.GetType ().Assembly;
	
				ManagedXamlLoader loader = new ManagedXamlLoader (loading_asm, resourceLocator.ToString(), Deployment.Current.Surface.Native, PluginHost.Handle);
				loader.Hydrate (v, xaml);
			}
		}

		private static Dictionary<string,byte[]> local_xap_resources = new Dictionary<string,byte[]> ();

		unsafe static StreamResourceInfo GetXapResource (string resource)
		{
			try {
				string canon = Helper.CanonicalizeResourceName (resource);
				string res_file = Path.GetFullPath (Path.Combine (Deployment.Current.XapDir, canon));
				// ensure the file path is rooted against the XAP directory and that it exists
				if (!res_file.StartsWith (Deployment.Current.XapDir) || !File.Exists (res_file))
					return null;

				byte[] data = null;
				// we don't want to run out of file handles (see bug #535709) so we cache the data in memory
				if (!local_xap_resources.TryGetValue (res_file, out data)) {
					data = File.ReadAllBytes (res_file);
					local_xap_resources.Add (res_file, data);
				}
				fixed (byte* ptr = &data [0]) {
					Stream ums = new UnmanagedMemoryStream (ptr, data.Length, data.Length, FileAccess.Read);
					return new StreamResourceInfo (ums, null);
				}
			}
			catch {
				return null;
			}
		}

		/*
		 * Resources take the following format:
		 * 	[/[AssemblyName;component/]]resourcename
		 * They will always be resolved in the following order:
		 * 	1. Application manifest resources
		 * 	2. XAP content
		 */
		public static StreamResourceInfo GetResourceStream (Uri uriResource)
		{
			if (uriResource == null)
				throw new ArgumentNullException ("uriResource");

			if (uriResource.IsAbsoluteUri && uriResource.Scheme != Uri.UriSchemeFile) {
				throw new ArgumentException ("Absolute uriResource");
			}

			// FIXME: URI must point to
			// - the application assembly (embedded resources)
			// - an assembly part of the application package (embedded resources)
			// - something included in the package

			Assembly assembly;
			string assembly_name;
			string resource;
			string loc = uriResource.ToString ();
			int p = loc.IndexOf (';');

			/* We have a resource of the format /assembly;component/resourcename */
			/* It looks like the / is optional tho.  *SIGH* */
			if (p > 0) {
				int l = loc [0] == '/' ? 1 : 0;
				assembly_name = loc.Substring (l, p - l);
				assembly = GetAssembly (assembly_name);
				if (assembly == null)
					return null;

				resource = loc.Substring (p + 11);
			} else {
				assembly = Deployment.Current.EntryAssembly;
				// Deployment.Current.EntryPointAssembly is not usable outside the main thread
				assembly_name = assembly.GetName ().Name;
				resource = loc;
			}

			resource = resource [0] == '/' ? resource : string.Format ("/{0}", resource);
			resource = Path.GetFullPath (resource);
			resource = resource [0] == '/' ? resource.Substring (1) : resource;

			try {
				var manager = new ResourceManager (assembly_name + ".g", assembly) { IgnoreCase = true };
				var stream = manager.GetStream (Uri.EscapeUriString (resource));
				if (stream != null)
					return new StreamResourceInfo (stream, string.Empty);
			} catch {}

			return GetXapResource (resource);
		}

		internal static ManagedStreamCallbacks get_resource_cb_safe (string resourceBase, string name)
		{
			try {
				return get_resource_cb (resourceBase, name);
			} catch (Exception ex) {
				try {
					Console.WriteLine ("Moonlight: Unhandled exception in Application.get_resource_cb: {0}", ex);
				} catch {
				}
			}
			return new ManagedStreamCallbacks ();
		}

		internal static ManagedStreamCallbacks get_resource_cb (string resourceBase, string name)
		{
			StreamResourceInfo info = null;

			if (!string.IsNullOrEmpty (resourceBase)) {
				string combined = string.Format ("{0}{1}",
								 resourceBase.Substring (0, resourceBase.LastIndexOf ('/') + 1),
								 name);

				try {
					info = GetResourceStream (new Uri (combined, UriKind.Relative));
				} catch {}
			}

			if (info == null)
				info = GetResourceStream (new Uri (name, UriKind.Relative));


			if (info == null)
				return new ManagedStreamCallbacks ();

			return new StreamWrapper (info.Stream).GetCallbacks ();
		}

		internal static Assembly GetAssembly (string name)
		{
			foreach (var assembly in Deployment.Current.Assemblies)
				if (assembly.GetName ().Name == name)
					return assembly;

			return null;
		}

		public static StreamResourceInfo GetResourceStream (StreamResourceInfo zipPackageStreamResourceInfo, Uri uriResource)
		{
			if (zipPackageStreamResourceInfo == null)
				throw new ArgumentNullException ("zipPackageStreamResourceInfo");
			if (uriResource == null)
				throw new ArgumentNullException ("resourceUri");
			
			MemoryStream ms = new MemoryStream ();
			ManagedStreamCallbacks source_cb;
			ManagedStreamCallbacks dest_cb;
			StreamWrapper source_wrapper;
			StreamWrapper dest_wrapper;
			Stream source;

			source = zipPackageStreamResourceInfo.Stream;

			source_wrapper = new StreamWrapper (source);
			dest_wrapper = new StreamWrapper (ms);

			source_cb = source_wrapper.GetCallbacks ();
			dest_cb = dest_wrapper.GetCallbacks ();

			if (NativeMethods.managed_unzip_stream_to_stream (ref source_cb, ref dest_cb, uriResource.ToString ())) {
				if (source.CanSeek)
					source.Seek (0, SeekOrigin.Begin);
				ms.Seek (0, SeekOrigin.Begin);
				return new StreamResourceInfo (ms, null);
			}

			return null;
		}

		public static Application Current {
			get {
				IntPtr app = NativeMethods.application_get_current ();
				return NativeDependencyObjectHelper.Lookup (Kind.APPLICATION, app) as Application;
			}

			private set {
				NativeMethods.application_set_current (value == null ? IntPtr.Zero : value.NativeHandle);
			}
		}

		internal static bool IsCurrentSet {
			get {
				IntPtr app = NativeMethods.application_get_current ();
				return NativeDependencyObjectHelper.Lookup (app) != null;
			}
		}
#if NET_3_0
		// desktop assemblies always have (more than) elevated permissions
		public bool HasElevatedPermissions {
			get { return true; }
			set { ; }
		}
#else
		public bool HasElevatedPermissions {
			get { return SecurityManager.HasElevatedPermissions; }
			[EditorBrowsable (EditorBrowsableState.Never)]
			set { SecurityManager.HasElevatedPermissions = value; }
		}
#endif
		public Window MainWindow {
			get {
				Console.WriteLine ("System.Windows.Application.get_MainWindow: NIEX");
				throw new NotImplementedException ();
			}
		}
		
		public ResourceDictionary Resources {
			get {
				return (ResourceDictionary) ((INativeDependencyObjectWrapper)this).GetValue (ResourcesProperty);
			}
			set {
				((INativeDependencyObjectWrapper) this).SetValue (ResourcesProperty, value);
			}
		}

		public UIElement RootVisual {
			get {
				return root_visual;
			}

			set {
				if (value == null)
					throw new InvalidOperationException ();

				// Can only be set once according to the docs.
				if (root_visual != null)
					return;
				
				root_visual = value;

				NativeMethods.surface_attach (Deployment.Current.Surface.Native, root_visual.native);

				var handler = UIARootVisualSet;
				if (handler != null)
					handler (this, EventArgs.Empty);
			}
		}

		public SilverlightHost Host {
			get { return host ?? (host = new SilverlightHost ()); }
		}

		//used by A11Y infrastructure
		internal event EventHandler UIARootVisualSet;
		internal static event EventHandler UIANewApplication;

		public event EventHandler Exit;
		public event StartupEventHandler Startup;
		public event EventHandler<ApplicationUnhandledExceptionEventArgs> UnhandledException;
		
		public event CheckAndDownloadUpdateCompletedEventHandler CheckAndDownloadUpdateCompleted {
			add {
				RegisterEvent (EventIds.Application_CheckAndDownloadUpdateCompletedEvent, value,
					       Events.CreateCheckAndDownloadUpdateCompletedEventHandlerDispatcher (this, value));
			}
			remove {
				UnregisterEvent (EventIds.Application_CheckAndDownloadUpdateCompletedEvent, value);
			}
		}
		
		public event EventHandler InstallStateChanged {
			add {
				RegisterEvent (EventIds.Application_InstallStateChangedEvent, value,
					       Events.CreateEventHandlerDispatcher (value));
			}
			remove {
				UnregisterEvent (EventIds.Application_InstallStateChangedEvent, value);
			}
		}
		
		internal void OnStartup (StartupEventArgs e) {
			// FIXME: should we be sharing the
			// Dictionary<string,string> for each call to
			// the lifetime objects?  or should we be
			// creating a new one for each call?
			ApplicationServiceContext ctx = new ApplicationServiceContext (e.InitParams as Dictionary<string,string>);

			lifetime_objects.Close (); // it's now too late to add items to this collection

			for (int i = 0; i < ApplicationLifetimeObjects.Count; i++) {
				IApplicationService svc = ApplicationLifetimeObjects[i] as IApplicationService;
				if (svc != null)
					svc.StartService (ctx);

			}

			for (int i = 0; i < ApplicationLifetimeObjects.Count; i++) {
				IApplicationLifetimeAware asvc = ApplicationLifetimeObjects[i] as IApplicationLifetimeAware;
				if (asvc != null)
					asvc.Starting();
			}

			if (Startup != null)
				Startup (this, e);

			for (int i = 0; i < ApplicationLifetimeObjects.Count; i++) {
				IApplicationLifetimeAware asvc = ApplicationLifetimeObjects[i] as IApplicationLifetimeAware;
				if (asvc != null)
					asvc.Started();
			}
		}

		// initialized in ReinitializeStaticData
		internal static Dictionary<XmlnsDefinitionAttribute,Assembly> xmlns_definitions;
		internal static List<string> imported_namespaces;
		
		internal static void LoadXmlnsDefinitionMappings (Assembly a)
		{
			object [] xmlns_defs = a.GetCustomAttributes (typeof (XmlnsDefinitionAttribute), false);

			foreach (XmlnsDefinitionAttribute ns_mapping in xmlns_defs){
				xmlns_definitions [ns_mapping] = a;
			}
		}

		internal static void ImportXamlNamespace (string xmlns)
		{
			imported_namespaces.Add (xmlns);
		}

		static Type GetType (Assembly assembly, string ns, string name)
		{
			var fullname = string.IsNullOrEmpty (ns) ? name : ns + "." + name;
			return assembly.GetType (fullname);
		}

		internal static Type GetComponentTypeFromName (string name)
		{
			foreach (var pair in xmlns_definitions) {
				if (!imported_namespaces.Contains (pair.Key.XmlNamespace))
					continue;

				var type = GetType (pair.Value, pair.Key.ClrNamespace, name);
				if (type != null)
					return type;
			}

			return null;
		}

		//
		// Creates the proper component by looking the namespace and name
		// in the various assemblies loaded
		//
		internal static object CreateComponentFromName (string name)
		{
			Type t = GetComponentTypeFromName (name);

			if (t == null) {
				Console.Error.WriteLine ("Application.CreateComponentFromName - could not find type: {0}", name);
				return null;
			}

			return Activator.CreateInstance (t);
		}

		internal static void OnUnhandledException (object sender, Exception ex)
		{
			try {
				Application app = Application.Current;
				string unmanaged_report = ex.ToString();

				if (app != null && app.UnhandledException != null) {
					ApplicationUnhandledExceptionEventArgs args = new ApplicationUnhandledExceptionEventArgs (ex, false);
					try {
						app.UnhandledException (sender, args);
						if (args.Handled)
							unmanaged_report = null;
					} catch (Exception ex2) {
						Console.WriteLine ("Exception caught in Application UnhandledException handler: " + ex2);
						unmanaged_report = ex2.Message;
					}
				} else {
					Console.WriteLine ("Unhandled Exception: " + ex);
				}

				if (unmanaged_report != null) {
					Deployment.Current.EmitError (4004, unmanaged_report);
				}
			} catch {
				// Make this completely safe.
			}
		}

		private static readonly DependencyProperty ResourcesProperty =
			DependencyProperty.Lookup (Kind.APPLICATION, "Resources", typeof (ResourceDictionary));

		IntPtr _native;

		internal IntPtr NativeHandle {
			get { return _native; }
			set {
				if (_native != IntPtr.Zero) {
					throw new InvalidOperationException ("Application.native is already set");
				}

				_native = value;

				free_mapping = NativeDependencyObjectHelper.AddNativeMapping (value, this);
			}
		}

		IntPtr INativeEventObjectWrapper.NativeHandle {
			get { return NativeHandle; }
			set { NativeHandle = value; }
		}

		Kind INativeEventObjectWrapper.GetKind ()
		{
			return Kind.APPLICATION;
		}
	}
}
