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
using System.Net;
using System.Net.Sockets;
using System.IO;

namespace OnlineChat
{
    public partial class Login : Form
    {
        public Form1 main;
        private static int ServerPort = 9800;
        private string ServerIP = "127.0.0.1";//指定服务器IP
        public Socket clientSocket;
        public string message;
        private IPEndPoint ipeRemote1;
        private IPEndPoint epRemote1;
        private byte[] bytdata = new byte[1000];

        public Login()
        {
            InitializeComponent();
            epRemote1 = new IPEndPoint(IPAddress.Parse(ServerIP), ServerPort);//配置服务器端
            textBox1.Text = GetIpAddress();
        }

        private string GetIpAddress()
        {
            string hostName = Dns.GetHostName();   //获取本机名
            IPHostEntry localhost = Dns.GetHostByName(hostName);    //方法已过期，可以获取IPv4的地址
            IPAddress localaddr = localhost.AddressList[localhost.AddressList.Length-1];
            return localaddr.ToString();
        }

        public void button7_Click(object sender, EventArgs e)
        {
            clientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            //配置自己的网口
            main.myself.ip = textBox1.Text.ToString();
            main.myself.name = textBox5.Text.ToString();
            message = textBox1.Text.ToString() + "/" + textBox5.Text.ToString();

            //链接服务器
            try
            {
                clientSocket.Connect(epRemote1);
            }
            catch(SocketException ie)
            {
                MessageBox.Show("unable to connect to server:" + ie.ToString() );
            }

            clientSocket.Send(Encoding.UTF8.GetBytes(message));
            Thread myThread = new Thread(ListenServer);//监听线程
            myThread.IsBackground = true;
            myThread.Start();
            this.Hide();//隐藏登陆界面
        }

        private void ListenServer()
        {
            while (true)
            {
                String info = Receive(clientSocket);       // 接收客户端发送的信息 
                GetMessage(info);
             }
         }

        private string Receive(Socket socket)
        {
            string data = "";
            byte[] bytes = null;
            int len = socket.Available;
            if (len > 0)
            {
                bytes = new byte[1024];
                int receiveNumber = socket.Receive(bytes);
                data = Encoding.UTF8.GetString(bytes, 0, receiveNumber);
            }
            return data;
        }

        private static int GetIPNumer(string info)
        {
            int number = 0;
            while (info[number] != '/')
            {
                number++;
            }
            return number;
        }

        private static int GetName(int bigin, string info)
        {
            int number = bigin;
            while (info[number] != '\0')
            {
                number++;
            }
            return number - bigin;
        }

        private void label2_Click(object sender, EventArgs e)
        {

        }

        private void dealMessage(string info)//添加好友
        {
            int number = GetIPNumer(info);
            string IP = info.Substring(0, number);
            string name = info.Substring(number + 1);
            friend member = new friend(IP, name);
            int judge = 0;
            if (main.Friends.Count != 0)
            {
                foreach (friend mem in main.Friends)
                {
                    if (mem.name == member.name)
                    {
                        judge = 1;
                    }
                }
                if (judge == 0)
                {
                    main.Friends.Add(member);
                }
            }
            else
            {
                main.Friends.Add(member);
            }
        }

        private void GetMessage(string info)
        {
            if (!info.Equals(""))
            {
                string temp = "";
                for(int i = 0 ; i < info.Length ; i++)
                {
                    if(info[i] == ' ')
                    {
                        dealMessage(temp);
                        temp = "";
                    }
                    else
                    {
                        temp += info[i];
                    }
                }
            }
        }
    }
}
