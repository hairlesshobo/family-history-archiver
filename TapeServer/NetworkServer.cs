using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Archiver.Shared.Classes;
using Archiver.Shared.Interfaces;
using Archiver.TapeServer.Classes.Config;

namespace Archiver.TapeServer
{
    internal class NetworkServer
    {
        private TapeServerConfig config;
        private ITapeDrive tapeDrive;
        
        private IPAddress _listenAddress;

        private TcpListener _controlListener { get; set; }
        private bool controlAccept { get; set; } = false;

        public NetworkServer(TapeServerConfig ServerConfig, ITapeDrive TapeDrive)
        {
            config = ServerConfig;
            tapeDrive = TapeDrive;

            _listenAddress = IPAddress.Parse(config.ListenAddress);
        }

        public void StartControlServer()
        {
            _controlListener = new TcpListener(_listenAddress, config.Ports.Control);

            _controlListener.Start();
            controlAccept = true;

            Console.WriteLine($"Control server started. Listening to TCP clients at {_listenAddress.ToString()}:{config.Ports.Control}");

            ControlServerListen();
        }

        public void ControlServerListen()
        {
            if (_controlListener != null && controlAccept)
            {
                bool shutdown = false;

                // Continue listening.  
                while (!shutdown)
                {
                    Console.WriteLine("Waiting for client...");
                    var clientTask = _controlListener.AcceptTcpClientAsync(); // Get the client  

                    if (clientTask.Result != null)
                    {
                        Console.WriteLine("Client connected. Waiting for command.");
                        var client = clientTask.Result;

                        var stream = client.GetStream();
                        string data = null;
                        Byte[] bytes = new Byte[256];
                        int i;

                        bool stayConnected = true;
                        // try
                        // {
                            while (stayConnected && (i = stream.Read(bytes, 0, bytes.Length)) != 0)
                            {
                                // string hex = BitConverter.ToString(bytes);
                                data = Encoding.ASCII.GetString(bytes, 0, i);

                                if (!String.IsNullOrWhiteSpace(data))
                                {
                                    // TODO: Add exception handling
                                    RemoteTapeDriveCommand command = JsonSerializer.Deserialize<RemoteTapeDriveCommand>(data);

                                    Console.WriteLine($"Received command: {command.CommandType}");

                                    RemoteTapeDriveResponse response = new RemoteTapeDriveResponse();

                                    switch (command.CommandType.ToLower())
                                    {
                                        case "close":
                                            response.Success = true;
                                            stayConnected = false;
                                            break;

                                        case "gettapeinfo":
                                            response.Success = true;
                                            response.Object = tapeDrive.GetTapeInfo();
                                            break;

                                        case "preparedatawrite":
                                            response.Success = true;
                                            response.Value = new GenericValue(config.Ports.Stream);
                                            break;

                                        default:
                                            response.ErrorMessage = "Invalid command";
                                            break;
                                    }

                                    string replyString = JsonSerializer.Serialize(response, response.GetType(), new JsonSerializerOptions()
                                    {
                                        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
                                    });

                                    if (!replyString.EndsWith('\n'))
                                        replyString += '\n';

                                    Byte[] replyArray = System.Text.Encoding.ASCII.GetBytes(replyString);
                                    stream.Write(replyArray, 0, replyString.Length);
                                }
                            }
                        // }
                        // catch(Exception e)
                        // {
                        //     Console.WriteLine("Exception: {0}", e.ToString());
                        //     client.Close();
                        // }

                        Console.WriteLine("Closing connection.");
                        client.GetStream().Dispose();
                    }
                }
            }
        }
    }
}