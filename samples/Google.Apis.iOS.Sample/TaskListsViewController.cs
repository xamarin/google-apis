using System;
using System.Collections.Generic;
using System.Linq;
using Google.Apis.Tasks.v1;
using Google.Apis.Tasks.v1.Data;
using Google.Apis.Util;
using MonoTouch.UIKit;
using MonoTouch.Dialog;

namespace Google.Apis.iOS.Sample
{
	public partial class TaskListsViewController : DialogViewController
	{
		private readonly TasksService service;

		public TaskListsViewController (TasksService service)
			: base (UITableViewStyle.Plain, null, true)
		{
			this.service = service;

			Root = new RootElement ("Lists");
			NavigationItem.RightBarButtonItem = new UIBarButtonItem (UIBarButtonSystemItem.Add, OnCreateList);

			RefreshRequested += (s, e) => Refresh();

			Refresh();
		}

		private void Refresh()
		{
			AppDelegate.AddActivity();

			this.service.Tasklists.List().FetchAsync (OnTaskListResponse);
		}

		private UIAlertView create;
		private void OnCreateList (object sender, EventArgs eventArgs)
		{
			this.create = new UIAlertView ("Create List", String.Empty, null, "Cancel", "Create") {
				AlertViewStyle = UIAlertViewStyle.PlainTextInput,
			};
			this.create.ShouldEnableFirstOtherButton = a => 
				!String.IsNullOrWhiteSpace (a.GetTextField(0).Text);

			this.create.Clicked += (s, e) => {
				if (e.ButtonIndex != 1)
					return;

				CreateList (this.create.GetTextField (0).Text);
			};

			this.create.Show();
		}

		private void CreateList (string name)
		{
			if (String.IsNullOrWhiteSpace (name))
				return;

			AppDelegate.AddActivity();

			this.service.Tasklists.Insert (new TaskList { Title = name })
			    .FetchAsync (lr => {
					TaskList result = lr.GetResult();

					AppDelegate.FinishActivity();

					BeginInvokeOnMainThread (() => {
						var tasks = new TasksViewController (this.service, result);
						NavigationController.PushViewController (tasks, animated: true);
					});
			    });
		}

		private void OnTaskListResponse (LazyResult<TaskLists> response)
		{
			TaskLists lists = null;

			try {
				lists = response.GetResult();
			} catch (Exception ex) {
				BeginInvokeOnMainThread (ReloadComplete);
				AppDelegate.FinishActivity();
				ShowError (ex);
			}

			if (lists == null || lists.Items == null) {
				AppDelegate.FinishActivity();
				return;
			}

			Section section = new Section();
			section.AddAll (lists.Items.Select (l =>
				new StringElement (l.Title, () => {
					var tasks = new TasksViewController (this.service, l);
					NavigationController.PushViewController (tasks, true);
				})
			).Cast<Element>());

			BeginInvokeOnMainThread (() => {
				Root.Clear();
				Root.Add (section);

				ReloadComplete();

				AppDelegate.FinishActivity();
			});
		}

		private UIAlertView alert;
		private void ShowError (Exception exception)
		{
			BeginInvokeOnMainThread (() => {
				if (this.alert != null)
					this.alert.Dispose();

				this.alert = new UIAlertView ("Error", exception.Message, null, "Close");
				this.alert.Show();
			});
		}
	}
}