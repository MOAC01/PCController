using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Controller
{
    public partial class MainForm : Form
    {
        public static TcpListener server = null;
        public static TcpClient client = null;
        public static Thread thread;
        public string RemoteIP = null;
        int DefaultPort=48691;
        
        SynchronizationContext context = null;
        public MainForm()
        {
            InitializeComponent();
            context = SynchronizationContext.Current;
        }

        public void SetText(object view)
        {
            UI temp = (UI)view;
            string name = temp.GetName();
            string UpdateData = temp.GetUpdate();
            switch (name)
            {
                case "label":
                    int index = temp.GetIndex();
                    switch (index)
                    {
                        case 2:
                            label2.Text = UpdateData;
                          break;
                        case 6:
                          label6.Text = UpdateData;
                          if (temp.GetFlag() == true)
                              label6.ForeColor = Color.Green;
                          else label6.ForeColor = Color.Red;
                          break;
                    }
                    break;

                case "richtextbox":
                    richTextBox1.AppendText(UpdateData);
                    break;
                default: break;
                    
            }
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            LoadConfig();

        }

        public string GetIP()
        {
            string name = Dns.GetHostName();
            IPAddress[] ipadrlist = Dns.GetHostAddresses(name);
            foreach (IPAddress ipa in ipadrlist)
            {
                if (ipa.AddressFamily == AddressFamily.InterNetwork)
                    return ipa.ToString();
            }

            return "null";
        }

        public void LoadConfig()
        {
            numericUpDown1.Value = DefaultPort;

        }

       

        public  void Listen()
        {

            string strport = Console.ReadLine();
            try
            {
               
                IPEndPoint point = new IPEndPoint(IPAddress.Any, Convert.ToInt32(numericUpDown1.Value));
                server = new TcpListener(point);
                server.Start();
                Byte[] bytes = new Byte[256];
                string data = null;
               
                while (true)
                {
                   
                    string UpdateContent = "未连接";
                    UI ui = new UI("label", UpdateContent, 6,false);
                    context.Post(SetText, ui);
                    UI RichText = new UI();
                    RichText.SetName("richtextbox");
                    if (RemoteIP != null && RemoteIP !="")
                    {
                        RichText.SetUpdate(RemoteIP + "已断开" + " " + DateTime.Now.ToString() + "\n");
                        context.Post(SetText, RichText);
                    }
                    client = server.AcceptTcpClient();         //等待客户端连接
                    string ip = client.Client.RemoteEndPoint.ToString();
                    RemoteIP = ip;
                    
                    ui.SetUpdate("已连接:"+ip);
                    ui.SetFlag(true);
                    context.Post(SetText, ui);

                    RichText.SetUpdate(ip + "已连接" + " " + DateTime.Now.ToString() + "\n");
                    context.Post(SetText, RichText);
                    NetworkStream stream = client.GetStream();
                    int connState = 1;
                    stream.WriteByte((byte)connState);   //向连接的成功的客户端发送成功消息
                    int i;
                    UI other = new UI();
                    while (((i = stream.Read(bytes, 0, bytes.Length)) != 0))
                    {
                       
                        data = System.Text.Encoding.UTF8.GetString(bytes, 0, i);
                        other.SetName("richtextbox");
                        other.SetUpdate("客户端消息:" + data+" "+DateTime.Now.ToString()+"\n");
                        context.Post(SetText, other);
                        if (isShutdown(data))
                        {
                            string[] str = data.Split(',');      //分割字符串命令
                            int result = DoShutdown(str[0], str[1]);
                            if (result == 2 || result == 1)
                                stream.WriteByte((byte)result);
                        }
                        else
                        {
                            byte[] response = System.Text.Encoding.UTF8.GetBytes(getProcess());
                            int length = response.Length;
                            if (data.Equals("OK"))
                            {
                                stream.Write(response, 0, length);

                            }

                            else if (data.Equals("ready"))
                            {
                                string str = Convert.ToString(length);
                                byte[] size = System.Text.Encoding.UTF8.GetBytes(str);
                                stream.Write(size, 0, size.Length);
                            }

                            else
                            {

                                string str = Convert.ToString(length);
                                byte[] size = System.Text.Encoding.UTF8.GetBytes(str);
                                stream.WriteByte((byte)size.Length);
                            }

                        }

                    }

                }
            }
            catch (Exception e)
            {
                MessageBox.Show("程序运行过程中出现错误，详见运行日志", "错误",
                    MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                UI err = new UI("richtextbox",e.Message,0,true);
                context.Post(SetText, err);
            }

        }

    

        public static bool isShutdown(string command)
        {
            char[] temp = command.ToCharArray();
            for (int i = 0; i < temp.Length; i++)
                if (temp[i] == ',' || temp[i] == '，')
                    return true;
            return false;
        }



        public  int DoShutdown(string prefix, string suffix)
        {
            string ActionPrefix = "shutdown.exe";
            string ActionSuffix = null;
            int flag = 0;
            switch (prefix)
            {
                case "s":                        //关机
                    ActionSuffix = "-s -t ";
                    break;
                case "r":                       //重启
                    ActionSuffix = "-r -t ";
                    break;
                case "l":
                    ActionSuffix = "-l -t ";   //注销
                    break;

                case "k":                   //杀进程
                    flag = 1;

                    Process[] process = Process.GetProcessesByName(suffix);

                    if (process.Length > 0)
                    {
                        for (int i = 0; i < process.Length; i++)
                        {
                            try
                            {
                                process[i].Kill();   //杀死与进程名相关的进程
                            }
                            catch (Exception ex)
                            {
                                richTextBox1.AppendText(ex.Message + "\n");
                                flag = 2;
                                
                            }
                        }

                    }
                    else flag = 2;
                    break;


            }

            if (flag == 0)
            {
                ActionSuffix += suffix;
                Process.Start(ActionPrefix, ActionSuffix);
            }

            return flag;


        }

        public static string getProcess()
        {
            Process[] pro = Process.GetProcesses();//获取已开启的所有进程
            string xml = "<?xml version='1.0' encoding='UTF-8'?>" + "\n";
            string root = "<apps>" + "\n";
            string firstNode = "<app>" + "\n";
            string firstNodeEnd = "</app>" + "\n";
            string rootEnd = "</apps>";
            string appName = "<name>" + "\n";
            string appNameEnd = "</name>" + "\n";
            string processName = "<pname>" + "\n";
            string processNameEnd = "</pname>" + "\n";

            xml += root;
            for (int i = 0; i < pro.Length; i++)
            {

                if (pro[i].MainWindowTitle != "")
                {
                    xml += firstNode;
                    xml += appName + pro[i].MainWindowTitle + "\n" + appNameEnd;
                    xml += processName + pro[i].ProcessName + "\n" + processNameEnd;
                    xml += firstNodeEnd;
                }

            }

            xml += rootEnd;

            return xml;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            try
            {
                   thread = new Thread(Listen);
                   
                   thread.Start();
                   label5.Text = "已启动:" + GetIP() + ":" + numericUpDown1.Value;
                   label5.ForeColor = Color.Green;
                   button1.Enabled = false;
                    
            }
            catch (Exception ex)
            {
                MessageBox.Show("失败","启动失败，详情请见程序运行日志", 
                    MessageBoxButtons.OK,MessageBoxIcon.Exclamation);
                richTextBox1.AppendText(ex.Message+"\n");
                
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            richTextBox1.Clear();
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            e.Cancel = true;
            this.Hide();
        }

      

        private void 退出ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            System.Environment.Exit(0);
        }

        private void notifyIcon1_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            this.Show();
            this.WindowState = FormWindowState.Normal;
        }

        private void 关于AToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            About about = new About();
            about.Show();
        }

        private void 使用帮助HToolStripMenuItem_Click(object sender, EventArgs e)
        {
            System.Diagnostics.Process.Start("http://www.vegetapage.com/?p=91");
        }

      

     
    }
}
