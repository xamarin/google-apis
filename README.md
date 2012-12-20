# Google APIs for Xamrin

Quickly add access to Google's APIs to your Mono for Android app!

To get started with the Google APIs, you'll need to register your app
as a web application and obtain your client ID at the
[Google API Console](https://code.google.com/apis/console/).

A set of generated APIs are included, but additional or newer versions
of the APIs may be available at
[Google API .NET client WIKI](http://code.google.com/p/google-api-dotnet-client/wiki/APIs).

Before you can access the APIs, the user will need to login to Google.
You'll need to supply your Client ID, the redirect URI you supplied to Google, and
the scopes you're requesting. Each API has a set of scopes that will enable you to
access certain functionality.

```csharp
var auth = new Google.Apis.Authentication.OAuth2.GoogleAuthenticator (ClientID,
				new Uri ("http://example.com/callback"),
				Google.Apis.Tasks.v1.TasksService.Scopes.Tasks.GetStringValue());

// When we're authenticated, we'll show the tasks from the default list
Action showTasks = () =>
{
	var service = new Google.Apis.Tasks.v1.TasksService (auth);

	// get the tasks from the default task list
	var tasks = service.Tasks.List("@default").Fetch();
	foreach (var task in tasks.Items)
		Console.WriteLine (task.Title);
};

// We don't want to have to login every time, so we'll use the Xamarin.Auth AccoutnStore
AccountStore store = AccountStore.Create (this);
Account savedAccount = store.FindAccountsForService ("google").FirstOrDefault();
if (savedAccount != null)
{
	this.auth.Account = savedAccount;
	showTasks();
}
else
{
	this.auth.Completed += (sender, args) =>
	{
		if (args.IsAuthenticated)
		{
			// Save the account for the future
			store.Save (args.Account, "google");
			RunOnUiThread (showTasks);
		}
		else // Authentication failed
			Toast.MakeText (this, "Error logging in", ToastLength.Long).Show();
	};

	Intent authIntent = this.auth.GetUI (this);
	StartActivity (authIntent);
}

Intent loginIntent = auth.GetUI (this);
StartActivity (loginIntent);
```