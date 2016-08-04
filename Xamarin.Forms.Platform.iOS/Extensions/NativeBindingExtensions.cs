using System;
using System.Collections.Generic;
#if __UNIFIED__
using UIKit;

#else
using MonoTouch.UIKit;
#endif

namespace Xamarin.Forms.Platform.iOS
{
	public static class NativeBindingExtensions
	{
		public static void SetBinding(this UIView view, string propertyName, BindingBase binding, string eventSourceName)
		{
			NativeBindingHelpers.SetBinding(view, propertyName, binding, eventSourceName);
		}

		public static void SetBinding(this UIView view, string propertyName, BindingBase binding)
		{
			NativeViewPropertyListener nativePropertyListener = null;
			if (binding.Mode == BindingMode.TwoWay)
			{
				nativePropertyListener = new NativeViewPropertyListener(propertyName);
				view.AddObserver(nativePropertyListener, propertyName, 0, IntPtr.Zero);
			}

			NativeBindingHelpers.SetBinding(view, propertyName, binding, nativePropertyListener);
		}

		public static void SetValue(this UIView target, BindableProperty targetProperty, object value)
		{
			NativeBindingHelpers.SetValue(target, targetProperty, value);
		}

		public static void SetBindingContext(this UIView target, object bindingContext, Func<UIView, IEnumerable<UIView>> getChildren = null)
		{
			NativeBindingHelpers.SetBindingContext(target, bindingContext, getChildren);
		}
	}
}

