﻿//#define DEBUG
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using Network;


// 하경이 아이디 hakyongch
namespace Sonagi_Server
{
    public class Network // For Server
    {
        private List<Client> clients = new List<Client>();
        Socket sock;
        int port;
        public Network(int port)
        {
            this.port = port;
            sock = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            //sock.Bind(new IPEndPoint(IPAddress.Parse("45.32.50.20"), port)); sock.Listen(100);
            sock.Bind(new IPEndPoint(IPAddress.Any, port));
            SocketAsyncEventArgs args = new SocketAsyncEventArgs();
            //args.Completed += Args_Completed;
            //sock.AcceptAsync(args);
        }

        public void Listen()
        {
            //sock.Bind(new IPEndPoint(IPAddress.Any, port));
            sock.Listen(100);
            sock.BeginAccept(new AsyncCallback(Accept_Callback), sock);
        }

        void Accept_Callback(IAsyncResult iar)
        {
            Socket old = (Socket)iar.AsyncState;
            Socket client = old.EndAccept(iar);
            Client cl = new Client(client);
            clients.Add(cl);
            old.BeginAccept(new AsyncCallback(Accept_Callback), old);
            cl.sock.BeginReceive(cl.buffer, 0, cl.buffer.Length, SocketFlags.None, new AsyncCallback(Recevie_Callback), cl);
        }
        void Recevie_Callback(IAsyncResult iar)
        {
            Client cli = (Client)iar.AsyncState;
            int rev = 0;
            try
            {
                rev = cli.sock.EndReceive(iar);
            }
            catch { }
            if (rev != 0)
            {
                try
                {
                    Data d = Data.Deserialize(cli.buffer);
                    switch (d.Type)
                    {
                        case DataType.NONE:
                            Console.WriteLine(cli.IP.ToString() + "(" + d.Info.NickName + ")" + "님이 접속하셨습니다.");
#if DEBUG
                            Console.WriteLine(cli.buffer.Length);
#endif
                            Data _d = new Data(DataType.NONE, cli.IP, new ClientInfo(cli.IP, d.Info.NickName));
                            SocketAsyncEventArgs _args = new SocketAsyncEventArgs();

                            Send(cli, _d);
                            foreach(Client c in clients)
                            {
                                Data data = new Data(DataType.INFO, d.Info.NickName + "님이 접속하셨습니다.", new ClientInfo("", ""));
                                Send(c, data);
                            }
                            break;
                        case DataType.STRING:
                            Console.WriteLine(cli.IP.ToString() + " : " + d.InnerData.ToString());
                            foreach (Client c in clients)
                            {
                                SocketAsyncEventArgs args = new SocketAsyncEventArgs();
                                args.SetBuffer(cli.buffer, 0, 4096);
                                args.Completed += Args_Completed;
                                args.UserToken = c;
                                c.sock.SendAsync(args);
                            }
                            break;

                    }
                }
                catch(Exception ex)
                {
                    Console.WriteLine(ex.Message);
                    Console.WriteLine("개체 " + ex.Source + " 에서, 메서드 " + ex.TargetSite + "에서");
                }
                finally
                {
                    cli.buffer = new byte[4096];
                    try
                    {
                        cli.sock.BeginReceive(cli.buffer, 0, cli.buffer.Length, SocketFlags.None, new AsyncCallback(Recevie_Callback), cli);
                    }
                    catch { }
                }
            }
            else
            {
                try
                {
                    Console.WriteLine(cli.IP.ToString() + "(" + cli.NickName + ")" + "님이 접속을 종료하셨습니다.");
                    //RemoveEvent(so);
                    clients.Remove(cli);
                    cli.sock.Close();

                    foreach (Client c in clients)
                    {
                        Data data = new Data(DataType.INFO, cli.NickName + "님이 접속을 종료하셨습니다.", new ClientInfo("", ""));
                        Send(c, data);
                    }
                }
                catch { }
            }
        }

        private void Args_Completed(object sender, SocketAsyncEventArgs e)
        {
            
        }

        void Send(Client cli, Data _d)
        {
            SocketAsyncEventArgs _args = new SocketAsyncEventArgs();

            byte[] _data = new byte[4096];
            byte[] serialized = _d.Serialize();

            for (int i = 0; i < 4096; i++)
                _data[i] = 0;
            for (int i = 0; i < serialized.Length; i++)
                _data[i] = serialized[i];

            _args.SetBuffer(_data, 0, 4096); // 여기서 버그 생김 계쏙.. 첫번째 클라는 보내지는데 그담부턴 에러 작렬.
            _args.Completed += Args_Completed;
            _args.UserToken = cli;
            cli.sock.SendAsync(_args);
        }
    }
}
