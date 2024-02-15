using System.Buffers;
using System.Net;
using System.Net.Sockets;
using System.Text;

TcpListener server = new TcpListener(IPAddress.Any, 6379);
server.Start();
var pong = Encoding.UTF8.GetBytes("+PONG\r\n");

Boolean KeepAlive = true;

while (KeepAlive) {
	Socket socket = await server.AcceptSocketAsync(); // wait for client
	var res = ProcessSocket(socket);
}



async Task<string?> ProcessSocket(Socket socket) {
	using var _ = socket;

	while (true) {
		var result = await ReceiveAsync(socket);
		if(result == 0) {
			return null;
		}
		socket.Send(pong);
	}
}

async Task<int> ReceiveAsync(Socket socket) {
	byte[]? buffer = null;
	try {
		buffer = ArrayPool<byte>.Shared.Rent(1024);
		var result = await socket.ReceiveAsync(buffer, SocketFlags.None);
		return result;
	} finally {
		if (buffer != null) {
			ArrayPool<byte>.Shared.Return(buffer);
		}
	}
}
