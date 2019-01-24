using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using System.Threading;


namespace GoogleCalendarTaskTracker
{
    public partial class Form1 : Form
    {
        private List<Form> tdd_forms = new List<Form>();

        public Form1()
        {
            InitializeComponent();

            PopulateTasksFromFile();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            //begin
            if (listView1.SelectedIndices.Count > 0 && listView2.SelectedIndices.Count > 0)
            {
                ActiveTaskManager.WholeTasks.Add(new ActiveTask(listView1.SelectedItems[0].Text,
                                                           listView2.SelectedItems[0].Text,
                                                           DateTime.Now,
                                                           textBox1.Lines));
                ConfigurationManager.SaveToFile();

                listView2.Clear();
                textBox1.Clear();
            }

            foreach (ListViewItem lvi in listView1.SelectedItems)
                lvi.Selected = false;

            button1.Enabled = false;

            listView1.Focus();
        }

        private void PopulateTasksFromFile()
        {
            listView1.Clear();

            string[] lines = File.ReadAllLines("tasks.txt");
            foreach(string line in lines)
            {
                string[] split_line = line.Split('{');
                listView1.Items.Add(split_line[0]);
            }

            listView1.AutoResizeColumns(ColumnHeaderAutoResizeStyle.ColumnContent);
        }

        private void PopulateTaskTypesFromFile(string task)
        {
            listView2.Clear();

            string[] lines = File.ReadAllLines("tasks.txt");
            foreach(string line in lines)
            {
                string[] split_line = line.Split('{');
                if (split_line[0] == task)
                {
                    string[] types = split_line[1].Substring(0, split_line[1].Length-1).Split(',');
                    foreach (string type in types)
                        listView2.Items.Add(new ListViewItem(type));
                }
            }

            listView2.AutoResizeColumns(ColumnHeaderAutoResizeStyle.ColumnContent);
        }

        //the task selection has changed
        private void listView1_ItemSelectionChanged(object sender, ListViewItemSelectionChangedEventArgs e)
        {
            if (!e.IsSelected)
                listView2.Clear();
            else
                PopulateTaskTypesFromFile(e.Item.Text);
        }

        //a different tab has been selected; update the contents of the view tabs
        //this should be reworked in a way that only causes the visible tab to be updated
        private void tabControl1_SelectedIndexChanged(object sender, EventArgs e)
        {
            listBox1.Items.Clear();
            listBox1.Items.AddRange(ActiveTaskManager.WholeTasks.ToArray());

            listBox2.Items.Clear();
            listBox2.Items.AddRange(ActiveTaskManager.SuspendedTasks.ToArray());

            RefreshButtons();
        }

        //finalize; submit the activetask to google calendar
        private async void button2_Click(object sender, EventArgs e)
        {
            int index_counter = 0;

            if (listBox1.SelectedIndex > -1)
            {
                UseWaitCursor = true;

                Control[] controls = Controls.Cast<Control>().ToArray();
                bool[] control_state = new bool[controls.Length];

                for (index_counter = 0; index_counter < controls.Length; index_counter++)
                {
                    control_state[index_counter] = controls[index_counter].Enabled;
                    controls[index_counter].Enabled = false;
                }

                int[] selected_indicies = listBox1.SelectedIndices.Cast<int>().Reverse().ToArray();
                ActiveTask[] selected_items = listBox1.SelectedItems.Cast<ActiveTask>().Reverse().ToArray();

                index_counter = 0;
                foreach (ActiveTask selected_item in selected_items)
                {
                    List<ActiveTask> to_process = new List<ActiveTask>();

                    int selected_index = selected_indicies[index_counter];
                    string line_string = selected_item.ToString();

                    //if the current line contains merged tasks then split it before upload
                    if (IsMerged(selected_item.ToString()))
                        to_process.AddRange(SplitActiveTaskLine(line_string));
                    else
                        to_process.Add(selected_item);

                    foreach (ActiveTask current_task in to_process)
                    {
                        int timeout = 10000;
                        var task = new Task(delegate { GoogleCalendarManager.CompleteTaskAsync(current_task); });
                        task.Start();

                        if (await Task.WhenAny(task, Task.Delay(timeout)) != task)
                        {
                            MessageBox.Show(this, "Timeout. The requested operation was not completed.");
                            return;
                        }
                    }

                    ActiveTaskManager.WholeTasks.RemoveAt(selected_index);

                    index_counter++;
                }

                ConfigurationManager.SaveToFile();

                listBox1.Items.Clear();
                listBox1.Items.AddRange(ActiveTaskManager.WholeTasks.ToArray());

                ValidateDisplayedDialogs(); //ughhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhh so much repition

                for (index_counter = 0; index_counter < controls.Length; index_counter++)
                    controls[index_counter].Enabled = control_state[index_counter];

                UseWaitCursor = false;
            }
        }

        private void button3_Click(object sender, EventArgs e)
        {
            //remove list1
            if (listBox1.SelectedIndex > -1)
            {
                int selected_index = listBox1.SelectedIndex;

                listBox1.Items.RemoveAt(selected_index);
                ActiveTaskManager.WholeTasks.RemoveAt(selected_index);

                ConfigurationManager.SaveToFile();
            }

            ValidateDisplayedDialogs(); //ughhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhh
        }

        private void button4_Click(object sender, EventArgs e)
        { 
            //suspended task
            if(listBox1.SelectedIndex > -1)
            {
                int selected_index = listBox1.SelectedIndex;

                ActiveTask actitask = (ActiveTask)listBox1.SelectedItem;

                listBox1.Items.RemoveAt(selected_index);
                ActiveTaskManager.WholeTasks.RemoveAt(selected_index);

                ActiveTaskManager.SuspendedTasks.Add(new SuspendedTask(actitask, DateTime.Now));

                ConfigurationManager.SaveToFile();
            }

            ValidateDisplayedDialogs(); //ughhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhh
        }

        private void button5_Click(object sender, EventArgs e)
        {
            //join or split suspended tasks
            if (listBox2.SelectedIndices.Count > 1)
            {
                if (button5.Text == "Merge")
                {
                    int[] selected_indicies = listBox2.SelectedIndices.Cast<int>().ToArray();
                    SuspendedTask[] selected_tasks = listBox2.SelectedItems.Cast<SuspendedTask>().ToArray();

                    foreach (int index in selected_indicies.Reverse())
                    {
                        ActiveTaskManager.SuspendedTasks.RemoveAt(index);
                        listBox2.Items.RemoveAt(index);
                    }

                    string tasks = "{";
                    string types = "{";
                    List<string> notes_merge = new List<string>();

                    DateTime earliest = selected_tasks[0].StartTimeStamp;
                    DateTime latest = selected_tasks[0].EndTimeStamp;

                    foreach (SuspendedTask suspended_task in selected_tasks)
                    {
                        tasks += suspended_task.Task + ",";
                        types += suspended_task.Type + ",";

                        notes_merge.AddRange(suspended_task.Notes);
                        notes_merge.Add("");

                        if (suspended_task.StartTimeStamp < earliest)
                            earliest = suspended_task.StartTimeStamp;

                        if (suspended_task.EndTimeStamp > latest)
                            latest = suspended_task.EndTimeStamp;
                    }

                    tasks += "}";
                    types += "}";

                    SuspendedTask spt = new SuspendedTask(new ActiveTask(tasks, types, earliest, notes_merge.ToArray<string>()), latest);

                    ActiveTaskManager.SuspendedTasks.Add(spt);

                    ConfigurationManager.SaveToFile();

                    listBox2.Items.Add(spt);
                }
            }
            else
                if(listBox2.SelectedIndices.Count > 0)
                {
                    if (button5.Text == "Split")
                    {
                        SuspendedTask[] selected_tasks = listBox2.SelectedItems.Cast<SuspendedTask>().ToArray();
                        int[] selected_indicies = listBox2.SelectedIndices.Cast<int>().ToArray();

                        int list_index = 0;
                        foreach (string to_string in selected_tasks.Select(x => x.ToString()))
                        {

                            string line_text = to_string;

                            //"Task: " + task + ", Type: " + type + ", Start Timestamp: " + start_time.ToString() + " End Timestamp: " + break_stamp;
                            string[] task_headers = new string[] {"Task:", "Type:"};
                            string[] time_headers = new string[] {"Start Timestamp:", "End Timestamp:"};
                            string header_termination = ":";

                            List<string> tasks = new List<string>();
                            List<string> types = new List<string>();
                            DateTime start_time;
                            DateTime end_time;

                            foreach (string header in task_headers)
                            {
                                int start_index = line_text.IndexOf('{');
                                int end_index = line_text.IndexOf('}');

                                string value = line_text.Substring(start_index+1, (end_index-start_index)-1);
                                line_text = line_text.Substring(end_index + 1);
                            Console.WriteLine(value);
                            foreach (string stb in value.Split(','))
                                Console.WriteLine(stb + ",");

                            string[] split_types = value.Split(',');
                            split_types = split_types.Take(split_types.ToArray().Count()-1).ToArray();

                                if (header == task_headers[0])
                                    tasks.AddRange(split_types);
                                else
                                    if (header == task_headers[1])
                                    types.AddRange(split_types);
                            }

                            string[] split_times = line_text.Split(',');

                            foreach (string str in split_times)
                            Console.Write(str + " ");

                            start_time = DateTime.Parse(split_times[1].Replace("Start Timestamp:", "").Trim());
                            end_time = DateTime.Parse(split_times[2].Replace("End Timestamp:", "").Trim());

                            ActiveTaskManager.SuspendedTasks.RemoveAt(selected_indicies[list_index]);

                            for (int index_counter = 0; index_counter < tasks.Count; index_counter++)
                                    ActiveTaskManager.SuspendedTasks.Add(new SuspendedTask(new ActiveTask(tasks[index_counter], types[index_counter], start_time, new string[] { }), end_time));

                            list_index++;
                        }

                        listBox2.Items.Clear();
                        listBox2.Items.AddRange(ActiveTaskManager.SuspendedTasks.ToArray());

                        ConfigurationManager.SaveToFile();
                    }
                }

            RefreshButtons();
            ValidateDisplayedDialogs(); //ughhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhh
        }

        private async void button7_Click(object sender, EventArgs e)
        {
            //finalize
            int selected_index = listBox2.SelectedIndex;
            ActiveTask selected_item = (SuspendedTask)listBox2.SelectedItem;

            UseWaitCursor = true;

            Control[] controls = Controls.Cast<Control>().ToArray();
            bool[] control_state = new bool[controls.Length];
            for (int index_counter = 0; index_counter < controls.Length; index_counter++)
            {
                control_state[index_counter] = controls[index_counter].Enabled;
                controls[index_counter].Enabled = false;
            }

            if (selected_index > -1)
            {
                int timeout = 10000;
                var task = new Task(delegate { GoogleCalendarManager.CompleteTaskAsync(selected_item); });
                task.Start();
                if (await Task.WhenAny(task, Task.Delay(timeout)) != task)
                    Invoke((MethodInvoker)delegate { MessageBox.Show(this, "Timeout. The requested operation was not completed."); });
                else
                {
                    ActiveTaskManager.SuspendedTasks.RemoveAt(selected_index);
                    ConfigurationManager.SaveToFile();

                    listBox2.Items.Clear();
                    listBox2.Items.AddRange(ActiveTaskManager.SuspendedTasks.ToArray());

                    ValidateDisplayedDialogs(); //ughhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhh so much repition
                }
            }

            for (int index_counter = 0; index_counter < controls.Length; index_counter++)
                controls[index_counter].Enabled = control_state[index_counter];

            UseWaitCursor = false;
        }

        private void button6_Click(object sender, EventArgs e)
        {
            //remove list2
            if (listBox2.SelectedIndex > -1)
            {
                int selected_index = listBox2.SelectedIndex;

                listBox2.Items.RemoveAt(selected_index);
                ActiveTaskManager.SuspendedTasks.RemoveAt(selected_index);

                ConfigurationManager.SaveToFile();
            }

            ValidateDisplayedDialogs(); //ughhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhh sm so bd
        }

        private void button8_Click(object sender, EventArgs e)
        {
            //pop suspended task (return to whole task)
            if(listBox2.SelectedIndices.Count > 0)
            {
                int[] selected_indicies = listBox2.SelectedIndices.Cast<int>().ToArray();
                SuspendedTask[] selected_tasks = listBox2.SelectedItems.Cast<SuspendedTask>().ToArray();

                foreach (int index in selected_indicies.Reverse())
                {
                    ActiveTaskManager.SuspendedTasks.RemoveAt(index);
                    listBox2.Items.RemoveAt(index);
                }

                ActiveTaskManager.WholeTasks.AddRange(new Func<ActiveTask[]>(() =>
                {
                    ActiveTask[] actitasks = new ActiveTask[selected_tasks.Count()];
                    for (int task_counter = 0; task_counter < selected_tasks.Count(); task_counter++)
                        actitasks[task_counter] = new ActiveTask(selected_tasks[task_counter]);
                    return actitasks;
                }).Invoke());

                ConfigurationManager.SaveToFile();
            }

            ValidateDisplayedDialogs(); //ughhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhh
        }

        private void listBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            RefreshButtons();
        }

        private void RefreshButtons()
        {
            Button[] bs = new Button[] { button2, button3, button4 };

            if (listBox1.SelectedIndices.Count > 0)
                foreach (Button b in bs)
                    b.Enabled = true;
            else
                foreach (Button b in bs)
                    b.Enabled = false;

            bs = new Button[] {button6, button7, button8};

            if (listBox2.SelectedIndices.Count > 0)
                foreach (Button b in bs)
                    b.Enabled = true;
            else
                foreach (Button b in bs)
                    b.Enabled = false;

            if (listBox2.SelectedIndices.Count > (button5.Text == "Merge" ? 1 : (button5.Text == "Split" ? 0 : 0)))
                button5.Enabled = true;
            else
                button5.Enabled = false;
        }

        private void listBox1_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            if (listBox1.SelectedItems.Count > 0)
            {
                var senderList = (ListBox)sender;
                var clickedItem = senderList.SelectedItems[0];
                if (clickedItem != null)
                {
                    tdd_forms.Add(TaskDurationDisplayForm.ShowDuration((ActiveTask)clickedItem));
                }
            }
        }

        private void listBox2_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            if (listBox2.SelectedItems.Count > 0)
            {
                var senderList = (ListBox)sender;
                var clickedItem = senderList.SelectedItems[0];
                if (clickedItem != null)
                {
                    tdd_forms.Add(TaskDurationDisplayForm.ShowDuration((SuspendedTask)clickedItem));
                }
            }
        }

        private void listBox2_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (listBox2.SelectedIndices.Count > 0)
            {
                bool split_found = false;
                foreach (string line in listBox2.SelectedItems.Cast<SuspendedTask>().Select(x => x.ToString()))
                {
                    if (line.IndexOf('{') > -1)
                        if (line.IndexOf('}') > line.IndexOf('{'))
                        {
                            split_found = true;
                            button5.Text = "Split";
                        }
                }

                if (!split_found)
                    button5.Text = "Merge";
            }

            RefreshButtons();
        }

        private void ValidateDisplayedDialogs()
        {
            foreach (Form f in tdd_forms)
                if (!listBox1.Items.Contains(((TaskDurationDisplayForm)f).SourceTask) &&
                    !listBox2.Items.Cast<ActiveTask>().Contains(((TaskDurationDisplayForm)f).SourceTask))
                    f.Dispose();
                    
        }

        private void credentialManagementToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog ofd = new OpenFileDialog())
            {
                ofd.InitialDirectory = AppDomain.CurrentDomain.BaseDirectory;
                if (ofd.ShowDialog() == DialogResult.OK)
                    ConfigurationManager.CredentialFilePath = ofd.FileName;
            }
        }

        private void clearCredentialCacheToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if(Directory.Exists(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + "/.credentials/calendar-dotnet-quickstart"))
                Directory.Delete(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + "/.credentials/calendar-dotnet-quickstart", true);
        }

        private void nameToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //change calendar name
            using (TextEntryForm tef = new TextEntryForm("Enter Calendar Name"))
                if (tef.ShowDialog() == DialogResult.OK)
                    GoogleCalendarManager.CalendarName = tef.TextString;
        }

        private void defaultToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //return calendar name to defualt
            GoogleCalendarManager.CalendarName = GoogleCalendarManager.DefaultCalendarName;
        }

        private bool IsMerged(string line)
        {
            if (line.IndexOf('{') > -1)
                if (line.IndexOf('}') > line.IndexOf('{'))
                    return true;

            return false;
        }

        private ActiveTask[] SplitActiveTaskLine(string line_text)
        {
            List<ActiveTask> active_tasks = new List<ActiveTask>();

            //"Task: " + task + ", Type: " + type + ", Start Timestamp: " + start_time.ToString() + " End Timestamp: " + break_stamp;
            string[] task_headers = new string[] { "Task", "Type" };
            string[] time_headers = new string[] { "Start Timestamp", "End Timestamp" };
            string header_termination = ":";

            List<string> tasks = new List<string>();
            List<string> types = new List<string>();
            DateTime start_time;

            foreach (string header in task_headers)
            {
                int start_index = line_text.IndexOf('{');
                int end_index = line_text.IndexOf('}');

                string value = line_text.Substring(start_index + 1, (end_index - start_index) - 1);
                line_text = line_text.Substring(end_index + 1);
                Console.WriteLine(value);
                foreach (string stb in value.Split(','))
                    Console.WriteLine(stb + ",");

                string[] split_types = value.Split(',');
                split_types = split_types.Take(split_types.ToArray().Count()-1).ToArray();

                if (header == task_headers[0])
                    tasks.AddRange(split_types);
                else
                    if (header == task_headers[1])
                    types.AddRange(split_types);
            }

            string[] split_times = line_text.Split(',');

            foreach (string str in split_times)
                Console.Write(str + " ");

            start_time = DateTime.Parse(split_times[1].Replace("Timestamp:", "").Trim());

            for (int index_counter = 0; index_counter < task_headers.Count(); index_counter++)
                active_tasks.Add(new ActiveTask(tasks[index_counter], types[index_counter], start_time, new string[] { }));

            return active_tasks.ToArray();
        }

        private void listView2_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (listView2.SelectedIndices.Count > 0)
                button1.Enabled = true;
            else
                button1.Enabled = false;
        }

        private void nameToolStripMenuItem_DropDownOpening(object sender, EventArgs e)
        {
            //get calendar list and show them

            try
            {
                nameToolStripMenuItem.DropDownItems.Clear();

                nameToolStripMenuItem.DropDownItems.Add(new Func<ToolStripMenuItem>(() =>
                {
                    ToolStripMenuItem tsmi = new ToolStripMenuItem("primary");
                    tsmi.Click += (o, i) =>
                    {
                        GoogleCalendarManager.CalendarName = GoogleCalendarManager.DefaultCalendarName;
                    };

                    return tsmi;
                }).Invoke());

                foreach (string item in GoogleCalendarManager.CalendarNames)
                {
                    ToolStripMenuItem nstm = new ToolStripMenuItem(item);

                    nstm.Click += (o, i) =>
                    {
                        GoogleCalendarManager.CalendarName = ((ToolStripMenuItem)o).Text;
                        MessageBox.Show(GoogleCalendarManager.CalendarName);
                    };

                    nameToolStripMenuItem.DropDownItems.Add(nstm);
                }

                foreach (ToolStripMenuItem item in nameToolStripMenuItem.DropDownItems.Cast<ToolStripMenuItem>())
                    if (item.Text == GoogleCalendarManager.CalendarName)
                        item.Checked = true;
            }
            catch(Exception ex)
            {
                MessageBox.Show(this, "The operation timed out.");
            }
        }

        private void detailsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            string message_string = "";
            if(File.Exists(ConfigurationManager.CredentialFilePath))
                message_string += "The selected credtential file is located at: ";
            else
                message_string += "The selected credential file is invalid but supposedly located at: ";

            message_string += new FileInfo(ConfigurationManager.CredentialFilePath).FullName;

            message_string += Environment.NewLine;

            message_string += !File.Exists(ConfigurationManager.CredentialFilePath) ? "The Credential File Doesn't Exist" : "The Credential File Exists";

            message_string += Environment.NewLine;

            message_string += "The Cache File Is Located At: ";

            message_string += Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + "/.credentials/calendar-dotnet-quickstart";

            message_string += Environment.NewLine;

            message_string += Directory.Exists(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + "/.credentials/calendar-dotnet-quickstart") ? "The cache directory exists." : "The cache directory doesn't exist";

            MessageBox.Show(this, message_string);
        }

        //show the configuration dialog
        private void configurationToolStripMenuItem_Click(object sender, EventArgs e)
        {

        }

        private void tasksAndTypesToolStripMenuItem_Click_1(object sender, EventArgs e)
        {
            using (TasksAndTypesDialog ttd = new TasksAndTypesDialog())
            {
                ttd.Lines.Clear();
                ttd.Lines = new List<string>(File.ReadAllLines(ConfigurationManager.TasksAndTypesFilePath));
                if (ttd.ShowDialog() == DialogResult.OK)
                    PopulateTasksFromFile();
            }
        }
    }
}
