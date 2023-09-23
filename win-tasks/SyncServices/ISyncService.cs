using win_tasks.data;
using TTasks = System.Threading.Tasks;

namespace win_tasks.SyncServices {
    public interface ISyncService {
        public TTasks.Task<TaskList[]> GetLists();
        public TTasks.Task<Task[]> GetTasks();

        public TTasks.Task<TaskList> UpdateList(TaskList list);
        public TTasks.Task<Task> UpdateTask(Task task);

        public TTasks.Task<TaskList> AddList(TaskList list);
        public TTasks.Task<Task> AddTask(Task task);
        public TTasks.Task<Task> AddTask(Task task, Task? parent);
        public TTasks.Task<Task> AddTask(Task task, Task? parent, Task? previous);
        public TTasks.Task<Task> AddTask(Task task, Task? parent, TaskList list);
        public TTasks.Task<Task> AddTask(Task task, Task? parent, Task? previous, TaskList list);
        
        public TTasks.Task<Task> Move(Task task, Task? parent);
        public TTasks.Task<Task> Move(Task task, Task? parent, Task? previous);
        public TTasks.Task<Task> Move(Task task, Task? parent, TaskList list);
        public TTasks.Task<Task> Move(Task task, Task? parent, Task? previous, TaskList list);

        public TTasks.Task<bool> DeleteList(TaskList list);
        public TTasks.Task<bool> DeleteTask(Task task);
    }
}
