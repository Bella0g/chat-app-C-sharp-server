using System;
using chat_app_shared_c_;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Collections.Generic;
using System.IO;


namespace chat_server_c
{
    class Program
    {
        //TcpListener för att hantera inkommande anslutningar.
        static TcpListener? tcpListener;

        static void Main()
        {

            //Anger att tcpListener ska lyssna efter alla nätverksgränssnitt på port 27500.
            tcpListener = new TcpListener(IPAddress.Any, 27500);


            //CancellationToken instans för att se till att threads avslutas när programmet avslutas.
            CancellationTokenSource cts = new CancellationTokenSource();

            try
            {
                //Startar TcpListener och printar ut att den lyssnar på den angivna porten.
                tcpListener.Start();
                Console.WriteLine("Server is listening on port 27500");

                //Väntar på inkommande anslutningar, hanteras i en oändlig loop.
                while (true)
                {
                    //Accepterar en inkommande TcpClient-anslutning.
                    TcpClient client = tcpListener.AcceptTcpClient();
                    //Skapar en separat thread för att hantera klienten och kunna fortsätta lyssna efter nya anslutningar.
                    Thread clientThread = new Thread(() => HandleClient(client));
                    clientThread.Start();
                }
            }
            catch (Exception e)
            {
                //Om något blir fel printas ett meddelande ut.
                Console.WriteLine($"Error:{e.Message}");
            }
            finally
            {
                //Stänger av TcpListener och avbryter cts (threads) när programmet avslutas.
                tcpListener.Stop();
                cts.Cancel();
            }
        }

        //Metod för att hantera en enskild klientanslutning.
        static void HandleClient(TcpClient client)
        {
            //Hämtar nätverksströmmen från klienten för dataöverföring.
            NetworkStream stream = client.GetStream();
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
                        LogIn(dataParts[1], stream);
                    }
                    else if (dataType == "MESSAGE")
                    {
                        SaveMessage(dataParts[1], stream);
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

                var user = new User
                {
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

        static void LogIn(string loginData, NetworkStream stream)
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
                .Project(u => new { u.Username, u.Password })
                .FirstOrDefault();
            {
                if (user != null)
                {
                    Console.WriteLine($"Welcome, {user.Username}!"); // tabort
                    reply = ($"Welcome, {user.Username}!");
                    byte[] replyBuffer = Encoding.ASCII.GetBytes(reply);
                    stream.Write(replyBuffer, 0, replyBuffer.Length);

                }
                else
                {
                    Console.WriteLine("Invalid username or password");
                    reply = "Invalid username or password!";
                    byte[] replyBuffer = Encoding.ASCII.GetBytes(reply);
                    stream.Write(replyBuffer, 0, replyBuffer.Length);
                }
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
            var user = collection.Find(u => u.Username == username);
                if(user != null)
            {

            }
                
            
        }
    }

}

/*const string clientString = "mongodb://localhost:27017";//Connection string
MongoClient dbClient = new MongoClient(clientString); //Creating a MongoDB client using the clientString to localhost
var database = dbClient.GetDatabase("Users"); //Access the database Users in MongoDB
var collection = database.GetCollection<User>("users");//Reference the users collection on the database


//Declaring variable username to store the entered username
string username;

//Using a do-while loop to reapeatedly prompt the user until a valid username is entered
do
{
    Console.WriteLine("Enter username: "); //Asking the user to enter username using the terminal
    username = Console.ReadLine()!; //Read the input for username from the terminal

    if (string.IsNullOrWhiteSpace(username)) //Checking if the username string is null or whitespace
    {
        Console.WriteLine("Error: You need to enter a username."); //Error message for empty input
    }
} while (string.IsNullOrWhiteSpace(username)); //Condition for the loop to continue to execute as long as the username is null or a empty string


//Declaring variable password to store the entered password
string password;

//Using a do-while loop to reapeatedly prompt the user until a valid password is entered
do
{
    Console.WriteLine("Enter password: "); //Asking the user to enter password using the terminal
    password = Console.ReadLine()!; //Read the input for password from the terminal

    if (string.IsNullOrWhiteSpace(password)) //Checking if the password string is null or whitespace
    {
        Console.WriteLine("Error: You need to enter a password."); //Error message for empty input
    }
} while (string.IsNullOrWhiteSpace(password)); //Condition for the loop to continue to execute as long as the password is null or whitespaced

if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password)) //Checks if either the username or password is null or whitespaced.
{
    Console.WriteLine("Error: You need to enter a password and/or username."); //Error message if the If statement is true.
}
else

{
    //Creating a new User using the entered username and password
    var user = new User
    {
        Username = username,
        Password = password
    };

    //Inserts the user to the MongoDB collection 
    collection.InsertOne(user);

    Console.WriteLine("User is now registered!");
}*/