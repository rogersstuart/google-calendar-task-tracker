using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GoogleCalendarTaskTracker
{
    [Serializable]
    public class SuspendedTask : ActiveTask
    {
        DateTime break_stamp;

        public SuspendedTask(ActiveTask actitask, DateTime break_stamp) : base(actitask)
        {
            this.break_stamp = break_stamp;
        }

        public override string ToString()
        {
            return "Task: " + task + ", Type: " + type + ", Start Timestamp: " + start_time.ToString() + ", End Timestamp: " + break_stamp;
        }

        public DateTime EndTimeStamp
        {
            get
            {
                return break_stamp;
            }
        }
    }
}
