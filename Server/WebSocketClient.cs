using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace ConsoleApplication1.Server
{
    class WebSocketClient
    {
        private readonly TcpClient client;
        private readonly NetworkStream stream;
        public WebSocketClient(TcpClient client)
        {
            this.client = client;
            this.stream = client.GetStream();
        }

        public async void Listen()
        {
            while (this.client.Client.Connected)
            {
                try
                {
                    byte[] data = await readNetStreamInput();
                    String readMessage = Encoding.UTF8.GetString(data);
                    if (new Regex("^GET").IsMatch(readMessage))
                    {
                        byte[] response = acceptwebsocketrequest(readMessage);
                        stream.Write(response, 0, response.Length);
                        continue;
                    }

                    byte[] decoded;
                    string message;
                    if (CheckIfOnClose(data))
                        break;


                    DecodeMessage(data, out decoded, out message);
                    Console.WriteLine("message recieved : " + message + " from " + ((IPEndPoint)client.Client.RemoteEndPoint).Address);
                    byte[] reply = EncodeMessage(decoded);
                    stream.Write(reply, 0, reply.Length);




                }
                catch (Exception ex)
                {
                    Console.WriteLine("tcp client exception : "+ex.Message);
                    client.GetStream().Close();
                    client.Close();
                }

            }
            Console.WriteLine("a client closed ");
        }

        private bool CheckIfOnClose(byte[] data)
        {
            int opCode = data[0] & 15;
            if (opCode == 8)
                return true;

            return false;
        }

        private async Task<byte[]> readNetStreamInput()
        {
            byte[] input = new byte[4096];
            byte[] data;
            int length;

            length = await stream.ReadAsync(input, 0, input.Length);


            if (client.Available != 0)
            {
                Byte[] nextBytes = new Byte[client.Available];

                length += await stream.ReadAsync(nextBytes, 0, nextBytes.Length);


                data = new byte[length];
                input.CopyTo(data, 0);
                nextBytes.CopyTo(data, input.Length);
            }
            else
            {
                data = new byte[length];
                Array.Copy(input, data, length);
            }

            return data;
        }

        private byte[] acceptwebsocketrequest(string data)
        {
            return Encoding.UTF8.GetBytes("HTTP/1.1 101 Switching Protocols" + Environment.NewLine
                    + "Connection: Upgrade" + Environment.NewLine
                    + "Upgrade: websocket" + Environment.NewLine
                    + "Sec-WebSocket-Protocol: " + Constant.WebSocketPROTOCOL + Environment.NewLine
                    + "Sec-WebSocket-Accept: " + Convert.ToBase64String(
                        SHA1.Create().ComputeHash(
                            Encoding.UTF8.GetBytes(
                                new Regex("Sec-WebSocket-Key: (.*)").Match(data).Groups[1].Value.Trim() + "258EAFA5-E914-47DA-95CA-C5AB0DC85B11"
                            )
                        )
                    ) + Environment.NewLine
                    + Environment.NewLine);
        }

        private byte[] EncodeMessage(byte[] decoded)
        {
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

            return reply;
        }

        private void DecodeMessage(byte[] bytes, out byte[] decoded, out string message)
        {


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

            decoded = new Byte[messageData.Length];
            for (int i = 0; i < messageData.Length; i++)
            {
                decoded[i] = (Byte)(messageData[i] ^ masks[i % 4]);
            }
            message = Encoding.UTF8.GetString(decoded);

        }
    }
}
