using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using Google.Apis.Tasks.v1;
using Google.Apis.Tasks.v1.Data;
using Google.Apis.Util;
using MonoTouch.Foundation;
using MonoTouch.UIKit;
using MonoTouch.Dialog;

namespace Google.Apis.iOS.Sample
{
	public class TasksViewController : DialogViewController
	{
		private readonly TasksService service;
		private readonly TaskList list;

		public TasksViewController (TasksService service, TaskList list)
			: base (UITableViewStyle.Plain, null, true)
		{
			if (service == null)
				throw new ArgumentNullException ("service");
			if (list == null)
				throw new ArgumentNullException ("list");

			this.service = service;
			this.list = list;

			NavigationItem.RightBarButtonItem = new UIBarButtonItem (UIBarButtonSystemItem.Add, OnAddItem);

			Root = new RootElement (list.Title) {
				new Section()
			};

			RefreshRequested += (s, e) => Refresh();
			Refresh();
		}

		private UIAlertView create;
		private void OnAddItem (object sender, EventArgs e)
		{
			this.create = new UIAlertView ("Create Task", String.Empty, null, "Cancel", "Create") {
				AlertViewStyle = UIAlertViewStyle.PlainTextInput,
			};
			this.create.ShouldEnableFirstOtherButton = a => 
				!String.IsNullOrWhiteSpace (a.GetTextField(0).Text);

			this.create.Clicked += (s, ce) => {
				if (ce.ButtonIndex != 1)
					return;

				CreateItem (this.create.GetTextField (0).Text);
			};

			this.create.Show();
		}

		private void CreateItem (string text)
		{
			Task task = new Task {
				Title = text
			};

			AppDelegate.AddActivity();

			this.service.Tasks.Insert (task, this.list.Id).FetchAsync (lr => {
				AppDelegate.FinishActivity();

				Refresh();
			});
		}

		private void Refresh()
		{
			AppDelegate.AddActivity();

			this.service.Tasks.List (list.Id).FetchAsync (OnListTasks);
		}

		private void OnListTasks (LazyResult<Tasks.v1.Data.Tasks> response)
		{
			Tasks.v1.Data.Tasks result = null;

			try {
				result = response.GetResult();
			} catch (Exception ex) {
				AppDelegate.FinishActivity();
				ReloadComplete();
				ShowError (ex);
				return;
			}

			BeginInvokeOnMainThread (() => {
				var section = Root.First();
				section.Clear();
				
				if (result.Items != null) {
					section.AddAll (result.Items.Select (t =>
						new StyledStringElement (t.Title, () => OnTap (t)) {
							Accessory = (t.Status == "completed") ? UITableViewCellAccessory.Checkmark : UITableViewCellAccessory.None
						}
					).Cast<Element>());
				}

				ReloadComplete();

				AppDelegate.FinishActivity();
			});
		}

		private void OnTap (Task task)
		{
			AppDelegate.AddActivity();

			bool completing = (task.Status != "completed");
			task.Status = (completing) ? "completed" : "needsAction";
			task.Completed = (completing) ? XmlConvert.ToString (DateTime.Now, "yyyy-MM-ddTHH:mm:sszzzzzz") : null;

			this.service.Tasks.Update (task, this.list.Id, task.Id)
				.FetchAsync (lr => {
					Refresh();
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