using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Threading;

namespace GoogleCalendarTaskTracker
{
    public partial class TaskDurationDisplayForm : Form
    {
        private ActiveTask source_task;

        public TaskDurationDisplayForm(ActiveTask source_task)
        {
            InitializeComponent();

            this.source_task = source_task;
        }

        public static Form ShowDuration(SuspendedTask susptask)
        {
            var dispform = new TaskDurationDisplayForm(susptask);
            dispform.Show();
            dispform.label1.Text = (susptask.EndTimeStamp - susptask.StartTimeStamp).ToString();

            return dispform;
        }

        public static Form ShowDuration(ActiveTask actitask)
        {
            var dispform = new TaskDurationDisplayForm(actitask);
            dispform.Show();

            try
            {
                Task.Run(async () =>
                {
                    while (true)
                    {
                        dispform.Invoke((MethodInvoker)delegate
                        {
                            dispform.label1.Text = (DateTime.Now - actitask.StartTimeStamp).ToString();
                        });

                        await Task.Delay(1000/50);
                    }
                });
            }
            catch(Exception ex)
            {
                //no worries mate
            }

            return dispform;
        }

        public ActiveTask SourceTask
        {
            get
            {
                return source_task;
            }
        }

        private void TaskDurationDisplayForm_Load(object sender, EventArgs e)
        {

        }
    }
}
