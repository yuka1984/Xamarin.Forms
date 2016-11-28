using Xamarin.Forms.CustomAttributes;
using Xamarin.Forms.Internals;

#if UITEST
using Xamarin.UITest;
using NUnit.Framework;
#endif

// Apply the default category of "Issues" to all of the tests in this assembly
// We use this as a catch-all for tests which haven't been individually categorized
#if UITEST
[assembly: NUnit.Framework.Category("Issues")]
#endif

namespace Xamarin.Forms.Controls.Issues
{
	[Preserve(AllMembers = true)]
	[Issue(IssueTracker.Bugzilla, 48158, "Hidden controls become transparent, needs manual verification", PlatformAffected.iOS)]
	public class Bugzilla48158 : TestContentPage // or TestMasterDetailPage, etc ...
	{
		protected override void Init()
		{
			var grdInner = new Grid { BackgroundColor = Color.Red, IsVisible = false, Padding = new Thickness(10) };
			var btn = new Button { Text = "Click and verify background is red" };
			btn.Clicked += (s, e) =>
			{
				grdInner.IsVisible = !grdInner.IsVisible;
			};
			var grd = new Grid();
			grd.Children.Add(grdInner);
			grd.Children.Add(btn);
			Content = grd;
		}
	}
}