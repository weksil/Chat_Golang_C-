using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Numerics;

namespace testTCP
{
    class Program
    {
        private const string Host = "127.0.0.1";
        private const int port = 5555;
        private static bool isGettingID;
        private static bool isGettingKey;
        static void Main(string[] args)
        {
            // var t = (Pakage.Pow(49,8))%110;
            // Console.WriteLine(t);
            Work();
        }
        public static void Work()
        {
            Console.Write("Введите ip: ");
            var host = Console.ReadLine();
            if(host.Length == 0) host = Host;
            Console.Write("Введите имя: ");
            var name = Console.ReadLine();
            var messClient = new Client(){Name = name};
            messClient.Init();
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

                    #region Id
                        data = Pakage.GetID(messClient);
                        stream.WriteAsync(data,0,data.Length);
                        
                        Console.WriteLine("Getting id...");
                        while (!isGettingID){}
                        Console.WriteLine("id: " + messClient.Id + "\n");
                    #endregion
                    #region key
                        data = Pakage.MakeKey(messClient.G,messClient.P,messClient.A);
                        stream.WriteAsync(data,0,data.Length);
                        
                        Console.Write("Getting key...");
                        while (!isGettingKey){}
                        Console.Write(" Finish\n");
                    #endregion

                    do
                    {
                        mess.Body = Console.ReadLine();
                        Console.WriteLine("------------------");
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
                    Console.WriteLine("Error: " + e.Message);                   
                    Console.WriteLine("Tree: " + e.StackTrace);                   
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
                    
                        Pakage.Parse(msgClient,msg,data);
                        Console.WriteLine(msg.ToString() + "\n");
                        Console.WriteLine("------------------");
                        break;
                    case Pakage.commGetID:
                        // Pakage.Parse(msgClient,msg,data,false);
                        msgClient.Id = BitConverter.ToUInt32(data,1);;
                        msgClient.CalculateData();                       
                        isGettingID = true;

                        break;
                    case Pakage.commGetKey:
                        Pakage.Parse(msgClient,data);                     
                        isGettingKey = true;

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
        public UInt64 G;
        public UInt64 P;
        public UInt64 A;
        public UInt64 firstKey;
        private UInt64 SecretKey;
        public void Init()
        {
            Random r = new Random();
            G = (UInt64)r.Next(200,500);
            P = (UInt64)r.Next(10000,20000);

            firstKey = (UInt64)r.Next(50,100);
            A = UInt64.Parse( BigInteger.ModPow(G,firstKey,P).ToString());
            
        }
        public void SetKey(UInt64 key)
        {
            SecretKey = key;
        }
        public void CalculateData()
        {
            NameBytes = Encoding.UTF8.GetBytes(Name);
            IdBytes = BitConverter.GetBytes(Id);
        }
        public void Decode(byte[] mess)
        {
            int i = 1;
            while(mess[i - 1] != 255 && mess[i] != 0 )
            {

                mess[i] = (byte)(mess[i] ^ SecretKey);
                i++;
                if(mess.Length == i)
                {
                    mess[i] = (byte)(mess[i-1] ^ SecretKey);
                }
            }
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
        public const byte end = 255;
        private const byte separate = 254;
        private const int idSize = 4;
        public const byte commMess = 200;
        public const byte commGetID  = 201;
        private const byte commDisconnect = 202;
        public const byte commGetKey = 203;
        public const byte commSendKey = 204;
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
            msg.Author.Decode(res);
            return res;
        }
        public static byte[] MakeKey(UInt64 g, UInt64 p, UInt64 A)
        {
            int size = 8;
            byte[] res = new byte[2+3*size];
            res[0] = commGetKey;
            res[1+3*size] = end;
            var t = BitConverter.GetBytes(g);
            for (int i = 0; i < size; i++)
            {
                res[1+i] = t[i];
            }
            t = BitConverter.GetBytes(p);
            for (int i = 0; i < size; i++)
            {
                res[1+size+i] = t[i];
            }
            t = BitConverter.GetBytes(A);
            for (int i = 0; i < size; i++)
            {
                res[1+size + size+i] = t[i];
            }
            Console.WriteLine("G: " + g);
            Console.WriteLine("P: " + p);
            Console.WriteLine("A: " + A);
            return res;
        }
        public static void Parse(Client client, byte[] source)
        {
            UInt64 res = 0;
            UInt64 tmp = BitConverter.ToUInt64(source,1);
            Console.WriteLine("B: " + tmp);
            res = UInt64.Parse( BigInteger.ModPow(tmp,client.firstKey,client.P).ToString());
            
            client.SetKey(res);
            Console.WriteLine("secret key: " + res);
        }
        public static void Parse(Client client,Message outMess, byte[] source, bool mess = true  )
        {
            int iterator = idSize + 1 , i = iterator;
            // if(source[6] == end) return; // 0: comm,  1-9:  id, 10: end
            outMess.Author.Id = BitConverter.ToUInt32(source,1);
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
        public static UInt64 Pow(UInt64 x, UInt64 y)
        {
            UInt64 res = x;
            for (UInt64 i = 0; i < y -1; i++)
            {
                res *= x;
            }
            return res;

        }
    }
}