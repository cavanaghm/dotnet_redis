using System.Net;
using System.Net.Sockets;
using System.Text;

TcpListener server = new TcpListener(IPAddress.Any, 6379);
server.Start();
Socket client = server.AcceptSocket(); // wait for client

//Create a buffer
byte[] buffer = new byte[1000];
client.Receive(buffer); // receive data from client
Console.WriteLine("Received: " + Encoding.UTF8.GetString(buffer));
client.Send(Encoding.UTF8.GetBytes("+PONG\r\n")); // send data to client
