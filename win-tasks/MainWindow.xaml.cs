using System;
using System.Windows;

using Google.Apis.Tasks.v1;
using Google.Apis.Tasks.v1.Data;
using Google.Apis.Services;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Util.Store;
using Google.Apis.Calendar.v3;
using Google.Apis.Calendar.v3.Data;
using System.IO;
using System.Threading;
using System.Linq;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace win_tasks {
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged {

        private data.TaskList? currentList = null;
        public data.TaskList? CurrentList {
            get { return currentList; }
            set { currentList = value; CurrentTask = null; OnPropertyChanged(nameof(CurrentList)); }
        }

        private data.Task? currentTask = null;
        public data.Task? CurrentTask {
            get { return currentTask; }
            set { currentTask = value; OnPropertyChanged(nameof(CurrentTask)); }
        }

        public ObservableCollection<data.TaskList> TaskLists { get; private set; } = new();

        public MainWindow() {
            InitializeComponent();

            this.DataContext = this;
            RefreshAll();
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        public void OnPropertyChanged(string propertyName) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));


        private void btn_Refresh_Click(object sender, RoutedEventArgs e) {
            string currentListId = CurrentList.UID;
            RefreshAll();
            CurrentList = TaskLists.Where(t => t.UID == currentListId).FirstOrDefault();
        }

        public async void RefreshAll() {
            var syncService = new SyncServices.GoogleSyncService("client_secrets.json");
            await syncService.InitService();

            TaskLists.Clear();
            var taskLists = await syncService.GetTaskLists();
            foreach (var l in taskLists.Reverse()) {
                TaskLists.Add(l);
                await syncService.PopulateList(l);

                // populate only one list
                //break;
            }
        }

        public async void RetrieveTasks() {
            // see example:
            // oauth2: https://developers.google.com/api-client-library/dotnet/guide/aaa_oauth
            // dotnet: https://developers.google.com/api-client-library/dotnet/get_started
            // tasksapi: https://developers.google.com/tasks/reference/rest/v1/tasks
            // clientsecrets: https://developers.google.com/api-client-library/dotnet/guide/aaa_client_secrets
            //UserCredential credential; 
            //using (var stream = new FileStream("client_secrets.json", FileMode.Open, FileAccess.Read)) {
            //    credential = await GoogleWebAuthorizationBroker.AuthorizeAsync(
            //        GoogleClientSecrets.Load(stream).Secrets,
            //        new[] { TasksService.Scope.Tasks, CalendarService.Scope.Calendar },
            //        //"user", CancellationToken.None);
            //        "user", CancellationToken.None, new FileDataStore("Tasks.ListMyLibrary1"));
            //}

            //// tasks.org sync list: https://tasks.org/docs/sync.html

            //var service = new TasksService(new BaseClientService.Initializer() {
            //    HttpClientInitializer = credential,
            //    ApplicationName = "win-tasks"
            //});

            //var taskLists = service.Tasklists.List().Execute();
            //var tasksRequest = service.Tasks.List(taskLists.Items[0].Id);
            //tasksRequest.MaxResults = 100;
            //var tasks = tasksRequest.Execute();

            //var service = new CalendarService(new BaseClientService.Initializer() {
            //    HttpClientInitializer = credential,
            //    ApplicationName = "win-tasks"
            //});

            //var cli = service.CalendarList.List().Execute();
            //var idss = cli.Items.Select(it => it.Id).ToList();
            //var evv = service.Events.List(idss[3]).Execute();
        }

        private void Task_MouseUp(object sender, System.Windows.Input.MouseButtonEventArgs e) {
            if (e.OriginalSource is FrameworkElement fe && fe.DataContext is data.Task task) {
                System.Diagnostics.Debug.WriteLine($"Task '{task.UID}' with title '{task.Title}' clicked.");
                CurrentTask = task;
            }
        }
    }
}
