using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace testTCP
{
    class Program
    {
        private const string Host = "127.0.0.1";
        private const int port = 5555;
        private static bool isGettingID;
        static void Main(string[] args)
        {
            Console.Write("Введите ip: ");
            var host = Console.ReadLine();
            if(host.Length == 0) host = Host;
            Console.Write("Введите имя: ");
            var name = Console.ReadLine();
            var messClient = new Client(){Name = name};
            messClient.CalculateData();
            var mess = new Message(){Author = messClient, Body = ""};
            var cts = new CancellationTokenSource();

            try
            {    
                using(TcpClient client = new TcpClient())
                {
                    byte[] data;
                    Console.WriteLine("Try connect...");
                    client.Connect(host,port);
                    if(!client.Connected)
                    {
                        Console.WriteLine("Error connecting");                    
                    }
                    Console.WriteLine("Connecting!"); 
                    NetworkStream stream = client.GetStream();

                    Task listening = new Task(() => ListenServer(client,cts.Token,messClient));
                    listening.Start();

                    data = Pakage.GetID(messClient);
                    stream.WriteAsync(data,0,data.Length);
                    
                    Console.WriteLine("Getting id...");
                    while (!isGettingID){}
                    Console.WriteLine("id: " + messClient.Id + "\n");

                    do
                    {
                        mess.Body = Console.ReadLine();
                        if(mess.Body.Length == 0 || mess.Body == "qq")
                            break;
                        data = Pakage.Make(mess);
                        stream.WriteAsync(data,0,data.Length);
                    } while (true);
                    data = Pakage.Disconnect();
                    stream.WriteAsync(data,0,data.Length); 
                    cts.Cancel();
                }
                Console.WriteLine("Disconect");
                
            }
            catch (System.Exception e)
            {
                    Console.Write("Error: " + e.Message);                   
            }
        }
        public static void ListenServer(TcpClient client,CancellationToken cancelToken,Client msgClient)
        {
            byte[] data = new byte[1024];
            var stream = client.GetStream();
            int numberBytes;
            Message msg = new Message();
            msg.Author = new Client();
            while (!cancelToken.IsCancellationRequested)
            {
                numberBytes = stream.Read(data,0,data.Length);
                if(numberBytes == 0) continue;
                switch (data[0])
                {
                    case Pakage.commMess:
                        Pakage.Parse(msg,data);
                        Console.WriteLine(msg.ToString() + "\n");
                        break;
                    case Pakage.commGetID:
                        Pakage.Parse(msg,data);
                        msgClient.Id = msg.Author.Id;
                        msgClient.CalculateData();                       
                        isGettingID = true;

                        break;
                    default:
                                            
                        break;
                }
            }
        }
        
    }
    public class Client
    {
        public UInt32 Id;
        public string Name; 
        public byte[] NameBytes;
        public byte[] IdBytes;
        public void CalculateData()
        {
            NameBytes = Encoding.UTF8.GetBytes(Name);
            IdBytes = BitConverter.GetBytes(Id);
        }

    }
    public class Message 
    {
        public Client Author;
        public string Body;
        override public string ToString()
        {
            // return string.Format("\nID: {2} | Author: {0}\nBody: {1}",Author.Name,Body,Author.Id);
            return string.Format("\nAuthor: {0}\nBody: {1}",Author.Name,Body);
        }
    }
    public class Pakage
    {
        private const byte end = 255;
        private const byte separate = 254;
        private const int idSize = 4;
        public const byte commMess = 200;
        public const byte commGetID  = 201;
        public const byte commDisconnect = 202;

        public static byte[] Make(Message msg)
        {
            byte[] body = Encoding.UTF8.GetBytes(msg.Body);
            byte[] res = new byte[3 + msg.Author.IdBytes.Length+ msg.Author.NameBytes.Length + body.Length]; // 3 = separate + end + comm
            int iterator = 1;
            int i;
            res[0] = commMess;
            for (i = 0; i < msg.Author.IdBytes.Length; i++)
            {
                res[iterator] = msg.Author.IdBytes[i];
                iterator ++;
            }
            for (i = 0; i < msg.Author.NameBytes.Length; i++)
            {
                res[iterator] = msg.Author.NameBytes[i];
                iterator ++;
            }

            res[iterator] = separate;
            iterator ++;
            
            for (i = 0; i < body.Length; i++)
            {
                res[iterator] = body[i];
                iterator ++;
            }
            
            res[iterator] = end;
            return res;
        }
        public static void Parse(Message outMess, byte[] source)
        {
            int iterator = idSize + 1 , i = iterator;
            outMess.Author.Id = BitConverter.ToUInt32(source,1);
            if(source[5] == end) return; // 0: conmm,  1-4:  id, 5: end
            while (source[iterator] != separate)
            {
                iterator ++;
            }
            outMess.Author.Name = Encoding.UTF8.GetString(source,i,(iterator-i));
            iterator++;
            i = iterator;
            while (source[iterator] != end)
            {
                iterator ++;
            }
            outMess.Body = Encoding.UTF8.GetString(source,i,(iterator-i));
        }
        private static void Reverse(byte[] src)
        {            
            byte tmp ;
            for (int i = 0; i < src.Length/2; i++)
            {
                tmp = src[src.Length - 1 - i];
                src[src.Length - 1 - i] = src[i];
                src[i] = tmp;
            }
        }
        public static byte[] GetID(Client client)
        {
            var res = new byte[2 + client.NameBytes.Length];

            res[0] = commGetID;
            for (int i = 1; i < res.Length - 1; i++)
            {
                res[i] = client.NameBytes[i-1];
            }
            res[res.Length - 1] = end;
            return res;
        }
        public static byte[] Disconnect()
        {
            return new byte[2]{commDisconnect,end};
        }
    }
}