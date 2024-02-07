using System;
using chat_app_shared_c_;
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
    //TcpListener för att hantera inkommande anslutningar.
    static TcpListener? tcpListener;
    private static Dictionary<TcpClient, string> connectedUsers = new Dictionary<TcpClient, string>();
    private static IMongoCollection<User> collection;

    static void Main(string[] args)
    {
        //Anger att tcpListener ska lyssna efter alla nätverksgränssnitt på port 27500.
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

    //MongoDB anslutning.
    static void Mongodb()
    {
        const string databaseString = "mongodb://localhost:27017";
        MongoClient dbClient = new MongoClient(databaseString);
        var database = dbClient.GetDatabase("Users");
        collection = database.GetCollection<User>("users");
    }

    //Hanterar enskilda klientanslutningar.
    static void HandleClient(TcpClient client, NetworkStream stream)
    {
        //Hämtar nätverksströmmen från klienten för dataöverföring.
        //NetworkStream stream = client.GetStream();
        byte[] buffer = new byte[1024];

        try
        {
            int bytesRead;

            //Läser in data från klienten så länge det finns data att läsa.
            while ((bytesRead = stream.Read(buffer, 0, buffer.Length)) > 0)
            {
                //Konverterar den inkommande byte-arrayn till en sträng.
                string userData = Encoding.ASCII.GetString(buffer, 0, bytesRead);

                string[] dataParts = userData.Split('.');
                string dataType = dataParts[0];

                if (dataType == "REGISTER")
                {
                    Register(dataParts[1], stream);
                }
                else if (dataType == "LOGIN")
                {
                    LogIn(dataParts[1], stream, client);
                }
                else if (dataType == "PUBLIC_MESSAGE")
                {
                    PublicMessage(dataParts[1], stream, client);
                }
                else if (dataType == "LOGOUT")
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
            client.Close();
        }
    }

    //Hanterar registrering och sparar denna i MongoDB.
    static void Register(string registrationData, NetworkStream stream)
    {
        //Delar upp registreringsdatan i delar baserat på kommatecken.
        //Det urpsrungliga formatet ser ut så här $"{regUsername},{regPassword}".
        //Så arrayn får 2 platser, username på plats [0] och password på [1].
        string[] data = registrationData.Split(',');
        string reply = "";

        var maxUserId = collection.AsQueryable()
            .OrderByDescending(u => u.UserId)
            .Select(u => u.UserId)
            .FirstOrDefault();

        var user = new User
        {
            UserId = maxUserId + 1,
            Username = data[0],
            Password = data[1],
            Message = new List<string>()
        };

        collection.InsertOne(user);
        Console.WriteLine($"User {data[0]} registered.");
        reply = ($"User {data[0]} registered.");
        SendMessageToClient(stream, reply);
    }

    static void LogIn(string loginData, NetworkStream stream, TcpClient client)
    {
        string[] data = loginData.Split(',');
        string username = data[0];
        string password = data[1];
        string reply = "";
        var user = collection.Find(u => u.Username == username && u.Password == password)
            .Project(u => new { u.Username, u.Password, u.Message }) //Specify which properties that should be retrived
            .FirstOrDefault();

        if (user != null)
        {
            connectedUsers.Add(client, username);
            reply = ($"\nWelcome, {user.Username}!\n\nMessage history:\n");
            Console.WriteLine($"{username} has logged in.");
            SendMessageToClient(stream, reply);

            int messagesToPrint = Math.Min(30, user.Message.Count);
            int counter = 1;
            for (int i = user.Message.Count - messagesToPrint; i < user.Message.Count; i++)
            {
                SendMessageToClient(stream, $"\n{counter}. {user.Message[i]}");
                counter++;
            }

            foreach (var otherClient in connectedUsers.Where(x => x.Key != client))
            {
                SendMessageToClient(otherClient.Key.GetStream(), $"{username} has logged in");
            }
        }
        else
        {
            Console.WriteLine("Invalid username or password");
            reply = "There is no such user in the database. Please try again.";
            SendMessageToClient(stream, reply);
        }
    }

    static void SendMessageToClient(NetworkStream stream, string data)
    {
        byte[] buffer = Encoding.ASCII.GetBytes(data);
        stream.Write(buffer, 0, buffer.Length);
    }

    static void RemoveClient(string username, NetworkStream stream, TcpClient client)
    {
        if (connectedUsers.ContainsValue(username))
        {
            // Remove the client using the username
            var clientToRemove = connectedUsers.FirstOrDefault(x => x.Value == username).Key;
            connectedUsers.Remove(clientToRemove);
            Console.WriteLine($"{username} has logged out.");

            foreach (var otherClient in connectedUsers.Where(x => x.Key != client))
            {
                SendMessageToClient(otherClient.Key.GetStream(), $"{username} has logged out");
            }
        }
    }

    static void PublicMessage(string messageData, NetworkStream stream, TcpClient client)
    {
        string[] data = messageData.Split(',');
        string username = data[0];
        string message = data[1];
        
        SavePublicMessage(username, message);

        Console.WriteLine("Message saved successfully.");

        foreach (var otherClient in connectedUsers.Where(x => x.Key != client))
        {
            SendMessageToClient(otherClient.Key.GetStream(), $"{username}: {message}");
        }
    }

    static void SavePublicMessage(string senderUsername, string message)
    {
        var filter = Builders<User>.Filter.Empty;
        var update = Builders<User>.Update.Push(u => u.Message, $"{senderUsername} (Public): {message}");
        collection.UpdateMany(filter, update);
        Console.WriteLine("Message saved successfully to all users.");
    }

    //static void SavePrivateMessage(string messageData, NetworkStream stream)
    //{
    //    string[] data = messageData.Split(',');
    //    string username = data[0];
    //    string message = data[1];
    //    string replyMessage = "";

    //    var filter = Builders<User>.Filter.Eq(u => u.Username, username);
    //    var user = collection.Find(filter).FirstOrDefault();
    //    if (user != null)
    //    {
    //        var update = Builders<User>.Update.Push(u => u.Message, message);
    //        collection.UpdateOne(filter, update);
    //        Console.WriteLine("Message saved successfully.");
    //        replyMessage = "Message saved successfully.\n";
    //        SendMessageToClient(stream, replyMessage);
    //    }
    //    else
    //    {
    //        Console.WriteLine($"{stream} User not found.");
    //        replyMessage = "User not found.";
    //        SendMessageToClient(stream, replyMessage);
    //    }
    //}
}

