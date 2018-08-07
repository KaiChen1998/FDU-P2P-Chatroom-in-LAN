using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Net.Sockets;
using System.Net;
using System.Threading;

namespace p2pServer
{
    public partial class Form1 : Form
    {
        private static byte[] result = new byte[1024];
        private static int ServerPort = 9800;
        private static Socket serverSocket;
        public  List<ChatMemer> onlineMembers = new List<ChatMemer>();
        //public Dictionary<string, Socket> clients = new Dictionary<string, Socket>();
        public List<Socket> clients = new List<Socket>();
        public Thread receiveThread;
        public Socket clientSocket;
       // private string clientIp = "";

        public Form1()
        {
            InitializeComponent();
            this.Text = "Server";
            serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            serverSocket.Bind(new IPEndPoint(IPAddress.Any, ServerPort));  //绑定IP地址：端口  
            serverSocket.Listen(10);    //设定最多10个排队连接请求  
           
            //通过Clientsoket发送数据  
            Thread myThread = new Thread(ListenClientConnect);
            myThread.IsBackground = true;
            myThread.Start();            
            Init();
        }

        private void ListenClientConnect()
        {
            while (true)
            {
                clientSocket = serverSocket.Accept();
                receiveThread = new Thread(ReceiveMessage);
                receiveThread.Start(clientSocket);
            }
        }

        private void Send(Socket socket, string data)
        {
            if (socket != null && data != null && !data.Equals(""))
            {
                byte[] bytes = Encoding.UTF8.GetBytes(data);   // 将data转化为byte数组  
                socket.Send(bytes);                            
            }
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

        private static int GetName(int bigin , string info)
        {
            int number = bigin;
            while (info[number] != '\0')
            {
                number++;
            }
            return number - bigin;
        }

        public void ReceiveMessage(object clientSocket)
        {
            Socket myClientSocket = (Socket)clientSocket;
            string clientIp = myClientSocket.RemoteEndPoint.ToString();
            int judge = 0;
            for (int i = 0; i < clients.Count; i++)
            {
                if (clients[i] != null && clients[i].RemoteEndPoint.ToString() == clientIp)
                    judge = 1;
            }
            if (judge == 0)
                clients.Add(myClientSocket);
            else
            {
                MessageBox.Show("相同的IP地址");
                return;
            }

            IPAddress remote_ip = ((System.Net.IPEndPoint)myClientSocket.RemoteEndPoint).Address;
            string ip = remote_ip.ToString();
            while (true)
            {
                try
                {
                    //通过clientSocket接收数据  
                    result = new byte[1024];
                    myClientSocket.Receive(result);
                    string info = System.Text.Encoding.UTF8.GetString(result);
                    int number = GetIPNumer(info);
                    string IP = info.Substring(0, number);
                    string name = info.Substring(number + 1 , GetName(number+1,info));
                    ChatMemer member = new ChatMemer(IP,name);

                    if(onlineMembers.Count != 0)
                    {
                        int judge1 = 0;
                        foreach (ChatMemer mem in onlineMembers)
                        {
                            if (mem.name == member.name)
                            {
                                judge1 = 1;
                            }
                        }
                        if (judge1 == 0)
                        {
                            onlineMembers.Add(member);
                        }
                    }
                    else
                    {
                        onlineMembers.Add(member);
                    }

                    InitMemner();

                    if (onlineMembers.Count != 0)
                    {
                        //foreach (ChatMemer mem in onlineMembers)
                        //Send(PutPackage(), clientIp);
                        for(int i = 0;i<clients.Count; i++)
                        {
                            Send(clients[i], PutPackage());
                        }
                    }
                }
                catch
                {
                    RemoveofflineMemer(ip);//修改list
                    InitMemner();//修改listView
                    myClientSocket.Shutdown(SocketShutdown.Both);
                    myClientSocket.Close();
                    break;
                }
            }
        }

        public string  PutPackage()
        {
            string putpackage = null;
            if(onlineMembers.Count != 0)
            {
                foreach(ChatMemer mem in onlineMembers)
                {
                    putpackage += (mem.IP + "/" + mem.name + " ");
                }
            }
            return putpackage;
        }
        public void  RemoveofflineMemer(string ip)
        {
            foreach (ChatMemer mem in onlineMembers)
            {
                if(mem.IP == ip)
                {
                    onlineMembers.Remove(mem);
                    break;
                }
            }
        }

        public void Init()
        {
            ColumnHeader ch = new ColumnHeader();
            ch.Text = "登陆状态";
            ch.Width = 240;
            ch.TextAlign = HorizontalAlignment.Left;//设置列的对齐方式
            this.listView1.Columns.Add(ch);

            ColumnHeader ch1 = new ColumnHeader();
            ch1.Text = "IP地址";
            ch1.Width = 240;
            ch1.TextAlign = HorizontalAlignment.Left;//设置列的对齐方式
            this.listView1.Columns.Add(ch1);

            ColumnHeader ch2 = new ColumnHeader();
            ch2.Text = "昵称";
            ch2.Width = 240;
            ch2.TextAlign = HorizontalAlignment.Left;//设置列的对齐方式
            this.listView1.Columns.Add(ch2);
        }

        public void  InitMemner()
        {
            CheckForIllegalCrossThreadCalls = false;
            this.listView1.Clear();
            this.listView1.View = View.Details;
            this.listView1.Columns.Add("登陆状态", 120);
            this.listView1.Columns.Add("IP地址", 120);
            this.listView1.Columns.Add("昵称",120);

            if(onlineMembers.Count != 0)
            {
                foreach (ChatMemer mem in onlineMembers)
                {
                    ListViewItem row = new ListViewItem();
                    row.Text = "已登陆";
                    row.SubItems.Add(mem.IP);
                    row.SubItems.Add(mem.name);
                    this.listView1.Items.Add(row);
                }
            }

        }

        public void RemoveAll()
        {
            this.listView1.Clear();
            this.listView1.Items.Clear();
        }
    }


}

