using System;

namespace win_tasks.Data {
    internal struct TaskFilter {
        public bool loadCompleted;
        public DateTime? dueMin;
        public DateTime? dueMax;
        public DateTime? completedMin;
        public DateTime? completedMax;
        public DateTime? updatedMin;

        public TaskFilter() : this(true) { }
        public TaskFilter(bool loadCompleted) : this(loadCompleted, null, null, null, null, null) { }
        public TaskFilter(bool loadCompleted, DateTime? dueMin, DateTime? dueMax, DateTime? completedMin, DateTime? completedMax, DateTime? updatedMin) {
            this.loadCompleted = loadCompleted;
            this.dueMin = dueMin;
            this.dueMax = dueMax;
            this.completedMin = completedMin;
            this.completedMax = completedMax;
            this.updatedMin = updatedMin;
        }
    }
}
