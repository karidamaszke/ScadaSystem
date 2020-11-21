
namespace ScadaProject
{
    public enum TaskType
    {
        Read,
        Write
    }

    public class Task
    {
        public TaskType type;
        public int index;
        public int value;

        public Task(TaskType type, int index = 0, int value = 0)
        {
            this.type = type;
            this.index = index;
            this.value = value;
        }
    }
}
