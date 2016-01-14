using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

// State object for reading client data asynchronously
public class StateObject
{
    // Client  socket.
    public Socket workSocket = null;
    // Size of receive buffer.
    public const int BufferSize = 1024;
    // Receive buffer.
    public byte[] buffer = new byte[BufferSize];
    // Received data string.
    public StringBuilder sb = new StringBuilder();
}

public class AsynchronousSocketListener
{
    // Thread signal.
    public static ManualResetEvent allDone = new ManualResetEvent(false);
    public static string UID = string.Empty;
    public static bool hasFlag = true;
    public static string IP = string.Empty;
    public static string flagValue = "aaaa";
    public static string flagID = string.Empty;

    public AsynchronousSocketListener()
    {
    }

    public static void StartListening()
    {
        // Data buffer for incoming data.
        byte[] bytes = new Byte[1024];

        // Establish the local endpoint for the socket.
        // The DNS name of the computer

        IPAddress ipAddress = IPAddress.Parse("188.26.11.125"); //ipHostInfo.AddressList[0];
        IPEndPoint localEndPoint = new IPEndPoint(ipAddress, 80);

        // Create a TCP/IP socket.
        Socket listener = new Socket(AddressFamily.InterNetwork,
            SocketType.Stream, ProtocolType.Tcp);

        // Bind the socket to the local endpoint and listen for incoming connections.
        try
        {
            listener.Bind(localEndPoint);
            listener.Listen(100);

            while (true)
            {
                // Set the event to nonsignaled state.
                allDone.Reset();

                // Start an asynchronous socket to listen for connections.
                Console.WriteLine("Waiting for a connection...");
                listener.BeginAccept(
                    new AsyncCallback(AcceptCallback),
                    listener);

                // Wait until a connection is made before continuing.
                allDone.WaitOne();
            }

        }
        catch (Exception e)
        {
            Console.WriteLine(e.ToString());
        }

        Console.WriteLine("\nPress ENTER to continue...");
        Console.Read();

    }

    public static void AcceptCallback(IAsyncResult ar)
    {
        // Signal the main thread to continue.
        allDone.Set();

        // Get the socket that handles the client request.
        Socket listener = (Socket)ar.AsyncState;
        Socket handler = listener.EndAccept(ar);

        // Create the state object.
        StateObject state = new StateObject();
        state.workSocket = handler;
        handler.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0,
            new AsyncCallback(ReadCallback), state);
    }

    public static void ReadCallback(IAsyncResult ar)
    {
        String content = String.Empty;

        // Retrieve the state object and the handler socket
        // from the asynchronous state object.
        StateObject state = (StateObject)ar.AsyncState;
        Socket handler = state.workSocket;

        // Read data from the client socket. 
        int bytesRead = handler.EndReceive(ar);

        if (bytesRead > 0)
        {
            // There  might be more data, so store the data received so far.
            state.sb.Append(Encoding.ASCII.GetString(
                state.buffer, 0, bytesRead));

            // Check for end-of-file tag. If it is not there, read 
            // more data.
            content = state.sb.ToString();
            if (content.Equals("who_are_you?"))
            {
                Console.WriteLine("Read {0} bytes from socket. \n Data : {1}",
                        content.Length, content);
                Send(handler, UID);
            }
            else if (content.Equals("next_server"))
            {
                Console.WriteLine("Read {0} bytes from socket. \n Data : {1}",
                    content.Length, content);
                generateRandomIP();
                Send(handler, IP);
            }
            else if (content.Equals("have_flag?"))
            {
                Console.WriteLine("Read {0} bytes from socket. \n Data : {1}",
                   content.Length, content);
                string response = string.Empty;
                string flagId = string.Empty;
                if (!hasFlag)
                    response = "NO";
                else
                {
                    response = "YES ";                    
                    var chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
                    var random = new Random();
                    for (int i = 0; i < 16; i++)
                    {
                        flagId += chars[random.Next(chars.Length)];
                    }
                    flagID = flagId;
                }
                Send(handler, response + flagId);
            }
            else if (content.StartsWith("capture_flag"))
            {
                Console.WriteLine("Read {0} bytes from socket. \n Data : {1}",
                   content.Length, content);
                string response = string.Empty;                
                if (content.Split(" ".ToCharArray(), StringSplitOptions.RemoveEmptyEntries)[1].Equals('#' + flagID))
                {
                    response += "FLAG:" + flagValue;
                    hasFlag = false;
                }
                else
                    response += "ERR: You're trying to trick me!";
                Send(handler, response);
            }
            else if (content.StartsWith("hide_flag"))
            {
                flagValue = content.Split(" ".ToCharArray(), StringSplitOptions.RemoveEmptyEntries)[1].Substring(1);
                hasFlag = true;
                Send(handler, "");
            }        
        }
    }

    private static void Send(Socket handler, String data)
    {
        // Convert the string data to byte data using ASCII encoding.
        byte[] byteData = Encoding.ASCII.GetBytes(data);

        // Begin sending the data to the remote device.
        handler.BeginSend(byteData, 0, byteData.Length, 0,
            new AsyncCallback(SendCallback), handler);
    }

    private static void SendCallback(IAsyncResult ar)
    {
        try
        {
            // Retrieve the socket from the state object.
            Socket handler = (Socket)ar.AsyncState;

            // Complete sending the data to the remote device.
            int bytesSent = handler.EndSend(ar);
            Console.WriteLine("Sent {0} bytes to client.", bytesSent);

            handler.Shutdown(SocketShutdown.Both);
            handler.Close();

        }
        catch (Exception e)
        {
            Console.WriteLine(e.ToString());
        }
    }

    private static void generateUID()
    {
        string UIDfirstPart = "emanoloiu.";
        UID += UIDfirstPart;
        var chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
        var random = new Random();
        for (int i = 0; i < 16 - UIDfirstPart.Length; i++)
        {
            UID += chars[random.Next(chars.Length)];
        }
    }

    private static void generateRandomIP()
    {
        Random r = new Random();
        IP = r.Next(256) + "." + r.Next(256) + "." + r.Next(256) + "." + r.Next(256);
    }


    public static int Main(String[] args)
    {
        generateUID();
        StartListening();

        return 0;
    }
}