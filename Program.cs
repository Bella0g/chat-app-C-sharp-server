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


namespace chat_server_c
{
    class Program
    {
        //TcpListener för att hantera inkommande anslutningar.
        static TcpListener? tcpListener;
        private static Dictionary<TcpClient, string> connectedUsers = new Dictionary<TcpClient, string>();


        static void Main(string[] args)
        {

            //Anger att tcpListener ska lyssna efter alla nätverksgränssnitt på port 27500.
            tcpListener = new TcpListener(IPAddress.Any, 27500);
            tcpListener.Start();
            Console.WriteLine("Server is listening on port 27500");

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

            //Metod för att hantera en enskild klientanslutning.
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
                            ProcessRegistrationData(dataParts[1], stream);
                        }
                        else if (dataType == "LOGIN")
                        {
                            LogIn(dataParts[1], stream, client);
                        }
                        else if (dataType == "MESSAGE")
                        {
                            SaveMessage(dataParts[1], stream);
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

                    //foreach (var entry in ConnectedUsers)
                    //{
                    //    if (entry.Value == stream)
                    //    {
                    //        connectedUsers.Remove(client);
                    //        break;
                    //    }
                    //}

                    //Stänger anslutningen till clienten.
                    client.Close();
                }

            }

            //Metod som sköter hanteringen av registreringsdata, sparar denna till databasen.
            static void ProcessRegistrationData(string registrationData, NetworkStream stream)
            {
                //Delar upp registreringsdatan i delar baserat på kommatecken.
                //Det urpsrungliga formatet ser ut så här $"{regUsername},{regPassword}".
                //Så arrayn får 2 platser, username på plats [0] och password på [1].
                string[] data = registrationData.Split(',');
                string reply = "";
                //Kontrollerar registreringsdatan i isValidString och kontrollerar att den är i 2 delar.
                if (IsValidString(data[0]) && IsValidString(data[1]) && data.Length == 2)
                {
                    //Ansluter till MongoDB och lägger till användaren i databasen.
                    const string databaseString = "mongodb://localhost:27017";
                    MongoClient dbClient = new MongoClient(databaseString);
                    var database = dbClient.GetDatabase("Users");
                    var collection = database.GetCollection<User>("users");

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
                    byte[] replyBuffer = Encoding.ASCII.GetBytes(reply);
                    stream.Write(replyBuffer, 0, replyBuffer.Length);

                }
                else
                {
                    //Skriv ut felmeddelande om registreringsdatan är ogiltig.
                    Console.WriteLine("Invalid registration data or format.");
                    reply = "Invalid registration data or format.";
                    byte[] replyBuffer = Encoding.ASCII.GetBytes(reply);
                    stream.Write(replyBuffer, 0, replyBuffer.Length);
                }

                //Testar så att registreringsdatan inte är tom eller innehåller mellanslag eller kommatecken.
                bool IsValidString(string str)
                {
                    return !string.IsNullOrWhiteSpace(str) && !str.Contains(" ") && !str.Contains(",");
                }
            }

            static void LogIn(string loginData, NetworkStream stream, TcpClient client)
            {
                const string databaseString = "mongodb://localhost:27017";
                MongoClient dbClient = new MongoClient(databaseString);
                var database = dbClient.GetDatabase("Users");
                var collection = database.GetCollection<User>("users");

                string[] data = loginData.Split(',');
                string username = data[0];
                string password = data[1];
                string reply = "";
                var user = collection.Find(u => u.Username == username && u.Password == password)
                    .Project(u => new { u.Username, u.Password, u.Message }) //Specify which properties that should be retrived
                    .FirstOrDefault();

                connectedUsers.Add(client, username);
                
                if (user != null)
                {
                    BroadcastMessage($"BROADCAST.Users online:");
                    foreach (var name in connectedUsers)
                    {
                        Console.WriteLine($"{name.Value} is now online.");
                        BroadcastMessage($"{name.Value}\n");
                    }

                    reply = ($"\nWelcome, {user.Username}!\n\nMessage history:\n");
                    byte[] replyBuffer = Encoding.ASCII.GetBytes(reply);
                    stream.Write(replyBuffer, 0, replyBuffer.Length);

                    foreach (var userMessage in user.Message) //Iterates through Messages property(the list) of the user
                    {
                        SendMessageToClient(stream, $"\n{username}: {userMessage}"); //Uses SendToClient method to send messages to client
                    }
                }

                else
                {
                    Console.WriteLine("Invalid username or password");
                    reply = "Invalid username or password!";
                    byte[] replyBuffer = Encoding.ASCII.GetBytes(reply);
                    stream.Write(replyBuffer, 0, replyBuffer.Length);
                }


            }

            static void SaveMessage(string messageData, NetworkStream stream)
            {
                const string databaseString = "mongodb://localhost:27017";
                MongoClient dbClient = new MongoClient(databaseString);
                var database = dbClient.GetDatabase("Users");
                var collection = database.GetCollection<User>("users");

                string[] data = messageData.Split(',');
                string username = data[0];
                string message = data[1];

                var filter = Builders<User>.Filter.Eq(u => u.Username, username);
                var user = collection.Find(filter).FirstOrDefault();
                if (user != null)
                {
                    var update = Builders<User>.Update.Push(u => u.Message, message);
                    collection.UpdateOne(filter, update);
                    Console.WriteLine("Message saved successfully.");
                }
                else { Console.WriteLine("User not found."); }


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
                    BroadcastMessage($"BROADCAST.{username} is now offline.\n");

                }
                foreach (var clientUsers in connectedUsers) //Iterates through Messages property(the list) of the user
                {
                    Console.WriteLine(clientUsers); //Uses SendToClient method to send messages to client
                }
            }
            static void BroadcastMessage(string message)
            {
                foreach (var userStream in connectedUsers.Keys)
                {
                    NetworkStream stream = userStream.GetStream();
                    byte[] buffer = Encoding.ASCII.GetBytes(message);
                    stream.Write(buffer, 0, buffer.Length);
                }
            }

        }
    }
}
//    //CancellationToken instans för att se till att threads avslutas när programmet avslutas.
//    CancellationTokenSource cts = new CancellationTokenSource();

//    try
//    {
//        //Startar TcpListener och printar ut att den lyssnar på den angivna porten.
//        tcpListener.Start();
//        Console.WriteLine("Server is listening on port 27500");

//        //Väntar på inkommande anslutningar, hanteras i en oändlig loop.
//        while (true)
//        {
//            //Accepterar en inkommande TcpClient-anslutning.
//            TcpClient client = tcpListener.AcceptTcpClient();
//            //Skapar en separat thread för att hantera klienten och kunna fortsätta lyssna efter nya anslutningar.
//            Thread clientThread = new Thread(() => HandleClient(client));
//            clientThread.Start();
//        }
//    }
//    catch (Exception e)
//    {
//        //Om något blir fel printas ett meddelande ut.
//        Console.WriteLine($"Error:{e.Message}");
//    }
//    finally
//    {
//        //Stänger av TcpListener och avbryter cts (threads) när programmet avslutas.
//        tcpListener.Stop();
//        cts.Cancel();
//    }
//}