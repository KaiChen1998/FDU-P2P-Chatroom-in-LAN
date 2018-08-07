using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Oraycn.MCapture;
using Oraycn.MPlayer;
using System.Threading;
using System.Net;
using System.Net.Sockets;
using System.IO;
using System.Drawing;
using System.Drawing.Imaging;

namespace OnlineChat
{
    public partial class Form1 : Form
    {
        public Login form = null;
        public IAudioPlayer audioPlayer;
        public ICameraCapturer cameracapturer;
        public IMicrophoneCapturer microphoneCapturer;
        private int intPointVariable = 1;//端口增长幅度
        private IPEndPoint ipeLocal;//本地端口
        private IPEndPoint ipeRemote;//指定的远处端口
        private IPEndPoint ipeRemote1;//指定的远处端口
        private IPEndPoint ipeRemote2;//指定的远处端口
        private Socket LocalSocket;//本地套接字，要和本地端口绑定
        private long intMaxDataSize = 10000000;//接收缓冲区长度
        private Thread ListenThread;//监听线程1
        private Thread ListenThread_sound;
        private Thread ListenThread_video;
        private byte[] bytData;//数据保存数组
        private EndPoint epRemote;//监听的任意远处端口
        private EndPoint epRemote1;//监听的任意远处端口
        private EndPoint epRemote2;//监听的任意远处端口
        public List<friend> Friends = new List<friend>();
        public friend myself = new friend();
        public List<IPEndPoint> ipeLocals = new List<IPEndPoint>(3);//传文本，传音频，传视频
        public List<Socket> LocalSockets = new List<Socket>(3);//传文本，传音频，传视频
        private string ChatMemberName = null;
        public List<ListItem> list = new List<ListItem>();

        public Form1()
        {
            InitializeComponent();
            Oraycn.MCapture.GlobalUtil.SetAuthorizedUser("FreeUser", "");
            form = new Login();
            form.main = this;
            form.Show();
            this.Text = "ChatRoom";
            //本地socket配置
            //循环配置三种不同功能的三个不同Localsocket
            for (int i = 0, m = 8000; i < 3; i++, m+=500)
            {
                //本地socket配置
                ipeLocal = new IPEndPoint(IPAddress.Any, m);//配置本地IP 和 端口
                LocalSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
                ipeLocals.Add(ipeLocal);
                LocalSockets.Add(LocalSocket);
                BindSelf(LocalSockets[i], ipeLocals[i]);
            }

            //配置远端网口
            epRemote = (EndPoint)(new IPEndPoint(IPAddress.Any, 8000));//任何地方发来的数据都接受
            epRemote1 = (EndPoint)(new IPEndPoint(IPAddress.Any, 8500));//任何地方发来的数据都接受
            epRemote2 = (EndPoint)(new IPEndPoint(IPAddress.Any, 9000));//任何地方发来的数据都接受
            bytData = new byte[intMaxDataSize];

            //开始监听
            Listen();
        }

        /// <summary>
        /// 绑定自己的IP和端口
        /// </summary>
        /// <param name="ipe">IP端口对</param>
        /// <returns>绑定成功返回true</returns>
        public string BindSelf(Socket Local,IPEndPoint ipe)
        {
            while (true)
            {
                try
                {
                   Local.Bind(ipe);//socket与本地端点绑定
                   return ipe.Address.ToString() + " : " + ipe.Port;
                }
                catch
                {
                    ipe.Port = ipe.Port + intPointVariable;
                    intPointVariable++;
                }
            }
        }

        private string GetIpAddress()
        {
            string hostName = Dns.GetHostName();   //获取本机名
            IPHostEntry localhost = Dns.GetHostByName(hostName);    //方法已过期，可以获取IPv4的地址
            IPAddress localaddr = localhost.AddressList[localhost.AddressList.Length-1];
            return localaddr.ToString();
        }


        //Part1 语音通信
        private void button1_Click_1(object sender, EventArgs e)
        {
            if (button1.Text == "语音通话")
            {
                //远端socket配置
                ipeRemote1 = new IPEndPoint(IPAddress.Parse(label1.Text.ToString()), 8500);
                audioPlayer = PlayerFactory.CreateAudioPlayer(0, 16000, 1, 16, 2);
                microphoneCapturer = CapturerFactory.CreateMicrophoneCapturer(0);
                microphoneCapturer.AudioCaptured += new ESBasic.CbGeneric<byte[]>(microphoneCapturer_AudioCaptured);
                Listen_sound();
                microphoneCapturer.Start();
                button1.Text = "停止语音通话";
            }
            else
            {
                microphoneCapturer.Stop();
                ListenThread_sound.Suspend();
                button1.Text = "语音通话";
            }
        }

        //传送语音
        void microphoneCapturer_AudioCaptured(byte[] audioData)
        {
            LocalSockets[1].SendTo(audioData, ipeRemote1);
        }

        //音频监听线程
        public void Listen_sound()
        {
            ListenThread_sound = new Thread(new ThreadStart(DoListen_sound));
            ListenThread_sound.IsBackground = true;//设置为后台线程，这样当主线程结束后，该线程自动结束
            ListenThread_sound.Start();
        }

        public void DoListen_sound()
        {
            while (true)
            {
                if (LocalSockets[1].Poll(5000, SelectMode.SelectRead))
                {//每5ms查询一下网络，如果有可读数据就接收
                    LocalSockets[1].BeginReceiveFrom(bytData, 0, bytData.Length, SocketFlags.None, ref epRemote1, new AsyncCallback(ReceiveData), null);
                }
            }
        }

        /// <summary>
        /// 接收音频数据
        /// </summary>
        /// <param name="iar"></param>
        private void ReceiveData(IAsyncResult iar)
        {

            int intRecv = 0;
            try
            {
                intRecv = LocalSockets[1].EndReceiveFrom(iar, ref epRemote1);
            }
            catch
            {
                throw new Exception();
            }
            if (intRecv > 0)
            {
                byte[] bytReceivedData = new byte[intRecv];
                System.Buffer.BlockCopy(bytData, 0, bytReceivedData, 0, intRecv);
                audioPlayer.Play(bytReceivedData);
            }
        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {

        }


        //Part2 监听文本
        private void button2_Click(object sender, EventArgs e)
        {
            //远端socket配置
            try
            {
                ipeRemote = new IPEndPoint(IPAddress.Parse(label1.Text.ToString()), 8000);
                LocalSockets[0].SendTo(Encoding.UTF8.GetBytes(textBox4.Text), ipeRemote);
                TextView.Items.Add(myself.name + ":" + "\t\t\t" + textBox4.Text);
                textBox4.Text = null;
            }
            catch
            {
                MessageBox.Show("Something Error! Check your IP input.");
            }

        }

        /// <summary>
        /// 监听文本方法，用于监听远程发送到本机的信息
        /// </summary>
        public void Listen()
        {
            ListenThread = new Thread(new ThreadStart(DoListen));
            ListenThread.IsBackground = true;//设置为后台线程，这样当主线程结束后，该线程自动结束
            Control.CheckForIllegalCrossThreadCalls = false;
            ListenThread.Start();
        }

        /// <summary>
        /// 监听线程
        /// </summary>
        private void DoListen()
        {
            while (true)
            {
                if (LocalSockets[0].Poll(5000, SelectMode.SelectRead))
                {//每5ms查询一下网络，如果有可读数据就接收
                    LocalSockets[0].BeginReceiveFrom(bytData, 0, bytData.Length, SocketFlags.None, ref epRemote, new AsyncCallback(ShowTextData), null);
                }
            }
        }

        //文本输出函数
        private void ShowTextData(IAsyncResult iar)
        {
            int intRecv = 0;
            try
            {
                intRecv = LocalSockets[0].EndReceiveFrom(iar, ref epRemote);
            }
            catch
            {
                throw new Exception();
            }
            if (intRecv > 0)
            {
                byte[] bytReceivedData = new byte[intRecv];
                System.Buffer.BlockCopy(bytData, 0, bytReceivedData, 0, intRecv);
                TextView.Items.Add(ChatMemberName + ":" + "\t\t\t" + System.Text.Encoding.UTF8.GetString(bytReceivedData));
            }
        }

        private void label2_Click(object sender, EventArgs e)
        {

        }

        //Part3 视频通信
        private void button5_Click(object sender, EventArgs e)
        {
            if (button5.Text == "视频聊天")
            {
                ipeRemote2 = new IPEndPoint(IPAddress.Parse(label1.Text.ToString()), 9000);
                cameracapturer = CapturerFactory.CreateCameraCapturer(0, new Size(int.Parse("160"), int.Parse("120")), 10);//采集格式
                cameracapturer.ImageCaptured += new ESBasic.CbGeneric<Bitmap>(ImageCaptured);

                Listen_video();
                cameracapturer.Start();
                button5.Text = "停止视频聊天";
            }
            else
            {
                cameracapturer.Stop();
                ListenThread_video.Suspend();
                pictureBox1.Image = null;
                pictureBox2.Image = null;
                button5.Text = "视频聊天";
            }
        }

        //采集到的视频或桌面图像
        void ImageCaptured(Bitmap img)
        {
            if (this.InvokeRequired)
            {
                this.BeginInvoke(new ESBasic.CbGeneric<Bitmap>(this.ImageCaptured), img);
            }

            else
            {
                //展示自己的视频
                Bitmap old = (Bitmap)this.pictureBox1.Image;
                this.pictureBox1.Image = img;
                if (old != null)
                {
                    old.Dispose(); //立即释放不再使用的视频帧
                }

                //把img转化为Byte数组
                byte[] bytes = CompressionImage(img, 50);//压缩
                /*MemoryStream ms = new MemoryStream();
                img.Save(ms, System.Drawing.Imaging.ImageFormat.Bmp);
                byte[] bytes = ms.GetBuffer();  //byte[]   bytes=   ms.ToArray(); 这两句都可以，至于区别么，下面有解释
                ms.Dispose();
                ms.Close();*/

                //传送Byte数组
                LocalSockets[2].SendTo(bytes, ipeRemote2);//传送图像

            }
        }

        /// <summary>
        /// 监听方法，用于监听远程发送到本机的信息
        /// </summary>
        public void Listen_video()
        {
            ListenThread_video = new Thread(new ThreadStart(DoListen_video));
            ListenThread_video.IsBackground = true;//设置为后台线程，这样当主线程结束后，该线程自动结束
            Control.CheckForIllegalCrossThreadCalls = false;
            ListenThread_video.Start();
        }

        /// <summary>
        /// 监听线程
        /// </summary>
        private void DoListen_video()
        {
            while (true)
            {
                if (LocalSockets[2].Poll(5000, SelectMode.SelectRead))
                {//每5ms查询一下网络，如果有可读数据就接收
                    LocalSockets[2].BeginReceiveFrom(bytData, 0, bytData.Length, SocketFlags.None, ref epRemote2, new AsyncCallback(ShowVideoData), null);
                }
            }
        }

        //接收方展示视频
        private void ShowVideoData(IAsyncResult iar)
        {
            int intRecv = 0;
            try
            {
                intRecv = LocalSockets[2].EndReceiveFrom(iar, ref epRemote2);
            }
            catch
            {
                throw new Exception();
            }
            if (intRecv > 0)
            {
                byte[] bytReceivedData = new byte[intRecv];
                System.Buffer.BlockCopy(bytData, 0, bytReceivedData, 0, intRecv);

                //把byte数组转化为bitmap
                MemoryStream ms1 = new MemoryStream(bytReceivedData);
                Bitmap img = (Bitmap)Image.FromStream(ms1);
                ms1.Dispose();
                ms1.Close();
                
                //接收方播放视频
                Bitmap old = (Bitmap)pictureBox2.Image;
                pictureBox2.Image = img;
                if (old != null)
                {
                    old.Dispose(); //立即释放不再使用的视频帧
                }
            }
        }

        //视频压缩函数
        private static byte[] CompressionImage(Bitmap img, long quality)
        {
            //转成jpg
            var eps = new EncoderParameters(1);
            var ep = new EncoderParameter(System.Drawing.Imaging.Encoder.Quality, 50L);
            eps.Param[0] = ep;
            var jpsEncodeer = GetEncoder(ImageFormat.Png);

            //保存图片
            MemoryStream ms = new MemoryStream();
            img.Save(ms, jpsEncodeer, eps);

            //释放资源
            ep.Dispose();
            eps.Dispose();
            return ms.ToArray();
        }

        private static ImageCodecInfo GetEncoder(ImageFormat format)
        {
            ImageCodecInfo[] codecs = ImageCodecInfo.GetImageDecoders();
            foreach (ImageCodecInfo codec in codecs)
            {
                if (codec.FormatID == format.Guid)
                { return codec; }
            }
            return null;
        }

        private void pictureBox1_Click(object sender, EventArgs e)
        {

        }

        private void Form1_FormClosed(object sender, FormClosedEventArgs e)
        {
            form.Close();
        }

        private void button7_Click(object sender, EventArgs e)
        {
            //初始化好友列表
            if (Friends.Count != 0)
            {
                listBox1.DisplayMember = "Text";
                listBox1.ValueMember = "Value";
                foreach (friend mem in Friends)
                {
                    list.Add(new ListItem(mem.name, mem.ip));
                }
                listBox1.DataSource = list;
            }
        }

        private void listBox1_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (this.listBox1.SelectedItem != null)
            {
                this.label1.Text = this.listBox1.SelectedValue.ToString();
                ChatMemberName = list[this.listBox1.SelectedIndex].Text.ToString();
            }
        }

        private void listBox1_DoubleClick(object sender, EventArgs e)
        {
            if (this.listBox1.SelectedItem != null)
            {
                this.label1.Text = this.listBox1.SelectedValue.ToString();
                ChatMemberName = list[this.listBox1.SelectedIndex].Text.ToString();
            }
        }

        private void pictureBox2_Click(object sender, EventArgs e)
        {

        }

        private void label4_Click(object sender, EventArgs e)
        {

        }
    }

    


}
