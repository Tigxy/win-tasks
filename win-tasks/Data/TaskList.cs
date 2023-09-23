using System;
using System.Collections.Generic;
using System.Linq;

namespace win_tasks.data {
    public class TaskList {
        public string UID { get; internal set; }
        public string Title { get; set; }
        public List<Task> Tasks { get; set; } = new();
        public DateTime? ModificationDate { get; set; }

        private object underlyingObject;
        public T GetUnderlyingTasklist<T>() => (T)underlyingObject;
        public void SetUnderlyingTasklist<T>(T task) => underlyingObject = task;
        public IEnumerable<Task> GetAllSubtasks() {
            return Tasks.Concat(Tasks.SelectMany(t => t.GetAllSubtasks()));
        }
        public Task AddTask(Task task) => AddTask(task, Tasks.LastOrDefault());

        public Task AddTask(Task task, Task? previous) {
            task.ParentList = this;
            if (previous is null)
                Tasks.Add(task);
            else 
                Tasks.Insert(Tasks.IndexOf(task) + 1, task);
            return task;
        }
    }
}
