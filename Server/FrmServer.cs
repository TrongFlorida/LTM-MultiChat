using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Net;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Collections;
using System.Net.Http;
using System.Web.Script.Serialization;

namespace Server
{
    public partial class FrmServer : Form
    {
        public FrmServer()
        {
            InitializeComponent();
            KetNoi();
        }

        private void btnSend_Click(object sender, EventArgs e)
        {
            foreach (Socket sck in clientList)
            {
                Gui(sck);
            }
            
            AddMyMessage(txbMessage.Text);
            txbMessage.Clear();
        }

        public string TranslateText(string input)
        {
            string url = String.Format
            ("https://translate.googleapis.com/translate_a/single?client=gtx&sl={0}&tl={1}&dt=t&q={2}",
             "vi", "en", Uri.EscapeUriString(input));
            HttpClient httpClient = new HttpClient();
            string result = httpClient.GetStringAsync(url).Result;
            var jsonData = new JavaScriptSerializer().Deserialize<List<dynamic>>(result);
            var translationItems = jsonData[0];
            string translation = "";
            foreach (object item in translationItems)
            {
                IEnumerable translationLineObject = item as IEnumerable;
                IEnumerator translationLineString = translationLineObject.GetEnumerator();
                translationLineString.MoveNext();
                translation += string.Format(" {0}", Convert.ToString(translationLineString.Current));
            }
            if (translation.Length > 1) { translation = translation.Substring(1); };
            return translation;
        }

        IPEndPoint IP;
        Socket sckServer;
        List<Socket> clientList;
        /// <summary>
        /// Kết nối tới Sever
        /// </summary>
        void KetNoi()
        {
            clientList = new List<Socket>();
            IP = new IPEndPoint(IPAddress.Any, 9999);
            sckServer = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.IP);

            sckServer.Bind(IP);// Đợi nhân IP từ client
            
            //Thread listen dùng để lắng nghe các kết nối đến sever
            Thread listen = new Thread(() => {
                try
                {
                    while (true)
                    {
                        sckServer.Listen(100);
                        Socket sckClient = sckServer.Accept();
                        clientList.Add(sckClient);

                        //Thread receive dùng để xử lý dữ liệu nhận được khi lắng nghe
                        Thread receive = new Thread(Nhan);
                        receive.IsBackground = true;
                        //Khởi động Thread receive và truyền cho nó sckClient
                        receive.Start(sckClient);
                    }
                }
                catch
                {
                    // Nếu có lỗi trong quá trình kết nối thì sẽ khởi tạo lại IP
                    IP = new IPEndPoint(IPAddress.Any, 9999);
                    sckServer = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.IP);
                }

            });

            listen.IsBackground = true;
            listen.Start();
        }

        /// <summary>
        /// Tắt kết nối
        /// </summary>
        void Dong()
        {
            sckServer.Close();
            this.Close();
        }

        /// <summary>
        /// Gửi tin
        /// </summary>
        void Gui(Socket sckClient)
        {
            //Chỉ gửi nếu có nội dung bên trong textbox
            if (sckClient != null && txbMessage.Text != String.Empty)
            {
                sckClient.Send(dataSerialize(txbMessage.Text));
            }
        }

        /// <summary>
        /// Nhận tin
        /// </summary>
        void Nhan(Object obj)
        {
            Socket sckClient = (Socket)obj;
            try
            {
                // Dùng while để luôn lắng nghe
                while (true)
                {
                    byte[] data = new byte[1024 * 5000];//5 MB
                    sckClient.Receive(data);

                    string message = (string)dataDeserialize(data);

                    //Nhận thông tin và gửi lại cho tất cả các client trong list
                    foreach (Socket sck in clientList) 
                    {
                        if(sck != null && sck != sckClient) //sck != sckClient để tránh server gửi lại chính client gửi
                            sck.Send(dataSerialize(message));
                    }

                    AddReceiveMessage(message);
                }
            }
            catch
            {
                //Nếu nhận k đc thì sẽ đóng kết nối với client bị lỗi đó đi
                clientList.Remove(sckClient);
                sckClient.Close();
            }
        }

        /// <summary>
        /// Hiện tin nhắn chính mình khung chat
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        void AddMyMessage(string myMsg)
        {
            ListViewItem msg = new ListViewItem();
            msg.Text = "Server: " + myMsg;
            lsvMessage.Items.Add(msg);

            ListViewItem msg1 = new ListViewItem();
            msg1.Text = "Server: " + myMsg;
            listView1.Items.Add(msg1);
        }

        /// <summary>
        /// Hiện tin nhắn từ máy khác lên khung chat
        /// </summary>
        /// <param name="receiveMsg"></param>
        void AddReceiveMessage(string receiveMsg)
        {
            ListViewItem msg = new ListViewItem();
            ListViewItem noti = new ListViewItem();
            noti.Text = "Translated from Vietnamese:";
            msg.Text = "Client: " + TranslateText(receiveMsg);
            lsvMessage.Items.Add(noti);
            lsvMessage.Items.Add(msg);

            ListViewItem msg1 = new ListViewItem();
            msg1.Text = "Client: " + receiveMsg;
            listView1.Items.Add(msg1);
        }

        /// <summary>
        /// Biến đổi dữ liệu thành dãy byte để truyền đi
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        byte[] dataSerialize(object obj)
        {
            MemoryStream memoryStream = new MemoryStream();
            BinaryFormatter bf = new BinaryFormatter();


            // obj sẽ bị phân mãnh và gán vào memoryStream
            bf.Serialize(memoryStream, obj);

            // chuyển dữ liệu đã phân mảnh thành mảng array
            return memoryStream.ToArray();
        }

        /// <summary>
        /// Chuyển dãy byte nhận được thành dữ liệu gốc
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        object dataDeserialize(byte[] data)
        {
            MemoryStream memoryStream = new MemoryStream(data);
            BinaryFormatter bf = new BinaryFormatter();

            // giải mã dữ liệu nhận được
            return bf.Deserialize(memoryStream);

        }

        /// <summary>
        /// Tắt kết nối khi đóng form
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void FrmServer_FormClosed(object sender, FormClosedEventArgs e)
        {
            this.Close();
        }

        private void listView1_SelectedIndexChanged(object sender, EventArgs e)
        {

        }
    }
}
