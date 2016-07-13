namespace Xamarin.Forms
{
	#region MDP

	public class MasterDetailPageWindowsConfiguration : IPlatformElementConfiguration<IConfigWindows, MasterDetailPage>
	{
		public MasterDetailPageWindowsConfiguration(MasterDetailPage element)
		{
			Element = element;
		}

		internal static readonly BindableProperty CollapseStyleProperty = BindableProperty.Create(nameof(CollapseStyle), typeof(CollapseStyle),
			typeof(MasterDetailPage), CollapseStyle.None);

		internal static void SetCollapseStyle(BindableObject target, CollapseStyle value)
		{
			target.SetValue(CollapseStyleProperty, value);
		}

		internal static CollapseStyle GetCollapseStyle(MasterDetailPage target)
		{
			return (CollapseStyle)target.GetValue(CollapseStyleProperty);
		}

		internal CollapseStyle CollapseStyle
		{
			get { return (CollapseStyle)Element.GetValue(CollapseStyleProperty); }
			set { Element.SetValue(CollapseStyleProperty, value); }
		}

		public MasterDetailPage Element { get; }
	}

	public static class PlatformSpecificConfigurationExtensions
	{
		public static IPlatformElementConfiguration<IConfigWindows, MasterDetailPage> UseCollapseStyle(this IPlatformElementConfiguration<IConfigWindows, MasterDetailPage> config, CollapseStyle value)
		{
			config.Element.SetValue(MasterDetailPageWindowsConfiguration.CollapseStyleProperty, value);
			return config;
		}

		public static CollapseStyle GetCollapseStyle(this IPlatformElementConfiguration<IConfigWindows, MasterDetailPage> config)
		{
			return (CollapseStyle)config.Element.GetValue(MasterDetailPageWindowsConfiguration.CollapseStyleProperty);
		}

		public static IPlatformElementConfiguration<IConfigWindows, MasterDetailPage> UsePartialCollapse(this IPlatformElementConfiguration<IConfigWindows, MasterDetailPage> config)
		{
			config.Element.SetValue(MasterDetailPageWindowsConfiguration.CollapseStyleProperty, CollapseStyle.Partial);
			return config;
		}
	}

	#endregion

	#region Translucent

	public interface INavigationPagePlatformConfiguration
	{
		INavigationPageWindowsConfiguration OnWindows();

		INavigationPageAndroidConfiguration OnAndroid();

		INavigationPageiOSConfiguration OniOS();
	}

	public interface INavigationPageiOSConfiguration : IConfigElement<NavigationPage>
	{
		bool IsNavigationBarTranslucent { get; set; }
	}

	public interface INavigationPageAndroidConfiguration : IConfigElement<NavigationPage>
	{
	}

	public interface INavigationPageWindowsConfiguration : IConfigElement<NavigationPage>
	{
	}

	public class NavigationPageiOSConfiguration : INavigationPageiOSConfiguration
	{
		public NavigationPageiOSConfiguration(NavigationPage element)
		{
			Element = element;
		}

		public NavigationPage Element { get; }

		public bool IsNavigationBarTranslucent
		{
			get { return (bool)Element.GetValue(NavigationPageiOSpecifics.IsNavigationBarTranslucentProperty); }
			set { Element.SetValue(NavigationPageiOSpecifics.IsNavigationBarTranslucentProperty, value); }
		}
	}

	public class NavigationPageAndroidConfiguration : INavigationPageAndroidConfiguration
	{
		public NavigationPageAndroidConfiguration(NavigationPage element)
		{
			Element = element;
		}

		public NavigationPage Element { get; }
	}


	public class NavigationPageWindowsConfiguration : INavigationPageWindowsConfiguration
	{
		public NavigationPageWindowsConfiguration(NavigationPage element)
		{
			Element = element;
		}

		public NavigationPage Element { get; }
	}


	public static class NavigationPageiOSpecifics
	{
		public static readonly BindableProperty IsNavigationBarTranslucentProperty = BindableProperty.Create("IsNavigationBarTranslucent", typeof(bool),
			typeof(NavigationPage), false);

		public static void SetIsNavigationBarTranslucent(this NavigationPage navigationPage, bool value)
		{
			navigationPage.SetValue(IsNavigationBarTranslucentProperty, value);
		}

		public static bool GetIsNavigationBarTranslucent(this NavigationPage navigationPage)
		{
			return (bool)navigationPage.GetValue(IsNavigationBarTranslucentProperty);
		}
	}

	#endregion
}