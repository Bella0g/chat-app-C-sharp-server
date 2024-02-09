using System;
using Users;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Bson;
using MongoDB.Driver;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Collections.Generic;
using System.IO;
using System.Xml.Linq;


namespace chat_server_c;

class Server
{
    private static TcpListener? tcpListener;
    private static Dictionary<TcpClient, string> connectedUsers = new Dictionary<TcpClient, string>();
    private static IMongoCollection<User> collection;

    static void Main(string[] args)
    {
        tcpListener = new TcpListener(IPAddress.Any, 27500);
        tcpListener.Start();
        Console.WriteLine("Server is listening on port 27500");
        Mongodb();

        while (true)
        {
            if (tcpListener.Pending())
            {
                TcpClient client = tcpListener.AcceptTcpClient();
                Console.WriteLine("Client has connected");
                NetworkStream stream = client.GetStream();
                Thread clientThread = new Thread(() => HandleClient(client, stream));
                clientThread.Start();
            }
            Thread.Sleep(100);
        }
    }

    private static void Mongodb()
    {
        const string databaseString = "mongodb://localhost:27017";
        MongoClient dbClient = new MongoClient(databaseString);
        var database = dbClient.GetDatabase("Users");
        collection = database.GetCollection<User>("users");
    }

    private static void HandleClient(TcpClient client, NetworkStream stream)
    {
        byte[] buffer = new byte[1024];

        try
        {
            int bytesRead;

            while ((bytesRead = stream.Read(buffer, 0, buffer.Length)) > 0)
            {
                string userData = Encoding.ASCII.GetString(buffer, 0, bytesRead);
                string[] dataParts = userData.Split('.');
                string messageType = dataParts[0];

                if (messageType == "REGISTER")
                {
                    Register(dataParts[1], stream);
                }
                else if (messageType == "LOGIN")
                {
                    LogIn(dataParts[1], stream, client);
                }
                else if (messageType == "PUBLIC_MESSAGE")
                {
                    PublicMessage(dataParts[1], stream, client);
                }
                else if (messageType == "PRIVATE_MESSAGE")
                {
                    PrivateMessage(dataParts[1], stream, client);
                }
                else if (messageType == "LOGOUT")
                {
                    RemoveClient(dataParts[1], stream, client);
                }
            }
        }
        catch (Exception e)
        {
            //Om något blir fel printas ett meddelande ut. HÄR BLIR NÅGOT FEL
            Console.WriteLine($"Error: {e.Message}");
        }
        finally
        {
            //Stänger anslutningen till clienten.
            connectedUsers.Remove(client);
            client.Close();
        }
    }

    private static void Register(string registrationData, NetworkStream stream)
    {
        string[] data = registrationData.Split(',');
        string username = data[0];
        string password = data[1];

        var user = new User
        {
            Username = username,
            Password = password,
            Message = new List<string>()
        };

        collection.InsertOne(user);
        Console.WriteLine($"User {username} registered.");
        string reply = ($"User {username} registered.");
        SendMessageToClient(stream, reply);
    }

    private static void LogIn(string loginData, NetworkStream stream, TcpClient client)
    {
        string[] data = loginData.Split(',');
        string username = data[0];
        string password = data[1];
        string reply;

        var user = collection.Find(u => u.Username == username && u.Password == password)
            .Project(u => new { u.Username, u.Password, u.Message })
            .FirstOrDefault();

        if (user != null)
        {
            connectedUsers.Add(client, username);
            Console.WriteLine($"{username} has logged in!");

            int messagesToPrint = Math.Min(30, user.Message.Count);
            int counter = 1;
            reply = $"\nWelcome, {user.Username}!\n\nMessage history:\n";

            for (int i = user.Message.Count - messagesToPrint; i < user.Message.Count; i++)
            {
                reply += $"{counter}. {user.Message[i]}\n";
                counter++;
            }

            SendMessageToClient(stream, reply);

            foreach (var otherClient in connectedUsers.Where(x => x.Key != client))
            {
                SendMessageToClient(otherClient.Key.GetStream(), $"{username} has logged in!");
            }
        }
        else
        {
            Console.WriteLine("Invalid username or password");
            reply = "There is no such user in the database. Please try again.";
            SendMessageToClient(stream, reply);
        }
    }

    private static void SendMessageToClient(NetworkStream stream, string data)
    {
        byte[] buffer = Encoding.ASCII.GetBytes(data);
        stream.Write(buffer, 0, buffer.Length);
    }

    private static void RemoveClient(string username, NetworkStream stream, TcpClient client)
    {
        if (connectedUsers.ContainsValue(username))
        {
            var clientToRemove = connectedUsers.FirstOrDefault(x => x.Value == username).Key;
            connectedUsers.Remove(clientToRemove);
            Console.WriteLine($"{username} has logged out.");

            foreach (var otherClient in connectedUsers.Where(x => x.Key != client))
            {
                SendMessageToClient(otherClient.Key.GetStream(), $"{username} has logged out!");
            }
        }
    }

    private static void PublicMessage(string messageData, NetworkStream stream, TcpClient client)
    {
        string[] data = messageData.Split(',');
        string username = data[0];
        string message = data[1];

        SavePublicMessage(username, message);

        foreach (var otherClient in connectedUsers.Where(x => x.Key != client))
        {
            SendMessageToClient(otherClient.Key.GetStream(), $"{username}: {message}");
        }
    }

    private static void SavePublicMessage(string senderUsername, string message)
    {
        foreach (var userEntry in connectedUsers)
        {
            var userName = userEntry.Value;
            var filter = Builders<User>.Filter.Eq(u => u.Username, userName);
            var update = Builders<User>.Update.Push(u => u.Message, $"{senderUsername} (Public): {message}");
            collection.UpdateOne(filter, update);
        }

        Console.WriteLine("Message saved successfully to all connected users.");
    }

    private static void PrivateMessage(string messageData, NetworkStream stream, TcpClient client)
    {
        string[] data = messageData.Split(',');
        string username = data[0];
        string receiver = data[1];
        string message = data[2];
        string replyMessage = "";


        var uFilter = Builders<User>.Filter.Eq(u => u.Username, username);
        var rFilter = Builders<User>.Filter.Eq(u => u.Username, receiver);
        var user = collection.Find(uFilter).FirstOrDefault();
        var receiverU = collection.Find(rFilter).FirstOrDefault();
        if (receiverU != null)
        {
            var update = Builders<User>.Update.Push(u => u.Message, $"{username} (Private): {message}");

            collection.UpdateOne(uFilter, update);
            collection.UpdateOne(rFilter, update);
            Console.WriteLine("Message saved successfully.");

            var receiverUser = connectedUsers.FirstOrDefault(x => x.Value == receiver);
            if (receiverUser.Key != null)
            {
                SendMessageToClient(receiverUser.Key.GetStream(), $"{username}: {message}");
            }
        }
        else
        {
            Console.WriteLine($"{stream} User not found.");
            replyMessage = "User not found.";
            SendMessageToClient(stream, replyMessage);
        }
    }
}