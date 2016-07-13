namespace Xamarin.Forms
{
	public interface IElementConfiguration<out Elem> where Elem : Element
	{
		IPlatformElementConfiguration<IConfigWindows, Elem> OnWindows();
		IPlatformElementConfiguration<IConfigAndroid, Elem> OnAndroid();
		IPlatformElementConfiguration<IConfigIOS, Elem> OniOS();
	}
}
