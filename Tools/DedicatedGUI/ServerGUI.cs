using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using FTW.Engine.Shared;
using System.Threading;
using System.IO;
using System.Reflection;

namespace DedicatedGUI
{
    public partial class ServerGUI : Form
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new ServerGUI());
        }

        ServerBase server;
        public ServerGUI()
        {
            InitializeComponent();

            TextWriter tw = new TextBoxStreamWriter(txtOutput);
            Console.SetOut(tw);
            Console.SetError(tw);

            server = ServerBase.CreateReflection();

            Config config = Config.ReadFile(settingsFilename);
            if (config == null)
            {
                config = server.CreateDefaultConfig();
                config.SaveToFile(settingsFilename);
            }

            server.Start(true, config);

            this.Text = server.Name;
        }

        const string settingsFilename = "server.yml";

        public class TextBoxStreamWriter : TextWriter
        {
            TextBox output;

            public TextBoxStreamWriter(TextBox output)
            {
                this.output = output;
            }

            private delegate void charDelegate(char c);
            public override void Write(char value)
            {
                if (output.InvokeRequired)
                {
                    output.BeginInvoke(new charDelegate(Write), value);
                    return;
                }
                if (!output.IsDisposed)
                    output.AppendText(value.ToString()); // When character data is written, append it to the text box.
            }

            private delegate void stringDelegate(string s);
            public override void Write(string value)
            {
                if (output.InvokeRequired)
                {
                    output.BeginInvoke(new stringDelegate(Write), value);
                    return;
                }
                if (!output.IsDisposed)
                    output.AppendText(value);
            }

            public override void WriteLine(string value)
            {
                if (output.InvokeRequired)
                {
                    output.BeginInvoke(new stringDelegate(WriteLine), value);
                    return;
                }
                if (!output.IsDisposed)
                    output.AppendText(value + Environment.NewLine);
            }

            public override Encoding Encoding
            {
                get { return Encoding.Unicode; }
            }
        }

        private void Form1_FormClosed(object sender, FormClosedEventArgs e)
        {
            if (server != null && server.IsRunning)
                server.Stop();
        }

        private void txtInput_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == (char)13)
            {
                if (txtInput.Text.Trim() != string.Empty)
                {
                    string cmd = txtInput.Text.Trim();
                    txtOutput.AppendText(cmd + Environment.NewLine);
                    server.HandleCommand(cmd);
                }
                txtInput.Clear();
                e.Handled = true;
            }
        }

        private void txtOutput_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (char.IsLetterOrDigit(e.KeyChar) || char.IsPunctuation(e.KeyChar) || char.IsSymbol(e.KeyChar) || e.KeyChar == ' ')
            {
                txtInput.Text += e.KeyChar;
                txtInput.Focus();
                txtInput.SelectionStart = txtInput.SelectionLength;
            }
        }
    }
}
