using System.Net;
using System.Net.Sockets;
using System.Text;

TcpListener server = new TcpListener(IPAddress.Any, 6379);
server.Start();

//Create a buffer

Socket client = server.AcceptSocket(); // wait for client
Boolean KeepAlive = true;

while (KeepAlive) {
	if(server.Pending()) {
		Console.WriteLine("Client Waiting");
	}
	byte[] buffer = new byte[1024];
	SocketFlags flags = SocketFlags.Peek;
	var rBytes = client.ReceiveAsync(buffer, flags); // receive data from client
	Console.WriteLine("Received: " + Encoding.UTF8.GetString(buffer));
	byte[] res = HandleRequest(buffer);
	client.Send(res);
}

//Create a function that returns an integer and takes in an array of bytes
byte[] HandleRequest(byte[] buffer) {
	byte[] res = Encoding.UTF8.GetBytes("+PONG\r\n");
	return res;
	// return Encoding.UTF8.GetBytes("Error");
}
