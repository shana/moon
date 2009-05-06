//
// Control Unit Tests
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

using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Mono.Moonlight.UnitTesting;
using System.Windows.Markup;

namespace MoonTest.System.Windows.Controls {

	public class ConcreteControl : Control
	{

		public bool TemplateAppled
		{
			get;
			private set;
		}

		public object DefaultStyleKey_ {
			get { return base.DefaultStyleKey; }
			set { base.DefaultStyleKey = value; }
		}


		public DependencyObject GetTemplateChild_ (string s)
		{
			return base.GetTemplateChild (s);
		}

		public bool GotFocusCalled = false;
		public bool LostFocusCalled = false;

		protected override void OnGotFocus (RoutedEventArgs e)
		{
			GotFocusCalled = true;
			base.OnGotFocus (e);
		}

		protected override void OnLostFocus (RoutedEventArgs e)
		{
			LostFocusCalled = true;
			base.OnLostFocus (e);
		}

		static public DependencyProperty DefaultStyleKeyProperty_
		{
			get { return Control.DefaultStyleKeyProperty; }
		}

		public void OnGotFocus_ (RoutedEventArgs e)
		{
			base.OnGotFocus (e);
		}

		public void OnLostFocus_ (RoutedEventArgs e)
		{
			base.OnLostFocus (e);
		}

		public void OnKeyDown_ (KeyEventArgs e)
		{
			base.OnKeyDown (e);
		}

		public void OnKeyUp_ (KeyEventArgs e)
		{
			base.OnKeyUp (e);
		}

		public void OnMouseEnter_ (MouseEventArgs e)
		{
			base.OnMouseEnter (e);
		}

		public void OnMouseLeave_ (MouseEventArgs e)
		{
			base.OnMouseLeave (e);
		}

		public void OnMouseMove_ (MouseEventArgs e)
		{
			base.OnMouseMove (e);
		}

		public void OnMouseLeftButtonDown_ (MouseButtonEventArgs e)
		{
			base.OnMouseLeftButtonDown (e);
		}

		public void OnMouseLeftButtonUp_ (MouseButtonEventArgs e)
		{
			base.OnMouseLeftButtonUp (e);
		}

		public override void OnApplyTemplate ()
		{
			TemplateAppled = true;
			base.OnApplyTemplate ();
		}

	}

	[TestClass]
	public class ControlTest {
		class MoreConcreteControl : ConcreteControl {
		}

		class SiblingControl : Control {
		}

		[TestMethod]
		public void DefaultStyleKeyTest_Null ()
		{
			ConcreteControl c = new ConcreteControl ();
			Assert.IsNull (c.DefaultStyleKey_, "null");

			// issue here is that we can't assign the current (null) value without an exception
			// but the PropertyChange logic is "smart" enough not to allow this...
			Assert.Throws<ArgumentException> (delegate {
				c.DefaultStyleKey_ = null;
			}, "null");

			// ... and guess what it's not part of the PropertyChange validation!
			c.SetValue (ConcreteControl.DefaultStyleKeyProperty_, null);
		}

		[TestMethod]
		public void DefaultStyleKeyTest_More ()
		{
			ConcreteControl c = new ConcreteControl ();
			Assert.IsNull (c.DefaultStyleKey_, "null");

			// and some working tests
			c.DefaultStyleKey_ = typeof (ConcreteControl);
			Assert.AreEqual (typeof (ConcreteControl), c.DefaultStyleKey_, "DefaultStyleKey");

			MoreConcreteControl mc = new MoreConcreteControl ();
			mc.DefaultStyleKey_ = typeof (ConcreteControl);
			Assert.AreEqual (typeof (ConcreteControl), mc.DefaultStyleKey_, "DefaultStyleKey-Base");

			c = new ConcreteControl ();
			c.DefaultStyleKey_ = typeof (MoreConcreteControl);
			Assert.AreEqual (typeof (MoreConcreteControl), c.DefaultStyleKey_, "DefaultStyleKey-Inherited");

			mc = new MoreConcreteControl ();
			mc.DefaultStyleKey_ = typeof (SiblingControl);
			Assert.AreEqual (typeof (SiblingControl), mc.DefaultStyleKey_, "DefaultStyleKey-Sibling");

			mc = new MoreConcreteControl ();
			Assert.Throws<ArgumentException> (delegate {
				mc.DefaultStyleKey_ = typeof (Control);
			}, "Control");
		}

		[TestMethod]
		public void GetTemplateChildTest ()
		{
			ConcreteControl c = new ConcreteControl ();
			Assert.Throws<ArgumentException> (delegate {
				c.GetTemplateChild_ (null);
			}, "null");
			Assert.IsNull (c.GetTemplateChild_ (String.Empty), "Empty");
		}

		[TestMethod]
		public void InvalidValues()
		{
			ConcreteControl c = new ConcreteControl();
			c.FontSize = -1;
			c.FontSize = 0;
			c.FontSize = 1000000;

			c.Foreground = null;
			c.FontFamily = null;
		}

		[TestMethod]
		[MoonlightBug ("SetValue should not call DO.ClearValue when value is null")]
		public void NullifyFontFamily()
		{
			ConcreteControl c = new ConcreteControl();
			c.FontFamily = null;
			Assert.Throws<NullReferenceException>(delegate {
				object o = c.FontFamily;
			}, "#1");
			Assert.Throws<NullReferenceException>(delegate {
				c.SetValue (ConcreteControl.FontFamilyProperty, null);
			}, "#2");
			Assert.Throws<NullReferenceException>(delegate {
				Assert.IsNull (c.GetValue (ConcreteControl.FontFamilyProperty), "#3");
			}, "#3");
		}

		[TestMethod]
		public void DefaultMethods ()
		{
			ConcreteControl c = new ConcreteControl ();
			CheckDefaultMethods (c);
			// Focus returns false and does not trigger [Get|Lost]Focus
			Assert.IsFalse (c.GotFocusCalled, "GotFocusCalled");
			Assert.IsFalse (c.LostFocusCalled, "LostFocusCalled");
		}

		static public void CheckDefaultMethods (Control c)
		{
			Assert.IsFalse (c.ApplyTemplate (), "ApplyTemplate");
			Assert.IsFalse (c.Focus (), "Focus");
		}

		[TestMethod]
		public void Events ()
		{
			ConcreteControl c = new ConcreteControl ();
			c.IsEnabledChanged += delegate (object sender, DependencyPropertyChangedEventArgs e) {
				Assert.AreSame (c, sender, "sender");
				Assert.AreEqual (Control.IsEnabledProperty, e.Property, "IsEnabledProperty");
				Assert.IsFalse ((bool) e.NewValue, "NewValue");
				Assert.IsTrue ((bool) e.OldValue, "OldValue");
			};
			c.IsEnabled = false;
			Assert.IsFalse (c.IsEnabled, "IsEnabled");
		}

		[TestMethod]
		public void OnNull ()
		{
			ConcreteControl c = new ConcreteControl ();
			Assert.Throws<ArgumentNullException> (delegate {
				c.OnGotFocus_ (null);
			}, "OnGotFocus");
			Assert.Throws<ArgumentNullException> (delegate {
				c.OnLostFocus_ (null);
			}, "LostFocus");
			Assert.Throws<ArgumentNullException> (delegate {
				c.OnKeyDown_ (null);
			}, "OnKeyDown");
			Assert.Throws<ArgumentNullException> (delegate {
				c.OnKeyUp_ (null);
			}, "OnKeyU");
			Assert.Throws<ArgumentNullException> (delegate {
				c.OnMouseEnter_ (null);
			}, "OnMouseEnter");
			Assert.Throws<ArgumentNullException> (delegate {
				c.OnMouseLeave_ (null);
			}, "OnMouseLeave");
			Assert.Throws<ArgumentNullException> (delegate {
				c.OnMouseMove_ (null);
			}, "OnMouseMove");
			Assert.Throws<ArgumentNullException> (delegate {
				c.OnMouseLeftButtonDown_ (null);
			}, "OnMouseLeftButtonDown");
			Assert.Throws<ArgumentNullException> (delegate {
				c.OnMouseLeftButtonUp_ (null);
			}, "OnMouseLeftButtonUp");
		}
		
		[TestMethod]
		public void MeasureAppliesTemplate ()
		{
			ConcreteControl c = new ConcreteControl ();
			Assert.IsFalse (c.TemplateAppled, "#1");
			c.Measure (new Size (100, 100));
			Assert.IsFalse (c.TemplateAppled, "#2");
			c.ApplyTemplate ();
			Assert.IsFalse (c.TemplateAppled, "#3");
		}

		[TestMethod]
		[MoonlightBug]
		public void MeasureAppliesTemplate2 ()
		{
			ConcreteControl c = (ConcreteControl)XamlReader.Load (@"
<x:ConcreteControl	xmlns=""http://schemas.microsoft.com/client/2007""
					xmlns:x=""clr-namespace:MoonTest.System.Windows.Controls;assembly=moon-unit"">
	<x:ConcreteControl.Template>
		<ControlTemplate>
			<Grid />
		</ControlTemplate>
	</x:ConcreteControl.Template>
</x:ConcreteControl>");

			Assert.IsFalse (c.TemplateAppled, "#1");
			c.Arrange (new Rect (0, 0, 1000, 1000));
			Assert.IsTrue (c.TemplateAppled, "#2");
		}

		[TestMethod]
		public void MeasureAppliesTemplate3 ()
		{
			ConcreteControl c = (ConcreteControl) XamlReader.Load (@"
<x:ConcreteControl	xmlns=""http://schemas.microsoft.com/client/2007""
					xmlns:x=""clr-namespace:MoonTest.System.Windows.Controls;assembly=moon-unit"">
	<x:ConcreteControl.Template>
		<ControlTemplate>
			<Grid />
		</ControlTemplate>
	</x:ConcreteControl.Template>
</x:ConcreteControl>");

			Assert.IsFalse (c.TemplateAppled, "#1");
			c.Measure (new Size (100, 100));
			Assert.IsTrue (c.TemplateAppled, "#3");
		}

		[TestMethod]
		public void SetName ()
		{
			FrameworkElementTest.SetName (new ConcreteControl ());
		}
	}
}
