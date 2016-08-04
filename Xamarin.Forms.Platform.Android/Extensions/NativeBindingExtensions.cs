using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Java.Beans;

namespace Xamarin.Forms.Platform.Android
{
	public static class NativeBindingExtensions
	{
		public static void SetBinding(this global::Android.Views.View view, string propertyName, BindingBase binding, string eventSourceName=null)
		{
			NativeBindingHelpers.SetBinding(view, propertyName, binding, eventSourceName);
		}

		public static void SetBinding(this global::Android.Views.View view, BindableProperty targetProperty, BindingBase binding)
		{
			NativeBindingHelpers.SetBinding(view, targetProperty, binding);
		}

		public static void SetValue(this global::Android.Views.View target, BindableProperty targetProperty, object value)
		{
			NativeBindingHelpers.SetValue(target, targetProperty, value);
		}

		public static void SetBindingContext(this global::Android.Views.View target, object bindingContext, Func<global::Android.Views.View, IEnumerable<global::Android.Views.View>> getChildren = null)
		{
			NativeBindingHelpers.SetBindingContext(target, bindingContext, getChildren);
		}
	}
}

