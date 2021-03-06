﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace FTPOverSocket.Service
{
    class SocketService
    {
        private const int BUFFER_SIZE = 4096;
        private string address;
        private Socket socket;
        private static SocketService service;
        public static SocketService getInstance()
        {
            if (service == null)
            {
                service = new SocketService();
            }
            return service;
        }

        public SocketService()
        {

        }

        public bool Connect(string address, int port)
        {
            this.address = address;
            this.socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            try
            {
                this.socket.Connect(new IPEndPoint(IPAddress.Parse(address), port));
                return true;
            }
            catch (Exception)
            {
                return false;
                throw;
            }
        }

        public string Check()
        {
            socket.Send(Encoding.UTF8.GetBytes("{\"action\":\"check\"}"));
            byte[] bytes = new byte[BUFFER_SIZE];
            int size = socket.Receive(bytes, bytes.Length, 0);
            string response = Encoding.UTF8.GetString(bytes, 0, size);
            return response;
        }

        public string[] Dir()
        {
            socket.Send(Encoding.UTF8.GetBytes("{\"action\":\"dir\"}"));
            byte[] bytes = new byte[BUFFER_SIZE];
            int size = socket.Receive(bytes, bytes.Length, 0);
            string response = Encoding.UTF8.GetString(bytes, 0, size);
            string[] names = response.Split('\n');
            return names;
        }

        public void Close()
        {
            try
            {
                socket.Send(Encoding.UTF8.GetBytes("{\"action\":\"quit\"}"));
            }
            catch (Exception)
            {
                throw;
            }
            socket.Close();
            
        }

        public bool Login(string username, string password)
        {
            socket.Send(Encoding.UTF8.GetBytes(username + "??" + password));
            byte[] bytes = new byte[BUFFER_SIZE];
            int size = socket.Receive(bytes, bytes.Length, 0);
            string response = Encoding.UTF8.GetString(bytes, 0, size);
            if (response.Equals("LOGIN_SUCCESS"))
            {
                return true;
            } else
            {
                return false;
            }
        }

        public void Get(string filename, ProgressWindow pw)
        {
            socket.Send(Encoding.UTF8.GetBytes("{\"action\":\"get\",\"filename\":\"" + filename + "\"}"));
            if (File.Exists(@".\files\" + filename))
                File.Delete(@".\files\" + filename);
            FileStream fs = File.Create(@".\files\" + filename);
            byte[] bytess = new byte[9];
            int sizee = socket.Receive(bytess, bytess.Length, 0);
            string response = Encoding.UTF8.GetString(bytess, 0, sizee);
            int fileSize = int.Parse(response);
            using (BinaryWriter writer = new BinaryWriter(fs))
            {
                pw.pb.Maximum = fileSize;
                pw.pb.Value = 0;
                pw.DoEvents();
                int count = 0;
                DateTime lastTime = DateTime.Now;
                double lastPos = 0;
                byte[] bytes = new byte[BUFFER_SIZE];
                while (true)
                {
                    count += 1;
                    if (count >= 200)
                    {
                        DateTime nowTime = DateTime.Now;
                        double diff = (nowTime - lastTime).TotalSeconds;
                        double pos = pw.pb.Value;
                        pw.labelSpeed.Content = ((pos - lastPos) / 1024 / diff).ToString("#0.000") + "KB/s";
                        lastTime = nowTime;
                        lastPos = pos;
                        count = 1;
                    }
                    int size = socket.Receive(bytes, bytes.Length, 0);
                    count += 4;
                    Console.WriteLine(count);
                    writer.Write(bytes, 0, size);
                    pw.pb.Value += size;
                    pw.labelStatus.Content = (pw.pb.Value / 1024).ToString("#0") + "/" + (fileSize/1024).ToString() + "KB";
                    pw.DoEvents();
                    if (size < BUFFER_SIZE)
                    {
                        break;
                    }
                }
                writer.Close();
            }
            fs.Close();
        }

        public void Upload(string filepath, string filename, ProgressWindow pw)
        {
            socket.Send(Encoding.UTF8.GetBytes("{\"action\":\"upload\",\"filename\":\"" + filename + "\"}"));
            FileStream fs = File.OpenRead(filepath);
            using (BinaryReader reader = new BinaryReader(fs))
            {
                pw.pb.Maximum = 100;
                pw.pb.Value = 0;
                pw.DoEvents();
                int count = 0;
                DateTime lastTime = DateTime.Now;
                long lastPos = 0;
                byte[] bytes = new byte[BUFFER_SIZE];
                while (reader.BaseStream.Position < reader.BaseStream.Length)
                {
                    count += 1;
                    if (count >= 200)
                    {
                        DateTime nowTime = DateTime.Now;
                        double diff = (nowTime - lastTime).TotalSeconds;
                        long pos = reader.BaseStream.Position;
                        pw.labelSpeed.Content = ((pos - lastPos) / diff / 1024).ToString("#0.000") + "KB/s";
                        lastTime = nowTime;
                        lastPos = pos;
                        count = 1;
                    }
                    
                    int size = reader.Read(bytes, 0, BUFFER_SIZE);
                    byte[] toSend = new byte[size];
                    for (int i = 0; i < size; i++)
                    {
                        toSend[i] = bytes[i];
                    }
                    this.socket.Send(toSend);

                    pw.pb.Value = (int)((double)reader.BaseStream.Position / reader.BaseStream.Length * 100);
                    pw.labelStatus.Content = (reader.BaseStream.Position/1024).ToString("#0") + "/" + (reader.BaseStream.Length/1024).ToString("0") + "KB";
                    pw.DoEvents();
                }
                System.Threading.Thread.Sleep(500);
                this.socket.Send(Encoding.UTF8.GetBytes("DONE"));
                reader.Close();
            }
            fs.Close();
        }

        public string[] Detail(string filename)
        {
            socket.Send(Encoding.UTF8.GetBytes("{\"action\":\"detail\",\"filename\":\"" + filename + "\"}"));
            byte[] bytes = new byte[BUFFER_SIZE];
            int size = socket.Receive(bytes, bytes.Length, 0);
            string response = Encoding.UTF8.GetString(bytes, 0, size);
            return response.Split('?');
        }

        public bool Remove(string filename)
        {
            socket.Send(Encoding.UTF8.GetBytes("{\"action\":\"remove\",\"filename\":\"" + filename + "\"}"));
            byte[] bytes = new byte[BUFFER_SIZE];
            int size = socket.Receive(bytes, bytes.Length, 0);
            string response = Encoding.UTF8.GetString(bytes, 0, size);
            if (response.Equals("OK"))
                return true;
            else
                return false;
        }
    }
}
