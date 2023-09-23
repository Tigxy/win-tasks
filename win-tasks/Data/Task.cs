using System;
using System.Collections.Generic;
using System.Linq;

namespace win_tasks.data {
    public class Task {
        // see fields here that we might want to support: https://tasks.org/docs/sync
        public string UID { get; internal set; }
        public string Title { get; set; }
        public string Description { get; set; } = string.Empty;
        internal Task? Parent { get; set; }
        internal Task? PreviousTask {
            get {
                // provide additional parameters
                int? taskIdx = Parent?.Subtasks.IndexOf(this);
                Task? previous = (taskIdx is null || taskIdx == 0) ? null : Parent?.Subtasks[(int)taskIdx - 1];
                return previous;
            }
        }

        internal TaskList ParentList { get; set; }
        public List<Task> Subtasks { get; set; } = new();
        public DateTime? DueDate { get; set; }
        public DateTime? CreationDate { get; set; }
        public DateTime? ModificationDate { get; set; }
        public DateTime? CompletionDate { get; set; }
        public Priority Priority { get; set; }
        public string? Location { get; set; }
        public DateTime? Reminder { get; set; }
        public DateTime? Recurrence { get; set; }
        public int Position { get; set; }
        public bool IsCompleted { get; set; }
        public bool IsModified { get; set; }

        private object underlyingObject = null!;
        public T GetUnderlyingTask<T>() => (T)underlyingObject;
        public void SetUnderlyingTask<T>(T task) => underlyingObject = task!;
        public IEnumerable<Task> GetAllSubtasks() => Subtasks.Concat(Subtasks.SelectMany(t => t.GetAllSubtasks()));

        public void SetModified() {
            ModificationDate = DateTime.UtcNow;
        }

        public void SetNotCompleted() {
            CompletionDate = null;
            IsCompleted = false;
        }

        public void SetCompleted() {
            CompletionDate = DateTime.UtcNow;
            IsCompleted = true;
        }

        public void SetCreationDate() => CreationDate = DateTime.UtcNow;

    }
}
