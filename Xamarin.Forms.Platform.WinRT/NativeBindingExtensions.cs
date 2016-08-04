using System;
using System.Collections.Generic;
using System.ComponentModel;
using Windows.UI.Xaml;

#if WINDOWS_UWP

namespace Xamarin.Forms.Platform.UWP
#else

namespace Xamarin.Forms.Platform.WinRT
#endif
{
	public static class NativeBindingExtensions
	{
		public static void SetBinding(this FrameworkElement view, string propertyName, BindingBase binding, string eventSourceName)
		{
			NativeEventWrapper eventE = null;
			if (binding.Mode == BindingMode.TwoWay && !(view is INotifyPropertyChanged))
				eventE = new NativeEventWrapper(view, propertyName, eventSourceName);

			NativeBindingHelpers.SetBinding(view, propertyName, binding, eventE);
		}

		public static void SetBinding(this FrameworkElement view, string propertyName, BindingBase binding)
		{
			NativePropertyListener nativePropertyListener = null;
			if (binding.Mode == BindingMode.TwoWay)
				nativePropertyListener = new NativePropertyListener(view, propertyName);


			NativeBindingHelpers.SetBinding(view, propertyName, binding, nativePropertyListener as INotifyPropertyChanged);
		}

		public static void SetValue(this FrameworkElement target, BindableProperty targetProperty, object value)
		{
			NativeBindingHelpers.SetValue(target, targetProperty, value);
		}

		public static void SetBindingContext(this FrameworkElement target, object bindingContext, Func<FrameworkElement, IEnumerable<FrameworkElement>> getChildren = null)
		{
			NativeBindingHelpers.SetBindingContext(target, bindingContext, getChildren);
		}
	}
}
