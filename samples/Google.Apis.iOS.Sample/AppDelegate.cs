using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Google.Apis.Authentication.OAuth2;
using Google.Apis.Tasks.v1;
using Google.Apis.Tasks.v1.Data;
using Google.Apis.Util;
using MonoTouch.Dialog;
using MonoTouch.Foundation;
using MonoTouch.UIKit;
using Xamarin.Auth;

namespace Google.Apis.iOS.Sample
{
	[Register("AppDelegate")]
	public partial class AppDelegate : UIApplicationDelegate
	{
		public const string ClientID = "Your Client ID";
		public const string RedirectUrl = "Your redirect URL";

		public override bool FinishedLaunching (UIApplication app, NSDictionary options)
		{
			window = new UIWindow (UIScreen.MainScreen.Bounds);

			this.auth = new GoogleAuthenticator (ClientID, new Uri (RedirectUrl), TasksService.Scopes.Tasks.GetStringValue());
			this.auth.Completed += (sender, e) => {
				if (e.IsAuthenticated)
					BeginInvokeOnMainThread (Setup);
				else
					BeginInvokeOnMainThread (ShowLogin);
			};

			ShowLogin();
			
			window.MakeKeyAndVisible();

			return true;
		}

		private void ShowLogin()
		{
			UIViewController loginController = this.auth.GetUI();
			window.RootViewController = loginController;
		}

		private void Setup()
		{
			this.service = new TasksService (this.auth);
			window.RootViewController = new UINavigationController (new TaskListsViewController (this.service));
		}

		private TasksService service;
		private GoogleAuthenticator auth;
		private UIWindow window;

		private static int busy;
		public static void AddActivity()
		{
			UIApplication.SharedApplication.InvokeOnMainThread (() => {
				if (busy++ < 1)
					UIApplication.SharedApplication.NetworkActivityIndicatorVisible = true;
			});
		}

		public static void FinishActivity()
		{
			UIApplication.SharedApplication.InvokeOnMainThread(() => {
				if (--busy < 1)
					UIApplication.SharedApplication.NetworkActivityIndicatorVisible = false;
			});
		}
	}
}