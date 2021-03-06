﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace GoogleCalendarTaskTracker
{
    public partial class TextEntryForm : Form
    {
        public TextEntryForm() : this(""){}
        public TextEntryForm(string title)
        {
            InitializeComponent();

            Text = title;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            //ok
            DialogResult = DialogResult.OK;
        }

        private void button2_Click(object sender, EventArgs e)
        {
            //cancel
            DialogResult = DialogResult.Cancel;
        }

        public string TextString
        {
            get
            {
                return textBox1.Text.Trim();
            }
        }

        private void TextEntryForm_Shown(object sender, EventArgs e)
        {
            button1.NotifyDefault(true);
        }
    }
}
