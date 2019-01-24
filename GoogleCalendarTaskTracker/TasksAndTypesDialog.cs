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

namespace GoogleCalendarTaskTracker
{
    public partial class TasksAndTypesDialog : Form
    {
        List<string> lines = new List<string>();

        public TasksAndTypesDialog()
        {
            InitializeComponent();
        }

        private void DisplayTasks()
        {
            dataGridView1.Rows.Clear();
            dataGridView2.Rows.Clear();

            foreach (string line in lines)
            {
                string[] split_line = line.Split('{');
                dataGridView1.Rows.Add(split_line[0].Trim());
            }
        }

        private string GetTaskAtIndex(int index)
        {
            if (index < lines.Count)
            {
                string[] split_line = lines[index].Split('{');
                return split_line[0].Trim();
            }
            else
                if (dataGridView1.Rows.Count > index)
                {
                    lines.Add((string)dataGridView1.Rows[index].Cells[0].Value + "{}");
                    return (string)dataGridView1.Rows[index].Cells[0].Value;
                }
                else
                    return null;
        }

        private int GetTaskLineIndex(string task)
        {
            
            for (int line_counter = 0; line_counter < lines.Count; line_counter++)
            {
                string[] split_line = lines[line_counter].Split('{');
                if (split_line[0].Trim() == task)
                    return line_counter;
            }

            return -1;
        }

        public void DisplayTypes(string task)
        {
            
            dataGridView2.Rows.Clear();

            foreach (string line in lines)
            {
                string[] split_line = line.Split('{');
                if (split_line[0].Trim() == task)
                {
                    string[] types = split_line[1].Substring(0, split_line[1].Length - 1).Split(',');
                    foreach (string type in types)
                        if(type.Length > 0)
                            dataGridView2.Rows.Add(type.Trim());
                }
            }
        }

        public List<string> Lines
        {
            get
            {
                return lines;
            }

            set
            {
                lines = value;

                DisplayTasks();
            }
        }
        

        private void dataGridView1_SelectionChanged(object sender, EventArgs e)
        {
           DisplayTypes((string)dataGridView1.SelectedCells[0].Value);
        }

        private void dataGridView1_CellEndEdit(object sender, DataGridViewCellEventArgs e)
        {
           GenerateLineFromView(GetTaskAtIndex(e.RowIndex));
        }

        private void dataGridView2_CellEndEdit(object sender, DataGridViewCellEventArgs e)
        {
            GenerateLineFromView((string)dataGridView1.SelectedCells[0].Value);
        }

        private void GenerateLineFromView(string source_task)
        {
            List<string> displayed_types = new List<string>();
            foreach (DataGridViewRow row in dataGridView2.Rows)
                foreach (DataGridViewCell cell in row.Cells)
                    displayed_types.Add((string)cell.Value);

            int line_index = GetTaskLineIndex(source_task);
            string new_line = "";

            if (line_index > -1)
            {
                new_line += dataGridView1.Rows[line_index].Cells[0].Value;
                new_line += "{";
                foreach (string type in displayed_types)
                    if(type != displayed_types.Last<string>())
                        new_line += type + ",";
                new_line += "}";

                lines[line_index] = new_line;
            }
        }

        //okay button
        private void button1_Click(object sender, EventArgs e)
        {
            File.WriteAllLines(ConfigurationManager.TasksAndTypesFilePath, lines);
            DialogResult = DialogResult.OK;
        }

        private void button2_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
        }
    }
}
