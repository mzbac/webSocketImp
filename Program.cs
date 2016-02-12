using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ConsoleApplication1
{
    class Program
    {   //browser client var exampleSocket = new WebSocket("ws://www.example.com/socketserver", "protocolOne");
        //exampleSocket.send("Here's some text that the server is urgently awaiting!");
        //reference https://developer.mozilla.org/en-US/docs/Web/API/WebSockets_API/Writing_WebSocket_client_applications
        static void Main(string[] args)
        {
            TcpListener server = new TcpListener(IPAddress.Parse("127.0.0.1"), 80);

            server.Start();
            Console.WriteLine("Server has started on 127.0.0.1:80.{0}Waiting for a connection...", Environment.NewLine);

            TcpClient client = server.AcceptTcpClient();

            Console.WriteLine("A client connected.");
            NetworkStream stream = client.GetStream();

            //enter to an infinite cycle to be able to handle every change in stream
            while (true)
            {
                while (!stream.DataAvailable) ;

                Byte[] bytes = new Byte[client.Available];

                stream.Read(bytes, 0, bytes.Length);
                //translate bytes of request to string
                String data = Encoding.UTF8.GetString(bytes);

                if (new Regex("^GET").IsMatch(data))
                {
                    Byte[] response = Encoding.UTF8.GetBytes("HTTP/1.1 101 Switching Protocols" + Environment.NewLine
                            + "Connection: Upgrade" + Environment.NewLine
                            + "Upgrade: websocket" + Environment.NewLine
                            + "Sec-WebSocket-Protocol: protocolOne" + Environment.NewLine
                            + "Sec-WebSocket-Accept: " + Convert.ToBase64String(
                                SHA1.Create().ComputeHash(
                                    Encoding.UTF8.GetBytes(
                                        new Regex("Sec-WebSocket-Key: (.*)").Match(data).Groups[1].Value.Trim() + "258EAFA5-E914-47DA-95CA-C5AB0DC85B11"
                                    )
                                )
                            ) + Environment.NewLine
                            + Environment.NewLine);

                    stream.Write(response, 0, response.Length);
                }
                else {
                    //If the second byte minus 128 is between 0 and 125, this is the length of message. If it is 126, the following 2 bytes (16-bit unsigned integer), if 127, the following 8 bytes (64-bit unsigned integer) are the length.
                    int length_code = bytes[1] & 127;
                    Byte[] masks;
                    Byte[] messageData;

                    if (length_code == 126)
                    {
                        masks = bytes.Skip(4).Take(4).ToArray();
                        messageData = bytes.Skip(6).ToArray();
                    }
                    else if (length_code == 127)
                    {
                        masks = bytes.Skip(10).Take(4).ToArray();
                        messageData = bytes.Skip(12).ToArray();
                    }
                    else {
                        masks = bytes.Skip(2).Take(4).ToArray();
                        messageData = bytes.Skip(6).ToArray();
                    }

                    //decoded byte = encoded byte XOR (position of encoded byte Mod 4)th byte of key
                    //reference https://developer.mozilla.org/en-US/docs/Web/API/WebSockets_API/Writing_WebSocket_server
                    Byte[] decoded = new Byte[messageData.Length];
                    for (int i = 0; i < messageData.Length; i++)
                    {
                        decoded[i] = (Byte)(messageData[i] ^ masks[i % 4]);
                    }
                    String message = Encoding.UTF8.GetString(decoded);
                    //encode message 
                    //reference http://stackoverflow.com/questions/8125507/how-can-i-send-and-receive-websocket-messages-on-the-server-side
                    byte[] rawData = decoded;
                    int frameCount = 0;
                    byte[] frame = new byte[10];

                    frame[0] = (byte)129;

                    if (rawData.Length <= 125)
                    {
                        frame[1] = (byte)rawData.Length;
                        frameCount = 2;
                    }
                    else if (rawData.Length >= 126 && rawData.Length <= 65535)
                    {
                        frame[1] = (byte)126;
                        int len = rawData.Length;
                        frame[2] = (byte)((len >> 8) & (byte)255);
                        frame[3] = (byte)(len & (byte)255);
                        frameCount = 4;
                    }
                    else {
                        frame[1] = (byte)127;
                        int len = rawData.Length;
                        frame[2] = (byte)((len >> 56) & (byte)255);
                        frame[3] = (byte)((len >> 48) & (byte)255);
                        frame[4] = (byte)((len >> 40) & (byte)255);
                        frame[5] = (byte)((len >> 32) & (byte)255);
                        frame[6] = (byte)((len >> 24) & (byte)255);
                        frame[7] = (byte)((len >> 16) & (byte)255);
                        frame[8] = (byte)((len >> 8) & (byte)255);
                        frame[9] = (byte)(len & (byte)255);
                        frameCount = 10;
                    }

                    int bLength = frameCount + rawData.Length;

                    byte[] reply = new byte[bLength];

                    int bLim = 0;
                    for (int i = 0; i < frameCount; i++)
                    {
                        reply[bLim] = frame[i];
                        bLim++;
                    }
                    for (int i = 0; i < rawData.Length; i++)
                    {
                        reply[bLim] = rawData[i];
                        bLim++;
                    }

                    stream.Write(reply, 0, reply.Length);
                }
            }
        }
    }
}
