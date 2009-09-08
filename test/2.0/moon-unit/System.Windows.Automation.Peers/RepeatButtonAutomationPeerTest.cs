//
// Unit tests for RepeatButtonAutomationPeer
//
// Contact:
//   Moonlight List (moonlight-list@lists.ximian.com)
//
// Copyright (C) 2009 Novell, Inc (http://www.novell.com)
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
using System.Windows.Controls.Primitives;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;

using Mono.Moonlight.UnitTesting;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace MoonTest.System.Windows.Automation.Peers {

	[TestClass]
	public class RepeatButtonAutomationPeerTest : ButtonBaseAutomationPeerTest {

		[TestMethod]
		public override void GetPattern ()
		{
			RepeatButtonAutomationPeer rbap = new RepeatButtonAutomationPeer (new RepeatButton ());
			Assert.AreEqual (AutomationControlType.Button, rbap.GetAutomationControlType (), "GetAutomationControlType");
			Assert.AreEqual ("RepeatButton", rbap.GetClassName (), "GetClassName");

			Assert.IsNull (rbap.GetPattern (PatternInterface.Dock), "Dock");
			Assert.IsNull (rbap.GetPattern (PatternInterface.ExpandCollapse), "ExpandCollapse");
			Assert.IsNull (rbap.GetPattern (PatternInterface.Grid), "Grid");
			Assert.IsNull (rbap.GetPattern (PatternInterface.GridItem), "GridItem");
			Assert.IsTrue (Object.ReferenceEquals (rbap, rbap.GetPattern (PatternInterface.Invoke)), "Invoke");
			Assert.IsNull (rbap.GetPattern (PatternInterface.MultipleView), "MultipleView");
			Assert.IsNull (rbap.GetPattern (PatternInterface.RangeValue), "RangeValue");
			Assert.IsNull (rbap.GetPattern (PatternInterface.Scroll), "Scroll");
			Assert.IsNull (rbap.GetPattern (PatternInterface.ScrollItem), "ScrollItem");
			Assert.IsNull (rbap.GetPattern (PatternInterface.Selection), "Selection");
			Assert.IsNull (rbap.GetPattern (PatternInterface.SelectionItem), "SelectionItem");
			Assert.IsNull (rbap.GetPattern (PatternInterface.Table), "Table");
			Assert.IsNull (rbap.GetPattern (PatternInterface.TableItem), "TableItem");
			Assert.IsNull (rbap.GetPattern (PatternInterface.Toggle), "Toggle");
			Assert.IsNull (rbap.GetPattern (PatternInterface.Transform), "Transform");
			Assert.IsNull (rbap.GetPattern (PatternInterface.Value), "Value");
			Assert.IsNull (rbap.GetPattern (PatternInterface.Window), "Window");

			Assert.IsNotNull (rbap.GetPattern (PatternInterface.Invoke), "Invoke #1");
		}

		[TestMethod]
		public override void GetClassName ()
		{
			FrameworkElementAutomationPeerContract feap
				= CreateConcreteFrameworkElementAutomationPeer (CreateConcreteFrameworkElement ());
			Assert.AreEqual ("RepeatButton", feap.GetClassName (), "GetClassNameCore");
			Assert.AreEqual ("RepeatButton", feap.GetClassNameCore_ (), "GetClassNameCoreCore");
		}

		[TestMethod]
		public override void GetAutomationControlType ()
		{
			RepeatButtonAutomationPeerPoker bapp = new RepeatButtonAutomationPeerPoker (new RepeatButton ());
			Assert.AreEqual (AutomationControlType.Button, bapp.GetAutomationControlType (), "GetAutomationControlType");
			Assert.AreEqual (AutomationControlType.Button, bapp.GetAutomationControlTypeCore_ (), "GetAutomationControlTypeCore");
		}

		[TestMethod]
		public void IInvokeProvider_Invoke ()
		{
			Test_InvokeProvider_Invoke (CreateConcreteFrameworkElement () as ButtonBase);
		}

		[TestMethod]
		public void InvokeProvider_Events ()
		{
			if (!EventsManager.Instance.AutomationSingletonExists)
				return;

			Test_InvokeProvider_Events ((ButtonBase) CreateConcreteFrameworkElement ());
		}

		public class RepeatButtonAutomationPeerPoker : RepeatButtonAutomationPeer, FrameworkElementAutomationPeerContract {

			public RepeatButtonAutomationPeerPoker (RepeatButton owner)
				: base (owner)
			{
			}

			#region Overridden methods

			public AutomationControlType GetAutomationControlTypeCore_ ()
			{
				return base.GetAutomationControlTypeCore ();
			}

			public string GetClassNameCore_ ()
			{
				return base.GetClassNameCore ();
			}

			#endregion

			#region FrameworkElementAutomationPeerContract Members

			public AutomationPeer GetLabeledByCore_ ()
			{
				return base.GetLabeledByCore ();
			}

			public string GetNameCore_ ()
			{
				return base.GetNameCore ();
			}

			public bool IsContentElementCore_ ()
			{
				return base.IsContentElementCore ();
			}

			public bool IsControlElementCore_ ()
			{
				return base.IsControlElementCore ();
			}

			public string GetAcceleratorKeyCore_ ()
			{
				return base.GetAcceleratorKeyCore ();
			}

			public string GetAccessKeyCore_ ()
			{
				return base.GetAccessKeyCore ();
			}

			public string GetAutomationIdCore_ ()
			{
				return base.GetAutomationIdCore ();
			}

			public global::System.Windows.Rect GetBoundingRectangleCore_ ()
			{
				return base.GetBoundingRectangleCore ();
			}

			public global::System.Collections.Generic.List<AutomationPeer> GetChildrenCore_ ()
			{
				return base.GetChildrenCore ();
			}

			public global::System.Windows.Point GetClickablePointCore_ ()
			{
				return base.GetClickablePointCore ();
			}

			public string GetHelpTextCore_ ()
			{
				return base.GetHelpTextCore ();
			}

			public string GetItemStatusCore_ ()
			{
				return base.GetItemStatusCore ();
			}

			public string GetItemTypeCore_ ()
			{
				return base.GetItemTypeCore ();
			}

			public string GetLocalizedControlTypeCore_ ()
			{
				return base.GetLocalizedControlTypeCore ();
			}

			public AutomationOrientation GetOrientationCore_ ()
			{
				return base.GetOrientationCore ();
			}

			public bool HasKeyboardFocusCore_ ()
			{
				return base.HasKeyboardFocusCore ();
			}

			public bool IsEnabledCore_ ()
			{
				return base.IsEnabledCore ();
			}

			public bool IsKeyboardFocusableCore_ ()
			{
				return base.IsKeyboardFocusableCore ();
			}

			public bool IsOffscreenCore_ ()
			{
				return base.IsOffscreenCore ();
			}

			public bool IsPasswordCore_ ()
			{
				return base.IsPasswordCore ();
			}

			public bool IsRequiredForFormCore_ ()
			{
				return base.IsRequiredForFormCore ();
			}

			#endregion
		}

		protected override FrameworkElement CreateConcreteFrameworkElement ()
		{
			return new RepeatButton ();
		}

		protected override FrameworkElementAutomationPeerContract CreateConcreteFrameworkElementAutomationPeer (FrameworkElement element)
		{
			return new RepeatButtonAutomationPeerPoker (element as RepeatButton);
		}
	}
}
