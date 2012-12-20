using System;
using System.Linq;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Views;
using Android.Widget;
using Google.Apis.Authentication.OAuth2;
using Google.Apis.Tasks.v1;
using Google.Apis.Tasks.v1.Data;
using Google.Apis.Util;
using Xamarin.Auth;

namespace Google.Apis.Android.Sample
{
	[Activity(Label = "GoogleApis.Android.Sample", MainLauncher = true, Icon = "@drawable/icon")]
	public class MainActivity : Activity
	{
		public const string ClientID = "Your Client ID";
		public const string RedirectUrl = "Your redirect URL";

		public static TasksService Service;
		private GoogleAuthenticator auth;

		protected override void OnCreate (Bundle bundle)
		{
			base.OnCreate (bundle);

			SetProgressBarIndeterminate (true);
			SetProgressBarIndeterminateVisibility (true);

			this.auth = new GoogleAuthenticator (ClientID, new Uri (RedirectUrl), TasksService.Scopes.Tasks.GetStringValue());

			AccountStore store = AccountStore.Create (this);
			Account savedAccount = store.FindAccountsForService ("google").FirstOrDefault();
			if (savedAccount != null)
			{
				this.auth.Account = savedAccount;
				Setup();
			}
			else
			{
				this.auth.Completed += (sender, args) =>
				{
					if (args.IsAuthenticated)
					{
						store.Save (args.Account, "google");
						RunOnUiThread (Setup);
					}
					else
						Toast.MakeText (this, "Error logging in", ToastLength.Long).Show();
				};

				Intent authIntent = this.auth.GetUI (this);
				StartActivity (authIntent);
			}
		}

		public override bool OnCreateOptionsMenu (IMenu menu)
		{
			IMenuItem createItem = menu.Add ("Create List");
			createItem.SetShowAsAction (ShowAsAction.IfRoom);
			createItem.SetOnMenuItemClickListener (new DelegatedMenuItemListener (OnCreateClicked));

			return true;
		}

		private bool OnCreateClicked (IMenuItem menuItem)
		{
			AlertDialog.Builder builder = new AlertDialog.Builder (this);
			builder.SetTitle ("Create List");

			EditText nameInput = (EditText)LayoutInflater.Inflate (Resource.Layout.NameDialog, null);

			builder.SetView (nameInput);
			builder.SetPositiveButton ("Create", (sender, args) => CreateList (nameInput.Text));
			builder.SetNegativeButton ("Cancel", (IDialogInterfaceOnClickListener)null);

			AlertDialog dialog = builder.Create();
			dialog.Show();

			return true;
		}

		private void CreateList (string text)
		{
			SetProgressBarIndeterminateVisibility (true);

			Service.Tasklists.Insert (new TaskList
			{
				Title = text,
			}).FetchAsync (l =>
			{
				TaskList list = l.GetResult();
				RunOnUiThread(() =>
				{
					SetProgressBarIndeterminateVisibility (false);
					AddListTab (list, select: true);
				});
			});
		}

		private void Setup()
		{
			ActionBar.NavigationMode = ActionBarNavigationMode.Tabs;
			ActionBar.Title = "Tasks Example";
			ActionBar.SetDisplayShowTitleEnabled (true);

			Service = new TasksService (this.auth);

			LoadLists();
		}

		private void LoadLists()
		{
			Service.Tasklists.List().FetchAsync (response =>
			{
				var result = response.GetResult();
				RunOnUiThread (() =>
				{
					ActionBar.RemoveAllTabs();

					if (result.Items == null)
						return;

					foreach (TaskList list in result.Items)
						AddListTab (list);

					SetProgressBarIndeterminateVisibility (false);
				});
			});
		}

		private void AddListTab (TaskList list, bool select = false)
		{
			var listTab = ActionBar.NewTab();
			listTab.SetText (list.Title);
			listTab.SetTabListener (new TabListener<ListFragment> (this, list.Id));

			if (!select)
				ActionBar.AddTab (listTab);
			else
				ActionBar.AddTab (listTab, select);
		}
	}
}

