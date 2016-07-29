using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Xamarin.Forms
{
	class BindableObjectProxy<TNativeView> : BindableObject where TNativeView : class
	{
		public static ConditionalWeakTable<TNativeView, BindableObjectProxy<TNativeView>> BindableObjectProxies { get; } = new ConditionalWeakTable<TNativeView, BindableObjectProxy<TNativeView>>();
		public WeakReference<TNativeView> TargetReference { get; set; }
		public IList<KeyValuePair<BindableProperty, BindingBase>> BindingsBackpack { get; } = new List<KeyValuePair<BindableProperty, BindingBase>>();
		public IList<KeyValuePair<BindableProperty, object>> ValuesBackpack { get; } = new List<KeyValuePair<BindableProperty, object>>();

		public BindableObjectProxy(TNativeView target)
		{
			TargetReference = new WeakReference<TNativeView>(target);
		}

		public void TransferAttachedPropertiesTo(View wrapper)
		{
			foreach (var kvp in BindingsBackpack)
				wrapper.SetBinding(kvp.Key, kvp.Value);
			foreach (var kvp in ValuesBackpack)
				wrapper.SetValue(kvp.Key, kvp.Value);
		}
	}
}