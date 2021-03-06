﻿using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Windows.Threading;
using System.Runtime.Serialization.Formatters.Binary;
using Online_Chat.ViewModels;
using Online_Chat.Models;
using Online_Chat.Extensions;
using Online_Chat.Services;
using System.Collections.ObjectModel;
using ProtoBuf;


namespace Online_Chat.Server
{
    public class TCPServer
    {
        private TcpListener _server;
        private List<TcpClient> _clients;
        private ObservableCollection<User> _users;
        private ObservableCollection<Message> _messages;
        private INetworkService _networkService;
        public TcpListener Server => _server;
        public TCPServer(TcpListener listener, INetworkService networkService)
        {
            _networkService = networkService;
            _clients = new List<TcpClient>();
            _users = new ObservableCollection<User>();
            _messages = new ObservableCollection<Message>();
            _server = listener;
            _server.Start();
            Task.Run(ListenForConnections);
            Task.Run(LookForInActiveUsers);
            Task.Run(ReadData);
        }

        private void ListenForConnections()
        {
            while (true)
            {
                TcpClient client = _server.AcceptTcpClient();
                _clients.Add(client);
            }
        }

        private async Task LookForInActiveUsers()
        {
            while (true)
            {
                await Task.Delay(3000);
                var oldusersCount = _clients.Count;
                foreach (var client in _clients.ToList())
                {
                    try
                    {
                        using (NetworkStream stream = new NetworkStream(client.Client, false))
                        {
                            byte[] emptydata = { };
                            stream.Write(emptydata, 0, emptydata.Length);
                        }
                    }
                    catch (IOException x)
                    {
                        // Client is disconnected
                        var index = _clients.IndexOf(client);
                        _clients.RemoveAt(index);
                        _users.RemoveAt(index);
                    }
                }
                // Check whether something has been removed from the collecton.
                if (oldusersCount != _clients.Count)
                {
                    BroadCastData(new SerializationData(_users, null),
                    SerializationData.Collections.UserCollection);
                }
            }
        }
        private void ReadData()
        {
            while (true)
            {
                Parallel.ForEach(_clients.ToList(), async (client) =>
                {
                    try
                    {
                        SerializationData.Objects data = await _networkService.ReceiveDataAsync<SerializationData.Objects>(client);
                        if (data is SerializationData.Objects.User)
                        {
                            _users.Add(await _networkService.ReceiveDataAsync<User>(client));
                            BroadCastData(new SerializationData(_users, null),
                                SerializationData.Collections.UserCollection);
                        }
                        else if (data is SerializationData.Objects.Message)
                        {
                            _messages.Add(await _networkService.ReceiveDataAsync<Message>(client));
                            BroadCastData(new SerializationData(null, _messages),
                                SerializationData.Collections.MessageCollection);
                        }
                    }
                    catch (IOException e)
                    {
                        // Either client is not connected or data is not found. Continue.
                        return;
                    }                  
                });              
            }
        }
        private void BroadCastData(SerializationData data, SerializationData.Collections dataType)
        {
            foreach (var Client in _clients.ToList())
            {
                using (NetworkStream stream = new NetworkStream(Client.Client, false))
                {
                    Serializer.SerializeWithLengthPrefix(stream, dataType, PrefixStyle.Fixed32);
                    Serializer.SerializeWithLengthPrefix(stream, data, PrefixStyle.Fixed32);
                }
            }
        }
    }
}
