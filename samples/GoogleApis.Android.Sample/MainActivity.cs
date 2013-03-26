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

namespace Google.Apis.Android.Sample
{
	[Activity(Label = "GoogleApis.Android.Sample", MainLauncher = true, Icon = "@drawable/icon")]
	public class MainActivity : Activity
	{
		public const string ClientID = "Your Client ID";
		public const string RedirectUrl = "Your redirect URL";

		public static TasksService Service;
		private readonly static GoogleAuthenticator Auth =
			new GoogleAuthenticator (ClientID, new Uri (RedirectUrl), TasksService.Scopes.Tasks.GetStringValue());

		private bool showLogin = true;
		private string selectedId;

		protected override void OnCreate (Bundle bundle)
		{
			base.OnCreate (bundle);

			SetProgressBarIndeterminate (true);
			SetProgressBarIndeterminateVisibility (true);

			ActionBar.NavigationMode = ActionBarNavigationMode.Tabs;
			ActionBar.Title = "Tasks Example";
			ActionBar.SetDisplayShowTitleEnabled (true);

			if (bundle != null)
				this.selectedId = bundle.GetString ("selected");

			this.showLogin = (bundle == null || bundle.GetBoolean ("showLogin", true));
			if (this.showLogin)
				ShowLogin();
			else
				Setup();
		}

		private void ShowLogin()
		{
			this.showLogin = false;
			Intent authIntent = Auth.GetUI (this);
			StartActivityForResult (authIntent, 1);
		}

		private void CreateList (string text)
		{
			SetProgressBarIndeterminateVisibility (true);

			Service.Tasklists.Insert (new TaskList { Title = text })
				.FetchAsync (l => {
					TaskList list = l.GetResult();
					RunOnUiThread(() => {
						SetProgressBarIndeterminateVisibility (false);
						AddListTab (list, select: true);
					});
				});
		}

		private void Setup()
		{
			Service = new TasksService (Auth);

			LoadLists();
		}

		private void LoadLists()
		{
			Service.Tasklists.List().FetchAsync (response => {
				var result = response.GetResult();
				RunOnUiThread (() => {
					ActionBar.RemoveAllTabs();

					if (result.Items == null)
						return;

					ActionBar.Tab[] tabs = result.Items.Select (AddListTab).ToArray();
					if (tabs.Length > 0) {
						var tab = tabs[0];
						if (this.selectedId != null)
							tab = tabs.FirstOrDefault (t => (string)t.Tag == this.selectedId) ?? tab;

						ActionBar.SelectTab (tab);
					}

					SetProgressBarIndeterminateVisibility (false);
				});
			});
		}

		private ActionBar.Tab AddListTab (TaskList list)
		{
			return AddListTab (list, false);
		}

		private ActionBar.Tab AddListTab (TaskList list, bool select)
		{
			var listTab = ActionBar.NewTab();
			listTab.SetText (list.Title);
			listTab.SetTag (list.Id);

			ListFragment existing = (ListFragment)FragmentManager.FindFragmentByTag (list.Id);
			listTab.SetTabListener (new TabListener<ListFragment> (this, list.Id, existing));

			ActionBar.AddTab (listTab, select);

			return listTab;
		}

		protected override void OnActivityResult (int requestCode, Result resultCode, Intent data)
		{
			if (resultCode == Result.Ok) {
				this.showLogin = false;
				RunOnUiThread (Setup);
			} else {
				this.showLogin = true;
				RunOnUiThread (ShowLogin);
			}

			base.OnActivityResult (requestCode, resultCode, data);
		}

		protected override void OnSaveInstanceState (Bundle outState)
		{
			outState.PutBoolean ("showLogin", this.showLogin);
			
			if (ActionBar.SelectedTab != null)
				outState.PutString ("selected", (string)ActionBar.SelectedTab.Tag);

			base.OnSaveInstanceState (outState);
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
	}
}

