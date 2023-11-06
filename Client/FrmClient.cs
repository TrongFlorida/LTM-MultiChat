using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

using System.Net.Http;
using System.Collections;
using System.Web.Script.Serialization;


namespace Client
{
    public partial class FrmClient : Form
    {
        public FrmClient()
        {
            InitializeComponent();
            KetNoi();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            Gui();
            AddMyMessage(txbMessage.Text);
            txbMessage.Clear();
        }


        public string TranslateText(string input)
        {
            string url = String.Format
            ("https://translate.googleapis.com/translate_a/single?client=gtx&sl={0}&tl={1}&dt=t&q={2}",
             "en", "vi", Uri.EscapeUriString(input));
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
        Socket sckClient;
        /// <summary>
        /// Kết nối tới Sever
        /// </summary>
        void KetNoi()
        {
            IP = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 9999);
            sckClient = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.IP);

            try
            {
                sckClient.Connect(IP);
            }
            catch
            {
                MessageBox.Show("Không thể kết nối đến sever!!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            Thread listen = new Thread(Nhan);
            listen.IsBackground = true;//Tắt thread khi tắt chương trình
            listen.Start();
        }

        /// <summary>
        /// Tắt kết nối
        /// </summary>
        void Dong()
        {
            sckClient.Close();
            this.Close();
        }

        /// <summary>
        /// Gửi tin
        /// </summary>
        void Gui()
        {
            //Chỉ gửi nếu có nội dung bên trong textbox
            if (txbMessage.Text != String.Empty)
            {
                sckClient.Send(dataSerialize(txbMessage.Text));
            }
        }
        
        /// <summary>
        /// Nhận tin
        /// </summary>
        void Nhan()
        {
            try
            {
                // Dùng while để luôn lắng nghe
                while (true)
                {
                    byte[] data = new byte[1024 * 5000];//5 MB
                    sckClient.Receive(data);

                    string message = (string)dataDeserialize(data);

                    AddReceiveMessage(message);
                }
            } catch
            {
                this.Close();
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
        }

        /// <summary>
        /// Hiện tin nhắn từ máy khác lên khung chat
        /// </summary>
        /// <param name="receiveMsg"></param>
        void AddReceiveMessage(string receiveMsg)
        {
            ListViewItem msg = new ListViewItem();
            msg.Text = "Client: " + TranslateText(receiveMsg);
            lsvMessage.Items.Add(msg);
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
        /// Tắt kết nối khi đóng Form
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void FrmClient_FormClosed(object sender, FormClosedEventArgs e)
        {
            this.Close();
        }
    }
}
