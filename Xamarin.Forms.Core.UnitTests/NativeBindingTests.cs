using System;
using System.Collections.Generic;
using NUnit.Framework;
using System.ComponentModel;

namespace Xamarin.Forms.Core.UnitTests
{
	public class MockNativeView
	{
		public IList<MockNativeView> SubViews { get; set; }
		public string Foo { get; set; }
		public int Bar { get; set; }
		public string Baz { get; set; }

		public void FireBazChanged()
		{
			BazChanged?.Invoke(this, new TappedEventArgs(null));
		}

		public event EventHandler<TappedEventArgs> BazChanged;
	}

	class MockNativeViewWrapper : View
	{
		public MockNativeView NativeView { get; }

		public MockNativeViewWrapper(MockNativeView nativeView)
		{
			NativeView = nativeView;

			//move all the Attached BPs from the nativeView to this wrapper
			BindableObjectProxy<MockNativeView> proxy;
			if (!BindableObjectProxy<MockNativeView>.BindableObjectProxies.TryGetValue(nativeView, out proxy))
				return;
			proxy.TransferAttachedPropertiesTo(this);
		}

		protected override void OnBindingContextChanged()
		{
			NativeView.SetBindingContext(BindingContext, nv => nv.SubViews);
			base.OnBindingContextChanged();
		}
	}

	public static class MockNativeViewExtensions
	{
		public static View ToView(this MockNativeView nativeView)
		{
			return new MockNativeViewWrapper(nativeView);
		}

		public static void SetBinding(this MockNativeView target, string targetProperty, BindingBase binding, string updateSourceEventName)
		{
			NativeBindingHelpers.SetBinding(target, targetProperty, binding, updateSourceEventName);
		}

		public static void SetBinding(this MockNativeView target, string targetProperty, BindingBase binding, INotifyPropertyChanged propertyChanged = null)
		{
			NativeBindingHelpers.SetBinding(target, targetProperty, binding, propertyChanged);
		}

		public static void SetBinding(this MockNativeView target, BindableProperty targetProperty, BindingBase binding)
		{
			NativeBindingHelpers.SetBinding(target, targetProperty, binding);
		}

		public static void SetValue(this MockNativeView target, BindableProperty targetProperty, object value)
		{
			NativeBindingHelpers.SetValue(target, targetProperty, value);
		}

		public static void SetBindingContext(this MockNativeView target, object bindingContext, Func<MockNativeView, IEnumerable<MockNativeView>> getChild = null)
		{
			NativeBindingHelpers.SetBindingContext(target, bindingContext, getChild);
		}
	}

	class MockINPC : INotifyPropertyChanged
	{
		public void FireINPC(object sender, string propertyName)
		{
			PropertyChanged?.Invoke(sender, new PropertyChangedEventArgs(propertyName));
		}

		public event PropertyChangedEventHandler PropertyChanged;
	}

	class MockVMForNativeBinding : INotifyPropertyChanged
	{
		string fFoo;
		public string FFoo {
			get { return fFoo; }
			set {
				fFoo = value;
				PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("FFoo"));
			}
		}

		int bBar;
		public int BBar {
			get { return bBar; }
			set { 
				bBar = value;
				PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("BBar"));
			}
		}

		public event PropertyChangedEventHandler PropertyChanged;
	}

	[TestFixture]
	public class NativeBindingTests
	{
		[SetUp]
		public void SetUp()
		{
			Device.PlatformServices = new MockPlatformServices();

			//this should collect the ConditionalWeakTable
			GC.Collect();
		}

		[Test]
		public void SetOneWayBinding()
		{
			var nativeView = new MockNativeView();
			Assert.AreEqual(null, nativeView.Foo);
			Assert.AreEqual(0, nativeView.Bar);

			nativeView.SetBinding("Foo", new Binding("FFoo", mode:BindingMode.OneWay));
			nativeView.SetBinding("Bar", new Binding("BBar", mode:BindingMode.OneWay));
			Assert.AreEqual(null, nativeView.Foo);
			Assert.AreEqual(0, nativeView.Bar);

			nativeView.SetBindingContext(new { FFoo = "Foo", BBar = 42 });
			Assert.AreEqual("Foo", nativeView.Foo);
			Assert.AreEqual(42, nativeView.Bar);
		}

		[Test]
		public void AttachedPropertiesAreTransferredFromTheBackpack()
		{
			var nativeView = new MockNativeView();
			nativeView.SetValue(Grid.ColumnProperty, 3);
			nativeView.SetBinding(Grid.RowProperty, new Binding("foo"));

			var view = nativeView.ToView();
			view.BindingContext = new { foo = 42 };
			Assert.AreEqual(3, view.GetValue(Grid.ColumnProperty));
			Assert.AreEqual(42, view.GetValue(Grid.RowProperty));
		}

		[Test]
		public void Set2WayBindings()
		{
			var nativeView = new MockNativeView();
			Assert.AreEqual(null, nativeView.Foo);
			Assert.AreEqual(0, nativeView.Bar);

			var vm = new MockVMForNativeBinding();
			nativeView.SetBindingContext(vm);
			var inpc = new MockINPC();
			nativeView.SetBinding("Foo", new Binding("FFoo", mode:BindingMode.TwoWay), inpc);
			nativeView.SetBinding("Bar", new Binding("BBar", mode:BindingMode.TwoWay), inpc);
			Assert.AreEqual(null, nativeView.Foo);
			Assert.AreEqual(0, nativeView.Bar);
			Assert.AreEqual(null, vm.FFoo);
			Assert.AreEqual(0, vm.BBar);

			nativeView.Foo = "oof";
			inpc.FireINPC(nativeView, "Foo");
			nativeView.Bar = -42;
			inpc.FireINPC(nativeView, "Bar");
			Assert.AreEqual("oof", nativeView.Foo);
			Assert.AreEqual(-42, nativeView.Bar);
			Assert.AreEqual("oof", vm.FFoo);
			Assert.AreEqual(-42, vm.BBar);

			vm.FFoo = "foo";
			vm.BBar = 42;
			Assert.AreEqual("foo", nativeView.Foo);
			Assert.AreEqual(42, nativeView.Bar);
			Assert.AreEqual("foo", vm.FFoo);
			Assert.AreEqual(42, vm.BBar);
		}

		[Test]
		public void Set2WayBindingsWithUpdateSourceEvent()
		{
			var nativeView = new MockNativeView();
			Assert.AreEqual(null, nativeView.Baz);

			var vm = new MockVMForNativeBinding();
			nativeView.SetBindingContext(vm);

			nativeView.SetBinding("Baz", new Binding("FFoo", mode: BindingMode.TwoWay), "BazChanged");
			Assert.AreEqual(null, nativeView.Baz);
			Assert.AreEqual(null, vm.FFoo);

			nativeView.Baz = "oof";
			nativeView.FireBazChanged();
			Assert.AreEqual("oof", nativeView.Baz);
			Assert.AreEqual("oof", vm.FFoo);

			vm.FFoo = "foo";
			Assert.AreEqual("foo", nativeView.Baz);
			Assert.AreEqual("foo", vm.FFoo);
		}

		[Test]
		public void NativeViewsAreCollected()
		{
			WeakReference wr = null;

			int i = 0;
			Action create = null;
			create = () => {
				if (i++ < 1024) {
					create();
					return;
				}

				var nativeView = new MockNativeView();
				nativeView.SetBinding("fooBar", new Binding("Foo", BindingMode.TwoWay));
				nativeView.SetBinding("Baz", new Binding("Qux", BindingMode.TwoWay), "BazChanged");

				wr = new WeakReference(nativeView);
				nativeView = null;

			};

			create();

			GC.Collect();
			GC.WaitForPendingFinalizers();
			GC.Collect();

			Assert.False(wr.IsAlive);
		}

		[Test]
		public void ProxiesAreCollected()
		{
			WeakReference wr = null;

			int i = 0;
			Action create = null;
			create = () => {
				if (i++ < 1024) {
					create();
					return;
				}

				var nativeView = new MockNativeView();
				nativeView.SetBinding("fooBar", new Binding("Foo", BindingMode.TwoWay));
				nativeView.SetBinding("Baz", new Binding("Qux", BindingMode.TwoWay), "BazChanged");

				BindableObjectProxy<MockNativeView> proxy;
				if (!BindableObjectProxy<MockNativeView>.BindableObjectProxies.TryGetValue(nativeView, out proxy))
					Assert.Fail();

				wr = new WeakReference(proxy);
				nativeView = null;
			};

			create();

			GC.Collect();
			GC.WaitForPendingFinalizers();
			GC.Collect();

			Assert.False(wr.IsAlive);
		}
	}
}