namespace Xamarin.Forms
{
	public interface IPlatformElementConfiguration<out Plat, out Elem>
							  : IConfigElement<Elem> where Plat : IConfigPlatform
														where Elem : Element
	{
	}
}
