using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GoogleCalendarTaskTracker
{
    [Serializable]
    public class ActiveTask
    {
        internal string task;
        internal string type;
        internal DateTime start_time;
        internal string[] notes_lines;

        public ActiveTask(string task, string type, DateTime start_time, string[] notes_lines)
        {
            this.task = task;
            this.type = type;
            this.start_time = start_time;
            this.notes_lines = notes_lines;
        }

        public ActiveTask(ActiveTask actitask) : this(actitask.Task, actitask.Type, actitask.StartTimeStamp, actitask.Notes){ }

        public override string ToString()
        {
            return "Task: " + task + ", Type: " + type + ", Timestamp: " + start_time.ToString();
        }

        public string Task
        {
            get
            {
                return task;
            }
        }

        public string Type
        {
            get
            {
                return type;
            }
        }

        public DateTime StartTimeStamp
        {
            get
            {
                return start_time;
            }
        }

        public string[] Notes
        {
            get
            {
                return notes_lines;
            }
        }
    }
}
