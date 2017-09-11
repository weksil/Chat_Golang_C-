using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace testTCP
{
    class Program
    {
        private const string Host = "127.0.0.1";
        private const int port = 5555;
        static void Main(string[] args)
        {
            Console.Write("Введите ip: ");
            var host = Console.ReadLine();
            Console.Write("Введите имя: ");
            var name = Console.ReadLine();
            Console.Write("Введите id: ");
            var id = UInt32.Parse(Console.ReadLine());
            var messClient = new Client(){Id = id, Name = name};
            var mess = new Message(){Author = messClient, Body = ""};
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
                    data = Pakage.Make(mess);
                    stream.WriteAsync(data,0,data.Length);
                    Thread listening = new Thread(new ParameterizedThreadStart(ListenServer));
                    listening.Start(client);
                    do
                    {
                        mess.Body = Console.ReadLine();
                        if(mess.Body.Length == 0 || mess.Body == "qq")
                            break;
                        data = Pakage.Make(mess);
                        stream.WriteAsync(data,0,data.Length);
                    } while (true);
                    Console.WriteLine("can disconect");
                    listening.Abort();
                    client.Close();
                    Console.WriteLine("status: " + client.Connected );
                }
            }
            catch (System.Exception e)
            {
                    Console.Write("Error: " + e.Message);                   
            }
        }
        public static void ListenServer(Object source)
        {
            TcpClient client = source as TcpClient;
            byte[] data = new byte[1024];
            var stream = client.GetStream();
            int numberBytes;
            Message msg = new Message();
            msg.Author = new Client();
            while (client.Connected && stream.CanRead)
            {
                numberBytes = stream.Read(data,0,data.Length);
                if(numberBytes == 0) continue;
                Pakage.Parse(msg,data);
                Console.WriteLine(msg.ToString());
            }
        }
        
    }
    public class Client
    {
        public UInt32 Id;
        public string Name; 
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
        private static byte[] end = new byte[1]{254};
        private static byte[] separate = new byte[1]{253};
        private const int idSize = 4;
        public static byte[] Make(Message msg)
        {
            byte[] idAuthor = BitConverter.GetBytes(msg.Author.Id);
            // Reverse(idAuthor);
            byte[] nameAuthor = Encoding.UTF8.GetBytes(msg.Author.Name);
            byte[] body = Encoding.UTF8.GetBytes(msg.Body);
            byte[] res = new byte[end.Length +separate.Length + idAuthor.Length + nameAuthor.Length + body.Length];
            int iterator = 0;
            int i;
            for (i = 0; i < idAuthor.Length; i++)
            {
                res[iterator] = idAuthor[i];
                iterator ++;
            }
            for (i = 0; i < nameAuthor.Length; i++)
            {
                res[iterator] = nameAuthor[i];
                iterator ++;
            }
            for (i = 0; i < separate.Length; i++)
            {
                res[iterator] = separate[i];
                iterator ++;
            }
            for (i = 0; i < body.Length; i++)
            {
                res[iterator] = body[i];
                iterator ++;
            }
            for (i = 0; i < end.Length; i++)
            {
                res[iterator] = end[i];
                iterator ++;
            }
            return res;
        }
        public static void Parse(Message outMess, byte[] source)
        {
            int iterator = idSize , i = iterator;
            outMess.Author.Id = BitConverter.ToUInt32(source,0);
            while (source[iterator] != separate[0])
            {
                iterator ++;
            }
            outMess.Author.Name = Encoding.UTF8.GetString(source,i,(iterator-i));
            iterator++;
            i = iterator;
            while (source[iterator] != end[0])
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
    }
}
