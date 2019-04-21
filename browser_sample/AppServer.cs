﻿using Newtonsoft.Json;
using SuperWebSocket;
using System;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Security.Cryptography.X509Certificates;
using System.Net.Security;
using System.Net.Sockets;
using System.Net;
using System.Configuration;
using System.Text;
using browser_sample;

namespace ParallelHybridApp
{

    public partial class AppServer : Form
    {
        public List<String> log_ary = new List<string>();
        public static AppServer frm;
        public Dictionary<string, WebSocketSession> session_ary = new Dictionary<string, WebSocketSession>();

        SuperWebSocket.WebSocketServer server_ssl;



        public AppServer()
        {
            InitializeComponent();
        }


        private void Form1_Load(object sender, EventArgs e)
        {
            frm = this;

            try
            {
                var server_config_ssl = new SuperSocket.SocketBase.Config.ServerConfig()
                {
                    Port = 443,
                    Ip = "127.0.0.1",
                    MaxConnectionNumber = 100,
                    Mode = SuperSocket.SocketBase.SocketMode.Tcp,
                    Name = "SuperWebSocket Sample Server",
                    MaxRequestLength = 1024 * 1024 * 10,
                    Security = "tls",
                    Certificate = new SuperSocket.SocketBase.Config.CertificateConfig
                    {
                        FilePath = ConfigurationManager.AppSettings["cert_file_path"],
                        Password = ConfigurationManager.AppSettings["cert_password"]
                    }
                };

                setup_server(ref server_ssl, server_config_ssl);

                valid_cert();


            }
            catch (Exception ex)
            {
                reflesh_cert();

                MessageBox.Show("証明書を更新しました。\nアプリケーションを再起動します。");

                Application.Restart();
            }

        }

        private void setup_server(ref WebSocketServer server, SuperSocket.SocketBase.Config.ServerConfig serverConfig)
        {
            var rootConfig = new SuperSocket.SocketBase.Config.RootConfig();

            server = new SuperWebSocket.WebSocketServer();

            //サーバーオブジェクト作成＆初期化
            server.Setup(rootConfig, serverConfig);

            //イベントハンドラの設定
            //接続
            server.NewSessionConnected += HandleServerNewSessionConnected;
            //メッセージ受信
            server.NewMessageReceived += HandleServerNewMessageReceived;
            //切断        
            server.SessionClosed += HandleServerSessionClosed;

            //サーバー起動
            server.Start();

        }


        //接続
        static void HandleServerNewSessionConnected(SuperWebSocket.WebSocketSession session)
        {
            frm.session_ary.Add(session.SessionID, session);

            frm.Invoke((MethodInvoker)delegate ()
            {
                frm.add_log(DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"), "接続");
            });

        }

        //メッセージ受信
        static void HandleServerNewMessageReceived(SuperWebSocket.WebSocketSession session,
                                                    string e)
        {
            frm.Invoke((MethodInvoker)delegate ()
            {
                MessageData recv = JsonConvert.DeserializeObject<MessageData>(e);

                switch (recv.command)
                {
                    case "add_message_to_app":

                        frm.add_log(recv.time, "受信: " + recv.message);

                        break;
                    case "start_search":

                        string keyword = recv.message;

                        frm.add_log(recv.time, "検索開始: " + keyword);

                        BrowserForm brws_frm = new BrowserForm();

                        brws_frm.keyword = keyword;
                        brws_frm.on_search_complete = delegate ()
                        {
                            frm.add_log(recv.time, "検索終了: " + keyword);

                            //TODO:メッセージ送信

                            SearchCompleteData send = new SearchCompleteData();

                            send.search_result_ary = brws_frm.search_result_ary;

                            frm.send_message_complete_search(send);

                            brws_frm.Close();
                        };

                        brws_frm.Show();
                        brws_frm.start();

                        break;
                }

            });

        }

        //切断
        static void HandleServerSessionClosed(SuperWebSocket.WebSocketSession session,
                                                    SuperSocket.SocketBase.CloseReason e)
        {
            if (frm != null)
            {
                frm.session_ary.Remove(session.SessionID);

                frm.Invoke((MethodInvoker)delegate ()
                {
                    frm.add_log(DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"), "切断");
                });
            }
        }

        private void Form1_FormClosed(object sender, FormClosedEventArgs e)
        {
            frm = null;

            server_ssl.Stop();
        }

        public void add_log(string time, String log)
        {
            log = "[" + time + "] " + log + "\r\n";
            this.txtMessage.AppendText(log);
        }

        //メッセージ送信
        private void send_message_to_sessions(string message)
        {
            foreach (var session in session_ary.Values)
            {
                MessageData send = new MessageData();

                send.command = "add_message_to_browser";
                send.message = message;
                send.time = DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss");

                string send_str = JsonConvert.SerializeObject(send);

                session.Send(send_str);

                add_log(send.time, "送信:" + message);
            }
        }

        private void send_message_complete_search(SearchCompleteData send)
        {
            send.time = DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss");

            string send_str = JsonConvert.SerializeObject(send);

            foreach (var session in session_ary.Values)
            {
                session.Send(send_str);

                add_log(send.time, "送信:SearchCompleteData");
            }
        }

        private void btnSend_Click(object sender, EventArgs e)
        {
            send_message_to_sessions(this.txtSendMessage.Text);
        }

        private static Boolean RemoteCertificateValidationCallback(Object sender,
        X509Certificate certificate,
        X509Chain chain,
        SslPolicyErrors sslPolicyErrors)
        {
            if (sslPolicyErrors == SslPolicyErrors.None)
            {
                return true;
            }

            return false;
        }

        private void valid_cert()
        {
            String hostName = ConfigurationManager.AppSettings["cert_local_host"];
            Int32 port = 443;

            using (TcpClient client = new TcpClient())
            {
                //接続先Webサーバー名からIPアドレスをルックアップ    
                IPAddress[] ipAddresses = Dns.GetHostAddresses(hostName);

                //Webサーバーに接続する
                client.Connect(new IPEndPoint(ipAddresses[0], port));

                //SSL通信の開始
                using (SslStream sslStream =
                    new SslStream(client.GetStream(), false, RemoteCertificateValidationCallback))
                {
                    //サーバーの認証を行う
                    //これにより、RemoteCertificateValidationCallbackメソッドが呼ばれる
                    sslStream.AuthenticateAsClient(hostName);
                }
            }
        }

        private void reflesh_cert()
        {
            //証明書の更新

            var cert_file_url = ConfigurationManager.AppSettings["cert_file_url"];
            var cert_file_path = ConfigurationManager.AppSettings["cert_file_path"];

            var wc = new WebClient();
            wc.DownloadFile(cert_file_url, cert_file_path);
        }


    }
}
