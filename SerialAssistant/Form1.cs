using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Text.RegularExpressions;

namespace SerialAssistant
{
    public partial class Form1 : Form
    {
        private long receive_count = 0; //接收字节计数
        private long send_count = 0;    //发送字节计数
        private StringBuilder sb = new StringBuilder();    //为了避免在接收处理函数中反复调用，依然声明为一个全局变量
        private DateTime current_time = new DateTime();    //为了避免在接收处理函数中反复调用，依然声明为一个全局变量

        public Form1()
        {
            InitializeComponent();
        }

        /* 扫描串口列表并添加到选择框 */
        private void Update_Serial_List()
        {
            comboBox1.Items.Clear();
            comboBox1.Items.AddRange(System.IO.Ports.SerialPort.GetPortNames());

            if (comboBox1.Items.Count > 0)
            {
                comboBox1.Text = comboBox1.Items[0].ToString();
            }
            else
            {
                comboBox1.Text = "";
            }
        }
        

        private void Form1_Load(object sender, EventArgs e)
        {
            /* 添加串口选择列表 */
            Update_Serial_List();

            /* 添加波特率列表 */
            string[] baud = { "9600", "38400", "57600", "115200" };
            comboBox2.Items.AddRange(baud);

            /* 添加数据位列表 */
            string[] data_length = { "5", "6", "7", "8", "9" };
            comboBox3.Items.AddRange(data_length);

            /* 添加校验位列表 */
            string[] verification_mode = { "None", "Odd", "Even", "Mark", "Space" };
            comboBox4.Items.AddRange(verification_mode);

            /* 添加停止位列表 */
            string[] stop_length = { "1", "1.5", "2" };
            comboBox5.Items.AddRange(stop_length);

            /* 设置默认选择值 */
            comboBox2.Text = "115200";
            comboBox3.Text = "8";
            comboBox4.Text = "None";
            comboBox5.Text = "1";

            /* 在串口未打开的情况下每隔2s刷新一次串口列表框 */
            timer1.Interval = 2000;
            timer1.Start();

        }


        private void timer1_Tick(object sender, EventArgs e)
        {
            Update_Serial_List();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            try
            {
                //将可能产生异常的代码放置在try块中
                //根据当前串口属性来判断是否打开
                if (serialPort1.IsOpen)
                {
                    //串口已经处于打开状态

                    serialPort1.Close();    //关闭串口
                    button1.Text = "打开串口";
                    comboBox1.Enabled = true;
                    comboBox2.Enabled = true;
                    comboBox3.Enabled = true;
                    comboBox4.Enabled = true;
                    comboBox5.Enabled = true;
                    label6.Text = "串口已关闭!";
                    label6.ForeColor = Color.Red;
                    button5.Enabled = false;        //失能发送按钮
                    

                    //开启端口扫描
                    timer1.Interval = 2000;
                    timer1.Start();
                }
                else
                {
                    /* 串口已经处于关闭状态，则设置好串口属性后打开 */
                    //停止串口扫描
                    timer1.Stop();
                    
                    comboBox1.Enabled = false;
                    comboBox2.Enabled = false;
                    comboBox3.Enabled = false;
                    comboBox4.Enabled = false;
                    comboBox5.Enabled = false;
                    serialPort1.PortName = comboBox1.Text;
                    serialPort1.BaudRate = Convert.ToInt32(comboBox2.Text);
                    serialPort1.DataBits = Convert.ToInt16(comboBox3.Text);

                    if (comboBox4.Text.Equals("None"))
                        serialPort1.Parity = System.IO.Ports.Parity.None;
                    else if (comboBox4.Text.Equals("Odd"))
                        serialPort1.Parity = System.IO.Ports.Parity.Odd;
                    else if (comboBox4.Text.Equals("Even"))
                        serialPort1.Parity = System.IO.Ports.Parity.Even;
                    else if (comboBox4.Text.Equals("Mark"))
                        serialPort1.Parity = System.IO.Ports.Parity.Mark;
                    else if (comboBox4.Text.Equals("Space"))
                        serialPort1.Parity = System.IO.Ports.Parity.Space;

                    if (comboBox5.Text.Equals("1"))
                        serialPort1.StopBits = System.IO.Ports.StopBits.One;
                    else if (comboBox5.Text.Equals("1.5"))
                        serialPort1.StopBits = System.IO.Ports.StopBits.OnePointFive;
                    else if (comboBox5.Text.Equals("2"))
                        serialPort1.StopBits = System.IO.Ports.StopBits.Two;

                    //打开串口，设置状态
                    serialPort1.Open();    
                    button1.Text = "关闭串口";
                    label6.Text = "串口已打开!";
                    label6.ForeColor = Color.Green;

                    //使能发送按钮
                    button5.Enabled = true;

                }
            }
            catch (Exception ex)
            {
                //捕获可能发生的异常并进行处理

                //捕获到异常，创建一个新的对象，之前的不可以再用
                serialPort1 = new System.IO.Ports.SerialPort();

                //刷新COM口选项
                comboBox1.Items.Clear();
                comboBox1.Items.AddRange(System.IO.Ports.SerialPort.GetPortNames());

                //响铃并显示异常给用户
                System.Media.SystemSounds.Beep.Play();
                button1.Text = "打开串口";
                MessageBox.Show(ex.Message);
                comboBox1.Enabled = true;
                comboBox2.Enabled = true;
                comboBox3.Enabled = true;
                comboBox4.Enabled = true;
                comboBox5.Enabled = true;

                //开启串口扫描
                timer1.Interval = 2000;
                timer1.Start();
            }
        }

        private void serialPort1_DataReceived(object sender, System.IO.Ports.SerialDataReceivedEventArgs e)
        {
            /* 串口接收事件处理 */

            int num = serialPort1.BytesToRead;      //获取接收缓冲区中的字节数
            byte[] received_buf = new byte[num];    //声明一个大小为num的字节数据用于存放读出的byte型数据


            receive_count += num;                   //接收字节计数变量增加nun
            serialPort1.Read(received_buf, 0, num);   //读取接收缓冲区中num个字节到byte数组中

            sb.Clear();     //防止出错,首先清空字符串构造器

            if (radioButton2.Checked)
            {
                //选中HEX模式显示
                foreach (byte b in received_buf)
                {
                    sb.Append(b.ToString("X2") + ' ');    //将byte型数据转化为2位16进制文本显示，并用空格隔开
                }

            }
            else
            {
                //选中ASCII模式显示
                sb.Append(Encoding.ASCII.GetString(received_buf));  //将整个数组解码为ASCII数组
            }
            try
            {
                //因为要访问UI资源，所以需要使用invoke方式同步ui
                Invoke((EventHandler)(delegate
                {
                    if (checkBox3.Checked)
                    {
                        //显示时间
                        current_time = System.DateTime.Now;     //获取当前时间
                        textBox1.AppendText("["+current_time.ToString("HH:mm:ss") + "]" + sb.ToString());
                    }
                    else
                    {
                        //不显示时间 
                        textBox1.AppendText(sb.ToString());
                    }
                    label8.Text = "接收：" + receive_count.ToString() + " Bytes";
                }
                  )
                );
            }
            catch (Exception ex)
            {
                //响铃并显示异常给用户
                System.Media.SystemSounds.Beep.Play();
                MessageBox.Show(ex.Message);

            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            textBox1.Clear();
            receive_count = 0;          //接收计数清零
            send_count = 0;             //发送计数
            label8.Text = "接收：" + receive_count.ToString() + " Bytes";
            label7.Text = "发送：" + receive_count.ToString() + " Bytes";
        }

        private void button5_Click(object sender, EventArgs e)
        {
            /* 发送发送区中的数据 */

            byte[] temp = new byte[1];

            try
            {
                //首先判断串口是否开启
                if (serialPort1.IsOpen)
                {
                    int num = 0;   //获取本次发送字节数

                    //判断发送模式
                    if (radioButton4.Checked)
                    {
                        //以HEX模式发送
                        //首先需要用正则表达式将用户输入字符中的十六进制字符匹配出来
                        string buf = textBox2.Text;
                        string pattern = @"\s";
                        string replacement = "";
                        Regex rgx = new Regex(pattern);
                        string send_data = rgx.Replace(buf, replacement);

                        //不发送新行
                        num = (send_data.Length - send_data.Length % 2) / 2;
                        for (int i = 0; i < num; i++)
                        {
                            temp[0] = Convert.ToByte(send_data.Substring(i * 2, 2), 16);
                            serialPort1.Write(temp, 0, 1);  //循环发送
                        }
                        //如果用户输入的字符是奇数，则单独处理
                        if (send_data.Length % 2 != 0)
                        {
                            temp[0] = Convert.ToByte(send_data.Substring(textBox2.Text.Length - 1, 1), 16);
                            serialPort1.Write(temp, 0, 1);
                            num++;
                        }
                        //判断是否需要发送新行
                        if (checkBox2.Checked)
                        {
                            //自动发送新行
                            serialPort1.WriteLine("");
                        }
                    }
                    else
                    {
                        //以ASCII模式发送
                        //判断是否需要发送新行
                        if (checkBox2.Checked)
                        {
                            //自动发送新行
                            serialPort1.WriteLine(textBox2.Text);
                            num = textBox2.Text.Length + 2; //回车占两个字节
                        }
                        else
                        {
                            //不发送新行
                            serialPort1.Write(textBox2.Text);
                            num = textBox2.Text.Length;
                        }
                    }

                    send_count += num;      //计数变量累加
                    label8.Text = "Tx:" + send_count.ToString() + "Bytes";   //刷新界面
                }
            }
            catch (Exception ex)
            {
                serialPort1.Close();
                //捕获到异常，创建一个新的对象，之前的不可以再用
                serialPort1 = new System.IO.Ports.SerialPort();
                //刷新COM口选项
                comboBox1.Items.Clear();
                comboBox1.Items.AddRange(System.IO.Ports.SerialPort.GetPortNames());
                //响铃并显示异常给用户
                System.Media.SystemSounds.Beep.Play();
                button1.Text = "打开串口";
                MessageBox.Show(ex.Message);
                comboBox1.Enabled = true;
                comboBox2.Enabled = true;
                comboBox3.Enabled = true;
                comboBox4.Enabled = true;
                comboBox5.Enabled = true;
            }
        }
    }
}
