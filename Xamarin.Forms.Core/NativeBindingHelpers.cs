using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using Xamarin.Forms.Internals;

namespace Xamarin.Forms
{
	static class NativeBindingHelpers
	{
		public static void SetBinding<TNativeView>(TNativeView target, string targetProperty, BindingBase bindingBase, string updateSourceEventName) where TNativeView : class
		{
			if (string.IsNullOrEmpty(updateSourceEventName))
				throw new ArgumentNullException(nameof(updateSourceEventName));

			var eventWrapper = new EventWrapper(target, targetProperty, updateSourceEventName);
			SetBinding(target, targetProperty, bindingBase, eventWrapper);
		}

		public static void SetBinding<TNativeView>(TNativeView target, string targetProperty, BindingBase bindingBase, INotifyPropertyChanged propertyChanged = null) where TNativeView : class
		{
			if (target == null)
				throw new ArgumentNullException(nameof(target));
			if (string.IsNullOrEmpty(targetProperty))
				throw new ArgumentNullException(nameof(targetProperty));
			
			var proxy = BindableObjectProxy<TNativeView>.BindableObjectProxies.GetValue(target, (TNativeView key) => new BindableObjectProxy<TNativeView>(key));
			BindableProperty bindableProperty = null;
			propertyChanged = propertyChanged ?? target as INotifyPropertyChanged;
			var binding = bindingBase as Binding;
			bindableProperty = CreateBindableProperty<TNativeView>(targetProperty);
			if (binding != null && binding.Mode != BindingMode.OneWay && propertyChanged != null)
				propertyChanged.PropertyChanged += (sender, e) => {
					if (e.PropertyName != targetProperty)
						return;
				SetValueFromNative<TNativeView>(sender as TNativeView, targetProperty, bindableProperty);
			};
			proxy.SetBinding(bindableProperty, bindingBase);
		}

		static BindableProperty CreateBindableProperty<TNativeView>(string targetProperty) where TNativeView : class
		{
			return BindableProperty.Create(
					targetProperty,
					typeof(object),
					typeof(BindableObjectProxy<TNativeView>),
					defaultBindingMode: BindingMode.Default,
					propertyChanged: (bindable, oldValue, newValue) => {
						TNativeView nativeView;
						if ((bindable as BindableObjectProxy<TNativeView>).TargetReference.TryGetTarget(out nativeView))
							SetNativeValue(nativeView, targetProperty, newValue);
				}
			);
		}

		static void SetNativeValue<TNativeView>(TNativeView target, string targetProperty, object newValue) where TNativeView : class
		{
			target.GetType().GetProperty(targetProperty)?.SetMethod?.Invoke(target, new [] { newValue });
		}

		static void SetValueFromNative<TNativeView>(TNativeView target, string targetProperty, BindableProperty bindableProperty) where TNativeView : class
		{
			BindableObjectProxy<TNativeView> proxy;
			if (!BindableObjectProxy<TNativeView>.BindableObjectProxies.TryGetValue(target, out proxy))
				return;
			SetValueFromRenderer(proxy, bindableProperty, target.GetType().GetProperty(targetProperty)?.GetMethod.Invoke(target, new object [] { }));
		}

		static void SetValueFromRenderer(BindableObject bindable, BindableProperty property, object value)
		{
			bindable.SetValueCore(property, value);
		}

		public static void SetBinding<TNativeView>(TNativeView target, BindableProperty targetProperty, BindingBase binding) where TNativeView : class
		{
			if (target == null)
				throw new ArgumentNullException(nameof(target));
			if (targetProperty == null)
				throw new ArgumentNullException(nameof(targetProperty));
			if (binding == null)
				throw new ArgumentNullException(nameof(binding));
			
			var proxy = BindableObjectProxy<TNativeView>.BindableObjectProxies.GetValue(target, (TNativeView key) => new BindableObjectProxy<TNativeView>(key));
			proxy.BindingsBackpack.Add(new KeyValuePair<BindableProperty, BindingBase>(targetProperty, binding));
		}

		public static void SetValue<TNativeView>(TNativeView target, BindableProperty targetProperty, object value) where TNativeView : class
		{
			if (target == null)
				throw new ArgumentNullException(nameof(target));
			if (targetProperty == null)
				throw new ArgumentNullException(nameof(targetProperty));

			var proxy = BindableObjectProxy<TNativeView>.BindableObjectProxies.GetValue(target, (TNativeView key) => new BindableObjectProxy<TNativeView>(key));
			proxy.ValuesBackpack.Add(new KeyValuePair<BindableProperty, object>(targetProperty, value));
		}

		public static void SetBindingContext<TNativeView>(TNativeView target, object bindingContext, Func<TNativeView, IEnumerable<TNativeView>> getChild = null) where TNativeView : class
		{
			if (target == null)
				throw new ArgumentNullException(nameof(target));

			var proxy = BindableObjectProxy<TNativeView>.BindableObjectProxies.GetValue(target, (TNativeView key) => new BindableObjectProxy<TNativeView>(key));
			proxy.BindingContext = bindingContext;
			if (getChild == null)
				return;
			var children = getChild(target);
			if (children == null)
				return;
			foreach (var child in children)
				if (child != null)
					SetBindingContext(child, bindingContext, getChild);
		}

		class EventWrapper : INotifyPropertyChanged
		{
			string TargetProperty { get; set; }
			static readonly MethodInfo s_handlerinfo = typeof(EventWrapper).GetRuntimeMethods().Single(mi => mi.Name == "OnPropertyChanged" && mi.IsPublic == false);

			public EventWrapper(object target, string targetProperty, string updateSourceEventName)
			{
				TargetProperty = targetProperty;
				Delegate handlerDelegate = null;
				EventInfo updateSourceEvent=null;
				try {
					updateSourceEvent = target.GetType().GetRuntimeEvent(updateSourceEventName);
					handlerDelegate = s_handlerinfo.CreateDelegate(updateSourceEvent.EventHandlerType, this);
				} catch (Exception){
					Log.Warning("EventWrapper", "Can not attach EventWrapper.");
				}
				if (updateSourceEvent != null && handlerDelegate != null)
					updateSourceEvent.AddEventHandler(target, handlerDelegate);
			}

			[Preserve]
			void OnPropertyChanged(object sender, EventArgs e)
			{
				PropertyChanged?.Invoke(sender, new PropertyChangedEventArgs(TargetProperty));
			}

			public event PropertyChangedEventHandler PropertyChanged;
		}
	}
}