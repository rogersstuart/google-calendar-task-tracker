using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;

namespace GoogleCalendarTaskTracker
{
    static class ActiveTaskManager
    {
        private static readonly string filename = "active_tasks.bin";

        private static List<ActiveTask> whole_tasks = new List<ActiveTask>();
        private static List<SuspendedTask> suspended_tasks = new List<SuspendedTask>();

        public static List<ActiveTask> WholeTasks
        {
            get
            {
                return whole_tasks;
            }
        }

        public static List<SuspendedTask> SuspendedTasks
        {
            get
            {
                return suspended_tasks;
            }
        }
    }
}
