using System;
using System.Linq;
using System.Xml;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Views;
using Android.Widget;
using Google.Apis.Tasks.v1.Data;
using Object = Java.Lang.Object;

namespace Google.Apis.Android.Sample
{
	public class ListFragment
		: Fragment
	{
		private ListView list;

		public override View OnCreateView (LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
		{
			if (this.list == null) {
				this.list = new ListView (Activity);
				this.list.LayoutParameters = new ViewGroup.LayoutParams (
					ViewGroup.LayoutParams.FillParent, ViewGroup.LayoutParams.WrapContent);
			}

			return this.list;
		}

		public override void OnDestroyView()
		{
			this.list = null;
			base.OnDestroyView();
		}

		public override void OnCreate (Bundle savedInstanceState)
		{
			SetHasOptionsMenu (true);

			base.OnCreate (savedInstanceState);
		}

		public override void OnStart()
		{
			Refresh();

			base.OnStart();
		}

		public override void OnCreateOptionsMenu (IMenu menu, MenuInflater inflater)
		{
			IMenuItem add = menu.Add ("Create Item");
			add.SetShowAsAction (ShowAsAction.Always);
			add.SetOnMenuItemClickListener (new DelegatedMenuItemListener (OnAddItemClicked));
		}

		private bool OnAddItemClicked (IMenuItem menuItem)
		{
			AlertDialog.Builder builder = new AlertDialog.Builder (Activity);
			builder.SetTitle ("Create Item");

			EditText nameInput = (EditText)Activity.LayoutInflater.Inflate (Resource.Layout.NameDialog, null);

			builder.SetView (nameInput);
			builder.SetPositiveButton ("Add", (sender, args) => CreateItem (nameInput.Text));
			builder.SetNegativeButton ("Cancel", (IDialogInterfaceOnClickListener)null);

			AlertDialog dialog = builder.Create();
			dialog.Show();

			return true;
		}

		private class TasksAdapter
			: BaseAdapter
		{
			private readonly Activity activity;
			private readonly string listId;
			private Task[] tasks;

			public TasksAdapter (Activity activity, string listId, Task[] tasks)
			{
				if (activity == null)
					throw new ArgumentNullException ("activity");
				if (listId == null)
					throw new ArgumentNullException ("listId");
				if (tasks == null)
					throw new ArgumentNullException ("tasks");

				this.activity = activity;
				this.listId = listId;
				this.tasks = tasks;
			}

			public override Object GetItem (int position)
			{
				return null;
			}

			public override long GetItemId (int position)
			{
				return position;
			}

			public override View GetView (int position, View convertView, ViewGroup parent)
			{
				Task task = this.tasks[position];

				bool existing = (convertView != null);
				CheckBox view = (CheckBox)(convertView ?? this.activity.LayoutInflater.Inflate (Resource.Layout.TaskItem, null));
				if (existing)
					view.CheckedChange -= OnChecked;

				view.Text = task.Title;
				view.Checked = task.Status == "completed";
				view.Tag = this.listId + ":" + task.Id;
				view.CheckedChange += OnChecked;

				return view;
			}

			private void OnChecked (object sender, CompoundButton.CheckedChangeEventArgs e)
			{
				CheckBox checkBox = (CheckBox)sender;
				string data = (string)checkBox.Tag;
				string[] ids = data.Split (':');

				MainActivity.Service.Tasks.Get (ids[0], ids[1]).FetchAsync (response =>
				{
					Task task = response.GetResult();
					task.Status = (e.IsChecked) ? "completed" : "needsAction";
					task.Completed = (e.IsChecked) ? XmlConvert.ToString (DateTime.Now, "yyyy-MM-ddTHH:mm:sszzzzzz") : null;

					task = MainActivity.Service.Tasks.Update (task, this.listId, task.Id).Fetch();
					this.activity.RunOnUiThread (() =>
					{
						Task existing = this.tasks.First (t => t.Id == task.Id);
						existing.Status = task.Status;
						existing.Completed = task.Completed;

						this.tasks = this.tasks.OrderBy (t => t.Completed != null).ToArray();
						NotifyDataSetChanged();
					});
				});
			}

			public override int Count
			{
				get { return this.tasks.Length; }
			}
		}

		private void Refresh()
		{
			Activity.SetProgressBarIndeterminateVisibility (true);
			MainActivity.Service.Tasks.List (Tag).FetchAsync (response => {
				var results = response.GetResult();
				if (results.Items == null)
					return;

				Task[] tasks = results.Items.ToArray();

				var activity = Activity;
				if (activity == null)
					return;

				activity.RunOnUiThread (() => {
					if (this.list == null)
						return;

					this.list.Adapter = new TasksAdapter (Activity, Tag, tasks);
				});
			});
		}

		private void CreateItem (string text)
		{
			MainActivity.Service.Tasks.Insert (new Task { Title = text}, Tag)
				.FetchAsync (response => {
					response.GetResult();
					Activity.RunOnUiThread (Refresh);
				});
		}
	}
}