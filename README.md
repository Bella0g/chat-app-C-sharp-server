# C# chat app server

Welcome to our real-time chat application built using C# and MongoDB.

![Alt text](/assets/)
![Alt text](/assets/)

## Features

- Key Bindings: Intuitively designed key bindings for a seamless user experience.
- Real-time Messaging: Experience instant communication with real-time messaging capabilities.
- In-Terminal Chat: Enjoy the convenience of chatting right from your terminal.


## Technologies Used

- C#: The core language driving the functionality of our chat application.
- MongoDB: Our chosen database for efficient and scalable data storage.

# Todo:

## Serverapplikation:

### User Manager:

- Hantera registrering och inloggning av användare.
- Spara användarinformation i databasen.
- Informera andra användare när en ny användare registrerar sig eller loggar in.

### Message Manager:

- Hantera globala och privata meddelanden.
- Spara meddelanden i en databas.
- Ge möjlighet att hämta historiken för användaren.

### Socket Manager:

- Hantera inkommande och utgående anslutningar via TCP/IP-sockets.
- Skicka och ta emot meddelanden från klienterna.

### Main Server:

- Samordna olika komponenter och starta upp servern.

## Klientapplikation:

#### User Interface:

- Skapa ett användarvänligt gränssnitt för att hantera inloggning, registrering och meddelanden. // Bella

### Socket Manager:

- Hantera anslutning till servern via TCP/IP-sockets. // Marcus
- Skicka och ta emot meddelanden från servern.

### Message Handler:

- Visa globala och privata meddelanden.
- Hantera inkommande och utgående meddelanden.
- Möjliggör skickande av privata meddelanden.
- Visa historiken för användaren vid inloggning.

## Databasintegration:

- Använd en databas (till exempel MongoDB) för att lagra användarinformation och meddelanden. // Sandra
- Skapa kopplingar och metoder för att spara och hämta data från databasen.
