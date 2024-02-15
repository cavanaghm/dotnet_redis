using System.Net;
using System.Net.Sockets;
using System.Text;

TcpListener server = new TcpListener(IPAddress.Any, 6379);
server.Start();
server.AcceptSocket(); // wait for client

// Accept incoming connection
TcpClient client = server.AcceptTcpClient();
NetworkStream stream = client.GetStream();
byte[] buffer = new byte[client.ReceiveBufferSize];
int bytesRead = stream.Read(buffer, 0, client.ReceiveBufferSize);
string dataReceived = Encoding.ASCII.GetString(buffer, 0, bytesRead);
Console.WriteLine(dataReceived);
