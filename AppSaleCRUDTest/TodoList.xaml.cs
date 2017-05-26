using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Xamarin.Forms;

namespace AppSaleCRUDTest
{
    public partial class TodoList : ContentPage
    {
        TodoItemManager manager;
        bool updating = false;
        TodoItem updatingItem;
        public TodoList()
        {
            InitializeComponent();

            manager = TodoItemManager.DefaultManager;

            // OnPlatform<T> doesn't currently support the "Windows" target platform, so we have this check here.
            if (manager.IsOfflineEnabled &&
                (Device.OS == TargetPlatform.Windows || Device.OS == TargetPlatform.WinPhone))
            {
                var syncButton = new Button
                {
                    Text = "Sync items",
                    HeightRequest = 30
                };
                syncButton.Clicked += OnSyncItems;

                buttonsPanel.Children.Add(syncButton);
            }
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            // Set syncItems to true in order to synchronize the data on startup when running in offline mode
            await RefreshItems(true, syncItems: true);
        }

        // Data methods
        async Task AddItem(TodoItem item)
        {
            await manager.SaveTaskAsync(item);
            todoList.ItemsSource = await manager.GetTodoItemsAsync();
        }

        async Task CompleteItem(TodoItem item)
        {
            item.Done = true;
            await manager.SaveTaskAsync(item);
            todoList.ItemsSource = await manager.GetTodoItemsAsync();
        }

        public async void OnAdd(object sender, EventArgs e)
        {
            var todo = new TodoItem();

            if (updating)
            {
                updatingItem.Name = newItemName.Text;
                todo = updatingItem;
                updating = false;
            }
            else
            {
                todo = new TodoItem { Name = newItemName.Text };
            }
            //var todo = new TodoItem { Name = newItemName.Text };
            await AddItem(todo);

            newItemName.Text = string.Empty;
            newItemName.Unfocus();

            exe.Text = "C";
            await RefreshItems(true, true);

        }

        // Event handlers
        async void OnActionSheetSimpleClicked(object sender, EventArgs e)
        {
            var mi = ((MenuItem)sender);
            var todo = mi.CommandParameter as TodoItem;

            var action = await DisplayActionSheet("ActionSheet: Send to?", "Cancel", null, "Delete", "Update");
            //Debug.WriteLine("Action: " + action);
            //await DisplayAlert("More Context Action", "Action: " + action, "OK");
            OnMore(action, todo);
        }

        private async void OnMore(string action, TodoItem todo)
        {
            switch (action)
            {
                case "Delete":
                    manager.DeleteTaskAsync(todo);
                    await RefreshItems(true, true);
                    break;
                case "Update":
                    UpdateDialog(todo);
                    break;                
                }
        }

        async void UpdateDialog(TodoItem todo)
        {
            if (todo.Id != null)
            {
                updatingItem = todo;
                exe.Text = "U";
                newItemName.Text = updatingItem.Name;
                newItemName.Focus();
                updating = true;
                //manager.UpdateTaskAsync(todo);
                //await RefreshItems(true, true);
            }
            else
            {
                //
            }
        }

        private int UpdateClicked(TodoItem todo)
        {
            throw new NotImplementedException();
        }

        //public void OnMore(object sender, EventArgs e)
        //{
        //    var mi = ((MenuItem)sender);
        //    DisplayAlert("More Context Action", mi.CommandParameter + " more context action", "OK");
        //}

        public async void OnSelected(object sender, SelectedItemChangedEventArgs e)
        {
            var todo = e.SelectedItem as TodoItem;
            if (Device.OS != TargetPlatform.iOS && todo != null)
            {
                // Not iOS - the swipe-to-delete is discoverable there
                if (Device.OS == TargetPlatform.Android)
                {
                    var result = await DisplayAlert(todo.Name, "Do you want to DELETE" + todo.Name, "OK","Cancel");
                    if (result)
                    {
                        manager.DeleteTaskAsync(todo);
                        await RefreshItems(true, true);
                    }
                    else
                    {
                            
                    }
                }
                else
                {
                    // Windows, not all platforms support the Context Actions yet
                    if (await DisplayAlert("Mark completed?", "Do you wish to complete " + todo.Name + "?", "Complete", "Cancel"))
                    {
                        await CompleteItem(todo);
                    }
                }
            }

            // prevents background getting highlighted
            todoList.SelectedItem = null;
        }

        // http://developer.xamarin.com/guides/cross-platform/xamarin-forms/working-with/listview/#context
        public async void OnComplete(object sender, EventArgs e)
        {
            var mi = ((MenuItem)sender);
            var todo = mi.CommandParameter as TodoItem;
            await CompleteItem(todo);
        }

        // http://developer.xamarin.com/guides/cross-platform/xamarin-forms/working-with/listview/#pulltorefresh
        public async void OnRefresh(object sender, EventArgs e)
        {
            var list = (ListView)sender;
            Exception error = null;
            try
            {
                await RefreshItems(false, true);
            }
            catch (Exception ex)
            {
                error = ex;
            }
            finally
            {
                list.EndRefresh();
            }

            if (error != null)
            {
                await DisplayAlert("Refresh Error", "Couldn't refresh data (" + error.Message + ")", "OK");
            }
        }

        public async void OnSyncItems(object sender, EventArgs e)
        {
            await RefreshItems(true, true);
        }

        private async Task RefreshItems(bool showActivityIndicator, bool syncItems)
        {
            using (var scope = new ActivityIndicatorScope(syncIndicator, showActivityIndicator))
            {
                todoList.ItemsSource = await manager.GetTodoItemsAsync(syncItems);
            }
        }

        private class ActivityIndicatorScope : IDisposable
        {
            private bool showIndicator;
            private ActivityIndicator indicator;
            private Task indicatorDelay;

            public ActivityIndicatorScope(ActivityIndicator indicator, bool showIndicator)
            {
                this.indicator = indicator;
                this.showIndicator = showIndicator;

                if (showIndicator)
                {
                    indicatorDelay = Task.Delay(2000);
                    SetIndicatorActivity(true);
                }
                else
                {
                    indicatorDelay = Task.FromResult(0);
                }
            }

            private void SetIndicatorActivity(bool isActive)
            {
                this.indicator.IsVisible = isActive;
                this.indicator.IsRunning = isActive;
            }

            public void Dispose()
            {
                if (showIndicator)
                {
                    indicatorDelay.ContinueWith(t => SetIndicatorActivity(false), TaskScheduler.FromCurrentSynchronizationContext());
                }
            }
        }
    }
}

