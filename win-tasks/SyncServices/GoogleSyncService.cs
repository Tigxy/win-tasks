using Google.Apis.Auth.OAuth2;
using Google.Apis.Tasks.v1;
using GoogleData = Google.Apis.Tasks.v1.Data;
using Google.Apis.Util.Store;
using System;
using System.IO;
using System.Threading;
using TTasks = System.Threading.Tasks;
using win_tasks.data;
using Google.Apis.Services;
using System.Xml;
using System.Collections.Generic;
using System.Linq;
using win_tasks.Data;

namespace win_tasks.SyncServices {
    internal class GoogleSyncService {

        // see https://developers.google.com/tasks/reference/rest/v1/tasklists

        private TasksService service;
        private readonly string oAuthSecretsFile;

        public GoogleSyncService(string OAuthSecretsFile) {
            oAuthSecretsFile = OAuthSecretsFile;
        }

        public async TTasks.Task InitService() {
            UserCredential credential;
            using (var stream = new FileStream(oAuthSecretsFile, FileMode.Open, FileAccess.Read)) {
                credential = await GoogleWebAuthorizationBroker.AuthorizeAsync(
                    GoogleClientSecrets.FromStream(stream).Secrets,
                    new[] { TasksService.Scope.Tasks },
                    "user", CancellationToken.None,
                    new FileDataStore(Globals.GetAppPath(), fullPath: true));
            }

            service = new TasksService(new BaseClientService.Initializer() {
                HttpClientInitializer = credential,
                ApplicationName = Globals.APP_NAME
            });
        }

        private DateTime? FromGoogleDateTime(string? value) {
            // date time format based on RFC3339 timestamp
            // see https://developers.google.com/tasks/reference/rest/v1/tasklists
            // and https://stackoverflow.com/a/91146
            if (value is null)
                return null;
            return XmlConvert.ToDateTime(value, XmlDateTimeSerializationMode.Utc);
        }

        private string? ToGoogleDateTime(DateTime? value) {
            if (value is null)
                return null;
            return XmlConvert.ToString((DateTime)value, XmlDateTimeSerializationMode.Utc);
        }

        private TaskList FromGoogleTaskList(GoogleData.TaskList list) {
            // create new task list instance
            // note that list has to be populated with tasks externally
            TaskList localList = new() {
                UID = list.Id,
                Title = list.Title,
                ModificationDate = FromGoogleDateTime(list.Updated)
            };
            localList.SetUnderlyingTasklist(list);
            return localList;
        }

        private GoogleData.TaskList ToGoogleTaskList(TaskList taskList) {
            var underlyingTaskList = taskList.GetUnderlyingTasklist<GoogleData.TaskList>();

            // only update the essential properties as everything else will be set on Google's side anyways
            underlyingTaskList.Title = taskList.Title;
            return underlyingTaskList;
        }

        private Task FromGoogleTask(GoogleData.Task task) {
            // other properties (children, parent, ...) have to be set somewhere else
            Task t = new Task() {
                UID = task.Id,
                Title = task.Title,
                Description = task.Notes,
                DueDate = FromGoogleDateTime(task.Due),
                ModificationDate = FromGoogleDateTime(task.Updated),
                CompletionDate = FromGoogleDateTime(task.Completed),
                IsCompleted = task.Status == "completed"
            };
            t.SetUnderlyingTask(task);
            return t;
        }

        private GoogleData.Task ToGoogleTask(Task task) {
            var utask = task.GetUnderlyingTask<GoogleData.Task>();

            return new() {
                // members that may be overriden
                Title = task.Title,
                Notes = task.Description,
                Due = ToGoogleDateTime(task.DueDate),
                Completed = ToGoogleDateTime(task.CompletionDate),
                Updated = ToGoogleDateTime(task.ModificationDate),
                Status = task.IsCompleted ? "completed" : "needsAction",

                // properties that won't be modified
                Id = utask?.Id,
                Kind = utask?.Kind,
                Links = utask?.Links,
                Hidden = utask?.Hidden,
                Deleted = utask?.Deleted,
                ETag = utask?.ETag,
                Position = utask?.Position,
                SelfLink = utask?.SelfLink,

                // property set by Move() methods
                Parent = utask?.Parent
            };
        }

        private TaskList UpdateFromGoogleTaskList(TaskList list, GoogleData.TaskList updated) {
            var updatedList = FromGoogleTaskList(updated);

            // restore references from old list
            updatedList.Tasks = list.Tasks;
            updatedList.Tasks.ForEach(t => {
                t.ParentList = updatedList;
                t.Subtasks.ForEach(st => st.ParentList = updatedList);
            });
            
            return updatedList;
        }

        private Task UpdateFromGoogleTask(Task task, GoogleData.Task updated) { 
            var updatedTask = FromGoogleTask(updated);
            updatedTask.ParentList = task.ParentList;

            // restore references from old task
            // we do this instead of just updating the different properties, as this requires less effort and
            // allows adding new properties later on without touching this method again
            updatedTask.Subtasks = task.Subtasks;
            updatedTask.Subtasks.ForEach(t => t.Parent = t);
            
            // restore parent reference
            updatedTask.Parent = task.Parent;

            // differenciate between top-level task and subtask
            var listContainingTask = updatedTask.Parent is null ? updatedTask.ParentList.Tasks : updatedTask.Parent.Subtasks;

            // replace old with new task instance
            listContainingTask[listContainingTask.IndexOf(task)] = updatedTask;

            return updatedTask;
        }

        private async TTasks.Task<GoogleData.TaskList[]> GetGoogleTaskLists() {
            // create request for task
            // see https://developers.google.com/tasks/reference/rest/v1/tasklists/list
            var request = service.Tasklists.List();
            var res = await request.ExecuteAsync();
            return res.Items.ToArray();
        }

        private void UpdateListRequestFields(TasksResource.ListRequest request, TaskFilter filter) {
            // filter by modification date
            if (filter.updatedMin is not null)
                request.UpdatedMin = ToGoogleDateTime((DateTime)filter.updatedMin);

            // filter by due date
            if (filter.dueMin is not null)
                request.DueMin = ToGoogleDateTime((DateTime)filter.dueMin!);
            if (filter.dueMax is not null)
                request.DueMax = ToGoogleDateTime((DateTime)filter.dueMax);

            // filter by completion date
            if (filter.completedMin is not null)
                request.CompletedMin = ToGoogleDateTime((DateTime)filter.completedMin!);
            if (filter.completedMax is not null)
                request.CompletedMin = ToGoogleDateTime((DateTime)filter.completedMax);

            // filter by completion state
            if (filter.loadCompleted)
                request.ShowCompleted = filter.loadCompleted;
        }

        public async TTasks.Task<GoogleData.Task[]> GetGoogleTasks(string listId, TaskFilter filter) {

            // create request for task
            // see https://developers.google.com/tasks/reference/rest/v1/tasks/update
            var request = service.Tasks.List(listId);
            UpdateListRequestFields(request, filter);

            // google only allows to retrieve a maximum of 100 tasks per request,
            // thus we need to iterate through the pages and collect individual tasks
            request.MaxResults = 100;

            string pageToken = "";
            List<GoogleData.Task> tasks = new();

            do {
                // retrieve tasks
                var result = await request.ExecuteAsync();

                // add to local collection
                tasks.AddRange(result.Items);

                // set parameters for retrieving next page (if one exists)
                pageToken = result.NextPageToken;
                request.PageToken = pageToken;

                // do-while to perform at least one request
            } while (!string.IsNullOrEmpty(pageToken));

            return tasks.ToArray();
        }

        public async TTasks.Task<TaskList[]> GetTaskLists() {
            var lists = await GetGoogleTaskLists();
            return lists.Select(list => FromGoogleTaskList(list)).ToArray();
        }

        public async TTasks.Task<TaskList> PopulateList(TaskList list) => await PopulateList(list, new TaskFilter());

        public async TTasks.Task<TaskList> PopulateList(TaskList list, TaskFilter filter) {
            var gTasks = await GetGoogleTasks(list.UID, filter);
            var tasks = gTasks.Select(t => FromGoogleTask(t))
                // need to do .ToList() to get hard references right away
                // see https://stackoverflow.com/a/43055780
                .ToList(); 

            // Create lookup dictionary for tasks
            var lookup = new Dictionary<string, Task>();
            foreach (var task in tasks) {
                lookup[task.UID] = task;
            }

            // build task tree
            foreach (var task in tasks) {
                GoogleData.Task gTask = task.GetUnderlyingTask<GoogleData.Task>();
                if (!string.IsNullOrEmpty(gTask.Parent)) {
                    // set parent-child references
                    lookup[gTask.Parent].Subtasks.Add(task);
                    task.Parent = lookup[gTask.Parent];
                }
            }

            foreach (var task in tasks) {
                // restore order of tasks in their respective lists
                task.Subtasks = task.Subtasks.OrderBy(t => t.GetUnderlyingTask<GoogleData.Task>().Position).ToList();

                // ... and also set parent list reference
                task.ParentList = list;
            }

            // top level tasks don't have any parents
            var topLevelTasks = tasks.Where(t => t.Parent is null).ToList();

            list.Tasks = topLevelTasks;
            return list;
        }

        public async TTasks.Task<TaskList> UpdateList(TaskList list) {
            // transform to google task format
            var gTaskList = ToGoogleTaskList(list);

            // create request for task
            // see https://developers.google.com/tasks/reference/rest/v1/tasks/update
            var request = service.Tasklists.Update(gTaskList, list.UID);

            // execute request
            var res = await request.ExecuteAsync();

            return UpdateFromGoogleTaskList(list, res);
        }

        public async TTasks.Task<Task> UpdateTask(Task task) {
            // transform to google task format
            var gTask = ToGoogleTask(task);

            // create request for task
            // see https://developers.google.com/tasks/reference/rest/v1/tasks/update
            var request = service.Tasks.Update(gTask, task.ParentList.UID, task.UID);

            // execute request
            var res = await request.ExecuteAsync();

            return UpdateFromGoogleTask(task, res);
        }

        public async TTasks.Task<TaskList> AddList(TaskList list) {
            var gList = ToGoogleTaskList(list);

            // create request for task
            // see https://developers.google.com/tasks/reference/rest/v1/tasklists/insert
            var request = service.Tasklists.Insert(gList);

            // execute request
            var res = await request.ExecuteAsync();

            return UpdateFromGoogleTaskList(list, res);
        }


        public async TTasks.Task<Task> AddTask(Task task) {
            // transform to google task format
            var gTask = ToGoogleTask(task);

            // create request for task
            // see https://developers.google.com/tasks/reference/rest/v1/tasks/insert
            var request = service.Tasks.Insert(gTask, task.ParentList.UID);

            // provide additional parameters
            if (task.PreviousTask is not null)
                request.Previous = task.PreviousTask.UID;
            if (task.Parent is not null)
                request.Parent = task.Parent.UID;

            // execute request
            var res = await request.ExecuteAsync();

            return UpdateFromGoogleTask(task, res);
        }

        public async TTasks.Task<Task> Move(Task task) {
            if (false) { }
            // TODO: allow move between lists
            //// differentiate whether task should be moved to a different list or just inside task list
            //if (list is not null && list.UID != task.ParentList.UID) {
            //    // move to new task list (requires add & delete operations) 
            //    throw new NotImplementedException();
            //}
            else {
                // perform move request inside list
                // see https://developers.google.com/tasks/reference/rest/v1/tasks/move
                var request = service.Tasks.Move(task.ParentList.UID, task.UID);

                // add parameters to define new position
                if (task.PreviousTask is not null)
                    request.Previous = task.PreviousTask.UID;
                if (task.Parent is not null)
                    request.Parent = task.Parent.UID;

                // execute request
                var res = await request.ExecuteAsync();

                return UpdateFromGoogleTask(task, res);
            }
        }

        public async TTasks.Task<bool> DeleteList(TaskList list) {
            // create and execute delete request
            // see https://developers.google.com/tasks/reference/rest/v1/tasklists/delete
            var res = await service.Tasklists.Delete(list.UID).ExecuteAsync();

            // check for success (empty return string)
            if (string.IsNullOrEmpty(res)) {
                return true;
            }

            return false;
        }

        public async TTasks.Task<bool> DeleteTask(Task task) {
            // create and execute delete request
            // see https://developers.google.com/tasks/reference/rest/v1/tasks/delete
            var res = await service.Tasks.Delete(task.ParentList.UID, task.UID).ExecuteAsync();

            // check for success (empty return string)
            if (res is string s && String.IsNullOrEmpty(s)) {

                if (task.Parent is not null)
                    task.Parent.Subtasks.Remove(task);

                return true;
            }
            return false;
        }
    }
}
