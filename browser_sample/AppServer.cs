﻿using Newtonsoft.Json;
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
using SuperSocket.WebSocket;

namespace ParallelHybridApp
{

    public partial class AppServer : Form
    {
        public List<String> log_ary = new List<string>();
        public static AppServer frm;
        public Dictionary<string, WebSocketSession> session_ary = new Dictionary<string, WebSocketSession>();

        WebSocketServer server;

        private BrowserForm _browser_frm  = new BrowserForm();


        public AppServer()
        {
            InitializeComponent();
        }


        private void Form1_Load(object sender, EventArgs e)
        {
            frm = this;

            try
            {
                var server_config = new SuperSocket.SocketBase.Config.ServerConfig()
                {
                    Port = 80,
                    Ip = "127.0.0.1",
                    MaxConnectionNumber = 100,
                    Mode = SuperSocket.SocketBase.SocketMode.Tcp,
                    Name = "SuperWebSocket Sample Server",
                    MaxRequestLength = 1024 * 1024 * 10
                };

                setup_server(ref server, server_config);

                _browser_frm.Show();

            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }

        }

        private void setup_server(ref WebSocketServer server, SuperSocket.SocketBase.Config.ServerConfig serverConfig)
        {
            var rootConfig = new SuperSocket.SocketBase.Config.RootConfig();

            server = new WebSocketServer();

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
        static void HandleServerNewSessionConnected(WebSocketSession session)
        {
            frm.session_ary.Add(session.SessionID, session);

            frm.Invoke((MethodInvoker)delegate ()
            {
                frm.add_log(DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"), "接続");
            });

        }

        //メッセージ受信
        static void HandleServerNewMessageReceived(WebSocketSession session,
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

                        //検索開始

                        string keyword = recv.message;

                        frm.add_log(recv.time, "検索開始: " + keyword);

                        frm._browser_frm.keyword = keyword;
                        frm._browser_frm.on_search_complete = delegate ()
                        {
                            frm.add_log(recv.time, "検索終了: " + keyword);

                            //検索結果送信

                            SearchCompleteData send = new SearchCompleteData();

                            send.search_result_ary = frm._browser_frm.search_result_ary;

                            frm.send_message_complete_search(send);

                        };

                        frm._browser_frm.start();

                        break;
                }

            });

        }

        //切断
        static void HandleServerSessionClosed(WebSocketSession session,
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

            server.Stop();
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


    }
}
