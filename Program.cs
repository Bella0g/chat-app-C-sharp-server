using System;
using chat_app_shared_c_;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver;
using static System.Runtime.InteropServices.JavaScript.JSType;


namespace chat_server_c
{
    class Program
    {
        static void Main()

        {
            const string clientString = "mongodb://localhost:27017";//Connection string
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


            }
        }

    }
}