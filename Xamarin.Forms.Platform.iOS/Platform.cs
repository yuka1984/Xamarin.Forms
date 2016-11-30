using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Foundation;
using UIKit;
using RectangleF = CoreGraphics.CGRect;

namespace Xamarin.Forms.Platform.iOS
{
	public class Platform : BindableObject, IPlatform, INavigation, IDisposable
	{
		internal static readonly BindableProperty RendererProperty = BindableProperty.CreateAttached("Renderer", typeof(IVisualElementRenderer), typeof(Platform), default(IVisualElementRenderer),
			propertyChanged: (bindable, oldvalue, newvalue) =>
			{
				var view = bindable as VisualElement;
				if (view != null)
					view.IsPlatformEnabled = newvalue != null;
			});

		readonly int _alertPadding = 10;

		readonly List<Page> _modals;
		readonly PlatformRenderer _renderer;
		bool _animateModals = true;
		bool _appeared;

		bool _disposed;

		internal Platform()
		{
			_renderer = new PlatformRenderer(this);
			_modals = new List<Page>();

			var busyCount = 0;
			MessagingCenter.Subscribe(this, Page.BusySetSignalName, (Page sender, bool enabled) =>
			{
				if (!PageIsChildOfPlatform(sender))
					return;
				busyCount = Math.Max(0, enabled ? busyCount + 1 : busyCount - 1);
				UIApplication.SharedApplication.NetworkActivityIndicatorVisible = busyCount > 0;
			});

			MessagingCenter.Subscribe(this, Page.AlertSignalName, (Page sender, AlertArguments arguments) =>
			{
				if (!PageIsChildOfPlatform(sender))
					return;

				if (Forms.IsiOS8OrNewer)
				{
					PresentAlert(arguments);
				}
				else
				{
					UIAlertView alertView;
					if (arguments.Accept != null)
						alertView = new UIAlertView(arguments.Title, arguments.Message, null, arguments.Cancel, arguments.Accept);
					else
						alertView = new UIAlertView(arguments.Title, arguments.Message, null, arguments.Cancel);

					alertView.Dismissed += (o, args) => arguments.SetResult(args.ButtonIndex != 0);
					alertView.Show();
				}
			});

			MessagingCenter.Subscribe(this, Page.ActionSheetSignalName, (Page sender, ActionSheetArguments arguments) =>
			{
				if (!PageIsChildOfPlatform(sender))
					return;

				var pageRoot = sender;
				while (!Application.IsApplicationOrNull(pageRoot.RealParent))
					pageRoot = (Page)pageRoot.RealParent;
				var pageRenderer = GetRenderer(pageRoot);

				if (Forms.IsiOS8OrNewer)
				{
					PresentActionSheet(arguments);
				}
				else
				{
					var actionSheet = new UIActionSheet(arguments.Title, null, arguments.Cancel, arguments.Destruction, arguments.Buttons.ToArray());

					actionSheet.ShowInView(pageRenderer.NativeView);

					actionSheet.Clicked += (o, args) =>
					{
						string title = null;
						if (args.ButtonIndex != -1)
							title = actionSheet.ButtonTitle(args.ButtonIndex);

						// iOS 8 always calls WillDismiss twice, once with the real result, and again with -1.
						arguments.Result.TrySetResult(title);
					};
				}
			});
		}

		internal UIViewController ViewController
		{
			get { return _renderer; }
		}

		internal Page Page { get; set; }

		void IDisposable.Dispose()
		{
			if (_disposed)
				return;
			_disposed = true;

			Page.DescendantRemoved -= HandleChildRemoved;
			MessagingCenter.Unsubscribe<Page, ActionSheetArguments>(this, Page.ActionSheetSignalName);
			MessagingCenter.Unsubscribe<Page, AlertArguments>(this, Page.AlertSignalName);
			MessagingCenter.Unsubscribe<Page, bool>(this, Page.BusySetSignalName);

			DisposeModelAndChildrenRenderers(Page);
			foreach (var modal in _modals)
				DisposeModelAndChildrenRenderers(modal);

			_renderer.Dispose();
		}

		void INavigation.InsertPageBefore(Page page, Page before)
		{
			throw new InvalidOperationException("InsertPageBefore is not supported globally on iOS, please use a NavigationPage.");
		}

		IReadOnlyList<Page> INavigation.ModalStack
		{
			get { return _modals; }
		}

		IReadOnlyList<Page> INavigation.NavigationStack
		{
			get { return new List<Page>(); }
		}

		Task<Page> INavigation.PopAsync()
		{
			return ((INavigation)this).PopAsync(true);
		}

		Task<Page> INavigation.PopAsync(bool animated)
		{
			throw new InvalidOperationException("PopAsync is not supported globally on iOS, please use a NavigationPage.");
		}

		Task<Page> INavigation.PopModalAsync()
		{
			return ((INavigation)this).PopModalAsync(true);
		}

		async Task<Page> INavigation.PopModalAsync(bool animated)
		{
			var modal = _modals.Last();
			_modals.Remove(modal);
			modal.DescendantRemoved -= HandleChildRemoved;

			var controller = GetRenderer(modal) as UIViewController;

			if (_modals.Count >= 1 && controller != null)
				await controller.DismissViewControllerAsync(animated);
			else
				await _renderer.DismissViewControllerAsync(animated);

			DisposeModelAndChildrenRenderers(modal);

			return modal;
		}

		Task INavigation.PopToRootAsync()
		{
			return ((INavigation)this).PopToRootAsync(true);
		}

		Task INavigation.PopToRootAsync(bool animated)
		{
			throw new InvalidOperationException("PopToRootAsync is not supported globally on iOS, please use a NavigationPage.");
		}

		Task INavigation.PushAsync(Page root)
		{
			return ((INavigation)this).PushAsync(root, true);
		}

		Task INavigation.PushAsync(Page root, bool animated)
		{
			throw new InvalidOperationException("PushAsync is not supported globally on iOS, please use a NavigationPage.");
		}

		Task INavigation.PushModalAsync(Page modal)
		{
			return ((INavigation)this).PushModalAsync(modal, true);
		}

		Task INavigation.PushModalAsync(Page modal, bool animated)
		{
			_modals.Add(modal);
			modal.Platform = this;

			modal.DescendantRemoved += HandleChildRemoved;

			if (_appeared)
				return PresentModal(modal, _animateModals && animated);
			return Task.FromResult<object>(null);
		}

		void INavigation.RemovePage(Page page)
		{
			throw new InvalidOperationException("RemovePage is not supported globally on iOS, please use a NavigationPage.");
		}

		SizeRequest IPlatform.GetNativeSize(VisualElement view, double widthConstraint, double heightConstraint)
		{
			var renderView = GetRenderer(view);
			if (renderView == null || renderView.NativeView == null)
				return new SizeRequest(Size.Zero);

			return renderView.GetDesiredSize(widthConstraint, heightConstraint);
		}

		public static IVisualElementRenderer CreateRenderer(VisualElement element)
		{
			var t = element.GetType();
			var renderer = Registrar.Registered.GetHandler<IVisualElementRenderer>(t) ?? new DefaultRenderer();
			renderer.SetElement(element);
			return renderer;
		}

		public static IVisualElementRenderer GetRenderer(VisualElement bindable)
		{
			return (IVisualElementRenderer)bindable.GetValue(RendererProperty);
		}

		public static void SetRenderer(VisualElement bindable, IVisualElementRenderer value)
		{
			bindable.SetValue(RendererProperty, value);
		}

		protected override void OnBindingContextChanged()
		{
			SetInheritedBindingContext(Page, BindingContext);

			base.OnBindingContextChanged();
		}

		internal void DidAppear()
		{
			_animateModals = false;
			Application.Current.NavigationProxy.Inner = this;
			_animateModals = true;
		}

		internal void DisposeModelAndChildrenRenderers(Element view)
		{
			IVisualElementRenderer renderer;
			foreach (VisualElement child in view.Descendants())
			{
				renderer = GetRenderer(child);
				child.ClearValue(RendererProperty);

				if (renderer != null)
				{
					renderer.NativeView.RemoveFromSuperview();
					renderer.Dispose();
				}
			}

			renderer = GetRenderer((VisualElement)view);
			if (renderer != null)
			{
				if (renderer.ViewController != null)
				{
					var modalWrapper = renderer.ViewController.ParentViewController as ModalWrapper;
					if (modalWrapper != null)
						modalWrapper.Dispose();
				}

				renderer.NativeView.RemoveFromSuperview();
				renderer.Dispose();
			}
			view.ClearValue(RendererProperty);
		}

		internal void DisposeRendererAndChildren(IVisualElementRenderer rendererToRemove)
		{
			if (rendererToRemove == null)
				return;

			if (rendererToRemove.Element != null && GetRenderer(rendererToRemove.Element) == rendererToRemove)
				rendererToRemove.Element.ClearValue(RendererProperty);

			var subviews = rendererToRemove.NativeView.Subviews;
			for (var i = 0; i < subviews.Length; i++)
			{
				var childRenderer = subviews[i] as IVisualElementRenderer;
				if (childRenderer != null)
					DisposeRendererAndChildren(childRenderer);
			}

			rendererToRemove.NativeView.RemoveFromSuperview();
			rendererToRemove.Dispose();
		}

		internal void LayoutSubviews()
		{
			if (Page == null)
				return;

			var rootRenderer = GetRenderer(Page);

			if (rootRenderer == null)
				return;

			rootRenderer.SetElementSize(new Size(_renderer.View.Bounds.Width, _renderer.View.Bounds.Height));
		}

		internal void SetPage(Page newRoot)
		{
			if (newRoot == null)
				return;
			if (Page != null)
				throw new NotImplementedException();
			Page = newRoot;

			if (_appeared == false)
				return;

			Page.Platform = this;
			AddChild(Page);

			Page.DescendantRemoved += HandleChildRemoved;

			Application.Current.NavigationProxy.Inner = this;
		}

		internal void WillAppear()
		{
			if (_appeared)
				return;

			_renderer.View.BackgroundColor = UIColor.White;
			_renderer.View.ContentMode = UIViewContentMode.Redraw;

			Page.Platform = this;
			AddChild(Page);

			Page.DescendantRemoved += HandleChildRemoved;

			_appeared = true;
		}

		void AddChild(VisualElement view)
		{
			if (!Application.IsApplicationOrNull(view.RealParent))
				Console.Error.WriteLine("Tried to add parented view to canvas directly");

			if (GetRenderer(view) == null)
			{
				var viewRenderer = CreateRenderer(view);
				SetRenderer(view, viewRenderer);

				_renderer.View.AddSubview(viewRenderer.NativeView);
				if (viewRenderer.ViewController != null)
					_renderer.AddChildViewController(viewRenderer.ViewController);
				viewRenderer.NativeView.Frame = new RectangleF(0, 0, _renderer.View.Bounds.Width, _renderer.View.Bounds.Height);
				viewRenderer.SetElementSize(new Size(_renderer.View.Bounds.Width, _renderer.View.Bounds.Height));
			}
			else
				Console.Error.WriteLine("Potential view double add");
		}

		void HandleChildRemoved(object sender, ElementEventArgs e)
		{
			var view = e.Element;
			DisposeModelAndChildrenRenderers(view);
		}

		bool PageIsChildOfPlatform(Page page)
		{
			while (!Application.IsApplicationOrNull(page.RealParent))
				page = (Page)page.RealParent;

			return Page == page || _modals.Contains(page);
		}

		// Creates a UIAlertAction which includes a call to hide the presenting UIWindow at the end
		UIAlertAction CreateActionWithWindowHide(string text, UIAlertActionStyle style, Action setResult, UIWindow window)
		{
			return UIAlertAction.Create(text, style,
					a =>
					{
						setResult();
						window.Hidden = true;
					});
		}

		void PresentAlert(AlertArguments arguments)
		{
			var window = new UIWindow { BackgroundColor = Color.Transparent.ToUIColor() };

			var alert = UIAlertController.Create(arguments.Title, arguments.Message, UIAlertControllerStyle.Alert);
			var oldFrame = alert.View.Frame;
			alert.View.Frame = new RectangleF(oldFrame.X, oldFrame.Y, oldFrame.Width, oldFrame.Height - _alertPadding * 2);

			if (arguments.Cancel != null)
			{
				alert.AddAction(CreateActionWithWindowHide(arguments.Cancel, UIAlertActionStyle.Cancel,
					() => arguments.SetResult(false), window));
			}

			if (arguments.Accept != null)
			{
				alert.AddAction(CreateActionWithWindowHide(arguments.Accept, UIAlertActionStyle.Default,
					() => arguments.SetResult(true), window));
			}

			PresentAlert(window, alert);
		}

		void PresentActionSheet(ActionSheetArguments arguments)
		{
			var alert = UIAlertController.Create(arguments.Title, null, UIAlertControllerStyle.ActionSheet);
			var window = new UIWindow { BackgroundColor = Color.Transparent.ToUIColor() };

			if (arguments.Cancel != null)
			{
				alert.AddAction(CreateActionWithWindowHide(arguments.Cancel, UIAlertActionStyle.Cancel, () => arguments.SetResult(arguments.Cancel), window));
			}

			if (arguments.Destruction != null)
			{
				alert.AddAction(CreateActionWithWindowHide(arguments.Destruction, UIAlertActionStyle.Destructive, () => arguments.SetResult(arguments.Destruction), window));
			}

			foreach (var label in arguments.Buttons)
			{
				if (label == null)
					continue;

				var blabel = label;

				alert.AddAction(CreateActionWithWindowHide(blabel, UIAlertActionStyle.Default, () => arguments.SetResult(blabel), window));
			}

			PresentAlert(window, alert, arguments);
		}

		static void PresentAlert(UIWindow window, UIAlertController alert, ActionSheetArguments arguments = null)
		{
			window.RootViewController = new UIViewController();
			window.RootViewController.View.BackgroundColor = Color.Transparent.ToUIColor();
			window.WindowLevel = UIWindowLevel.Alert + 1;
			window.MakeKeyAndVisible();

			if (UIDevice.CurrentDevice.UserInterfaceIdiom == UIUserInterfaceIdiom.Pad && arguments != null)
			{
				UIDevice.CurrentDevice.BeginGeneratingDeviceOrientationNotifications();
				var observer = NSNotificationCenter.DefaultCenter.AddObserver(UIDevice.OrientationDidChangeNotification,
					n => { alert.PopoverPresentationController.SourceRect = window.RootViewController.View.Bounds; });

				arguments.Result.Task.ContinueWith(t =>
				{
					NSNotificationCenter.DefaultCenter.RemoveObserver(observer);
					UIDevice.CurrentDevice.EndGeneratingDeviceOrientationNotifications();
				}, TaskScheduler.FromCurrentSynchronizationContext());

				alert.PopoverPresentationController.SourceView = window.RootViewController.View;
				alert.PopoverPresentationController.SourceRect = window.RootViewController.View.Bounds;
				alert.PopoverPresentationController.PermittedArrowDirections = 0; // No arrow
			}

			window.RootViewController.PresentViewController(alert, true, null);
		}

		async Task PresentModal(Page modal, bool animated)
		{
			var modalRenderer = GetRenderer(modal);
			if (modalRenderer == null)
			{
				modalRenderer = CreateRenderer(modal);
				SetRenderer(modal, modalRenderer);
			}

			var wrapper = new ModalWrapper(modalRenderer);

			if (_modals.Count > 1)
			{
				var topPage = _modals[_modals.Count - 2];
				var controller = GetRenderer(topPage) as UIViewController;
				if (controller != null)
				{
					await controller.PresentViewControllerAsync(wrapper, animated);
					await Task.Delay(5);
					return;
				}
			}

			// One might wonder why these delays are here... well thats a great question. It turns out iOS will claim the 
			// presentation is complete before it really is. It does not however inform you when it is really done (and thus 
			// would be safe to dismiss the VC). Fortunately this is almost never an issue
			await _renderer.PresentViewControllerAsync(wrapper, animated);
			await Task.Delay(5);
		}

		internal class DefaultRenderer : VisualElementRenderer<VisualElement>
		{
		}
	}
}