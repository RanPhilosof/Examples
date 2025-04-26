using Elisra.Infra.CommunicationShared;
//using Elisra.Infra.Logging;
//using Elisra.Infra.Scheduling;
using System;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace Elisra.Infra.Communication
{
	public static class Validation
	{
		public static byte[] Start = new byte[] { 10, 20, 30, 40, 50 };
		public static byte[] End = new byte[] { 80, 90, 100 };
		public static byte[] Count = new byte[] { 15, 25, 35, 45, 55 };

		public static string ValidationFailedExceptionMessage = "Failed reading data by validation test!";

		public static void Validate(BinaryReader br, byte[] arr)
		{
			var count = arr.Length;
			for (int i = 0; i < count; i++)
				if (arr[i] != br.ReadByte())
					throw new Exception(ValidationFailedExceptionMessage);
		}

		public static bool Sync(BinaryReader br, byte[] arr)
		{
			var readArr = new char[arr.Length];
			int i = 0;

			while (true)
			{
				var readChar = br.ReadByte();
				if (readChar == arr[i])
				{
					i++;
				}
				else
				{
					if (readChar == arr[0])
						i = 1;
					else
						i = 0;
				}

				if (i == arr.Length)
					return true;
			}
		}

		public static void Validate(Stream br, byte[] arr)
		{
			var count = arr.Length;
			for (int i = 0; i < count; i++)
			{
				int readByte;
				do
				{
					readByte = br.ReadByte();
				}
				while (readByte < 0);

				if (arr[i] != readByte)
					throw new Exception(ValidationFailedExceptionMessage);
			}
		}

		public static bool Sync(Stream br, byte[] arr)
		{
			var readArr = new char[arr.Length];
			int i = 0;

			while (true)
			{
				int readByte;
				do
				{
					readByte = br.ReadByte();
				}
				while (readByte < 0);

				if (readByte == arr[i])
				{
					i++;
				}
				else
				{
					if (readByte == arr[0])
						i = 1;
					else
						i = 0;
				}

				if (i == arr.Length)
					return true;
			}
		}

		public static void MaxCount(int count, int maxCount)
		{
			if (count > maxCount)
				throw new Exception(ValidationFailedExceptionMessage);
		}
	}

	public static class Helpers
	{
		public static void AbortThread(ref Thread thread)
		{
			if (thread != null)
			{
				thread.Abort();

				for (int i = 0; i < 1000000; i++)
				{
					if ((thread.ThreadState & System.Threading.ThreadState.Aborted) != System.Threading.ThreadState.Aborted)
						Thread.SpinWait(100);
					else
						break;
				}
			}
			thread = null;
		}
	}

	public enum CommunicationTypeEnum
	{
		Tcp,
		NamedPipes
	}

	/*
	public class Server<T> : IDisposable where T : ICommunicationSerialiable, new()
	{
		public event Action<T> PacketReceived;		

		private CommunicationTypeEnum communicationType;
		private TcpListener server;
		private TcpClient client;

		private NamedPipeServerStream pipeServer;
		private String Ip;
		private String Port;

		private Stream stream;
		private byte[] bytesArray;
		private MemoryStream memStream;

		private Thread keepAliveThread;
		private Thread receiveDataThread;

		private int? tcpReadTimeoutInterval_mSec;

		private ILogger logger;

		/// <param name="ip">Leave null-or-empty to listen all interfaces</param>
		public Server(
			String ip,
			String port,
			CommunicationTypeEnum communicationType,
			int maxMessageSizeInBytes = 52428800,
			ILogger logger = null)
		{
			bytesArray = new byte[maxMessageSizeInBytes];
			memStream = new MemoryStream(bytesArray);
			this.communicationType = communicationType;
			Ip = ip;
			Port = port;
			this.logger = logger;
		}

		public void SetTcpReadTimeoutInerval(int? readTimeoutInterval_mSec)
		{
			tcpReadTimeoutInterval_mSec = readTimeoutInterval_mSec;
		}

		public bool IsConnected
		{
			get
			{
				switch (communicationType)
				{
					case CommunicationTypeEnum.Tcp:
                        try
                        {
							if(client != null && client.Connected)
                            {
								NetworkStream _ = client.GetStream();
								return true;
							}
							return false;
								
						}
                        catch (Exception e)
                        {

							return false;
                        }
						break;
					case CommunicationTypeEnum.NamedPipes:
						return pipeServer != null && pipeServer.IsConnected;

					default:
						return false;
				}
			}
		}

		public void ConnectAndKeepAlive()
		{
			Helpers.AbortThread(ref keepAliveThread);
			Helpers.AbortThread(ref receiveDataThread);

			keepAliveThread = new Thread(() =>
			{
				receiveDataThread = null;

				while (true)
				{
					try
					{
						if (!this.IsConnected)
						{
							Helpers.AbortThread(ref receiveDataThread);

							this.TryStop();
							this.Start();
							this.WaitForConnection();

							receiveDataThread = new Thread(new ThreadStart(() =>
							{
								this.StartReceiveData();
							}));

							receiveDataThread.Start();
						}
					}
					catch { }

					Thread.Sleep(1000);
				}
			})
			{
				//This thread shouldn't keep the process alive
				IsBackground = true,
			};

			keepAliveThread.Start();
		}

		private System.Threading.Tasks.Task task;
		private Timer timer;

		public void ConnectAndKeepConnectionAlive()
		{
			timer = new Timer(KeepAlive, null, 0, 5000);
		}

		private void KeepAlive(Object o)
		{
			try
			{
				timer.Change(System.Threading.Timeout.Infinite, System.Threading.Timeout.Infinite);

				if (!IsConnected)
				{
					this.TryStop();

					this.Start();
					this.WaitForConnection();

					task = new System.Threading.Tasks.Task(() => this.StartReceiveData(), System.Threading.Tasks.TaskCreationOptions.LongRunning);
					task.Start();
				}
			}
			catch (Exception ex)
			{
				if (logger != null)
					logger.Error(ex.ToString());
			}
			finally
			{
				timer.Change(5000, 5000);
			}
		}

		private void TryStop()
		{
			try
			{
				switch (communicationType)
				{
					case CommunicationTypeEnum.Tcp:
						{
							if (server != null)
								server.Stop();
						}
						break;

					case CommunicationTypeEnum.NamedPipes:
						{
							if (pipeServer != null)
								pipeServer.Close();
						}
						break;
				}
			}
			catch (Exception ex)
			{
				if (logger != null) logger.Error(string.Format("Exception: {0}", ex.ToString()));
			}
		}

		public void Start()
		{
			try
			{
				switch (communicationType)
				{
					case CommunicationTypeEnum.Tcp:
						{
							server = new TcpListener(new IPEndPoint(string.IsNullOrEmpty(Ip) ? IPAddress.Any : IPAddress.Parse(Ip), int.Parse(Port)));
							server.Start();
						}
						break;

					case CommunicationTypeEnum.NamedPipes:
						{
							pipeServer = new NamedPipeServerStream(string.Format("PipesOfPiece_{0}", Port), PipeDirection.InOut, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous, 1000, 1000);
						}
						break;
				}
			}
			catch (SocketException e)
			{
				if (logger != null) logger.Error(string.Format("SocketException: {0}", e.ToString()));
			}
			catch (Exception ex)
			{
				if (server != null)
					server.Stop();

				if (pipeServer != null)
					pipeServer.Close();

				if (logger != null) logger.Error(string.Format("Exception: {0}", ex.ToString()));
			}
		}

		public void WaitForConnection()
		{
			try
			{
				switch (communicationType)
				{
					case CommunicationTypeEnum.Tcp:
						{
							if (logger != null)
								logger.Info("Waiting for a connection... ");
							client = server.AcceptTcpClient();
							if (logger != null)
								logger.Info("Tcp Client connected.");
							stream = client.GetStream();
							if (tcpReadTimeoutInterval_mSec.HasValue)
								stream.ReadTimeout = tcpReadTimeoutInterval_mSec.Value;
						}
						break;

					case CommunicationTypeEnum.NamedPipes:
						{
							if (logger != null)
								logger.Info("Waiting for a connection... ");
							pipeServer.WaitForConnection();
							if (logger != null)
								logger.Info("Named Pipes Client connected.");
							stream = pipeServer;
						}
						break;
				}
			}
			catch (Exception ex)
			{
				if (logger != null)
					logger.Error(string.Format("Connection failed, {0}", ex.ToString()));

				if (client != null)
					client.Close();

				if (server != null)
					server.Stop();

				if (pipeServer != null)
					pipeServer.Close();
			}
		}

		public void StartReceiveData()
		{
			try
			{
				bool isSync = false;
				while (true)
				{
					try
					{
						if (isSync)
						{
							var v = new T();

							int readSize = 0;
							while (readSize < sizeof(int))
								readSize += stream.Read(bytesArray, readSize, sizeof(int) - readSize);
							Validation.Validate(stream, Validation.Count);

							readSize = 0;
							var messageSize = BitConverter.ToInt32(bytesArray, 0);
							while (readSize < messageSize)
								readSize += stream.Read(bytesArray, readSize, messageSize - readSize);

							memStream.Position = 0;
							var br = new BinaryReader(memStream);

							v.Deserialize(br);

							Validation.Validate(stream, Validation.End);
							isSync = false;

							var packetReceived = PacketReceived;
							if (packetReceived != null)
								packetReceived(v);
						}
						else
						{
							isSync = Validation.Sync(stream, Validation.Start);
						}
					}
					catch (Exception ex)
					{
						if (ex.Message == Validation.ValidationFailedExceptionMessage)
						{
							isSync = false;
							if (logger != null)
								logger.Error("Message out of sync!");
						}
						else
						{
							throw;
						}
					}
				}
			}
			catch (Exception ex)
			{
				if (logger != null) logger.Error(string.Format("Connection failed, {0}", ex.ToString()));
				Close();
			}
		}

		private void Close()
		{
			try
			{
				if (stream != null)
					stream.Close();
			}
			catch { }
			finally { stream = null; }

			try
			{
				if (client != null)
					client.Close();
			}
			catch { }
			finally { client = null; }

			try
			{
				if (server != null)
					server.Stop();
			}
			catch { }
			finally { server = null; }

			try
			{
				if (pipeServer != null)
				{
					if (pipeServer.IsConnected)
						pipeServer.Disconnect();
					pipeServer.Close();
					pipeServer.Dispose();
				}
			}
			catch { }
			finally { pipeServer = null; }
		}

		public void Dispose()
		{
			Close();

			try { Helpers.AbortThread(ref keepAliveThread); }
			catch (Exception ex) { if (logger != null) logger.Error(ex.ToString()); }
			try { Helpers.AbortThread(ref receiveDataThread); } catch (Exception ex) { if (logger != null) logger.Error(ex.ToString()); }
		}
	}

	public class Client<T> : IDisposable where T : ICommunicationSerialiable, new()
	{
		public event Action<T> PacketIsAboutToBeSend;

		public event Action<T> PacketSent;

		protected NamedPipeClientStream pipeClient;

		protected TcpClient client;

		protected CommunicationTypeEnum communicationType;
		protected Stream stream;
		protected byte[] bytesArray;
		protected MemoryStream memStream;

		protected String Ip;
		protected String Port;

		private ulong sendDataCalledCounter = 0;

		private Thread keepAliveThread;

		private int? tcpWriteTimeoutInterval_mSec;

		private bool isDisposed = false;

		private ILogger logger;

		public Client(
			String ip,
			String port,
			CommunicationTypeEnum communicationType,
			int maxMessageSizeInBytes,
			ILogger logger = null)
		{
			bytesArray = new byte[maxMessageSizeInBytes];
			memStream = new MemoryStream(bytesArray);

			this.communicationType = communicationType;
			Ip = ip;
			Port = port;

			this.logger = logger;
		}

		public void SetTcpWriteTimeoutInerval(int? writeTimeoutInterval_mSec)
		{
			tcpWriteTimeoutInterval_mSec = writeTimeoutInterval_mSec;
		}

		public virtual bool IsConnected
		{
			get
			{
				switch (communicationType)
				{
					case CommunicationTypeEnum.Tcp:
						return client != null && client.Connected;

					case CommunicationTypeEnum.NamedPipes:
						return pipeClient != null && pipeClient.IsConnected;

					default:
						return false;
				}
			}
		}

		public virtual void SendData(T t)
		{
			try
			{
				sendDataCalledCounter++;

				var packetIsAboutToBeSend = PacketIsAboutToBeSend;
				if (packetIsAboutToBeSend != null)
					packetIsAboutToBeSend(t);

				memStream.Position = 0;
				var bw = new BinaryWriter(memStream);

				bw.Write(Validation.Start, 0, Validation.Start.Length);
				var comMessageStartPos = bw.BaseStream.Position;
				bw.BaseStream.Position += sizeof(Int32);
				bw.Write(Validation.Count, 0, Validation.Count.Length);
				var dataMessageStartPos = bw.BaseStream.Position;
				t.Serialize(bw);
				var dataMessageEndPos = bw.BaseStream.Position;
				var serializeSize = dataMessageEndPos - dataMessageStartPos;
				Array.Copy(BitConverter.GetBytes((int)serializeSize), 0, bytesArray, comMessageStartPos, sizeof(Int32));
				bw.Write(Validation.End, 0, Validation.End.Length);
				var bytesToSend = (int)bw.BaseStream.Position;

				//var modulu = sendDataCalledCounter % 50;
				//if (modulu >= 0 && modulu < 5)
				//    Log("i {0}: bytesToSend = {1}", sendDataCalledCounter, bytesToSend);
				
				stream.Write(bytesArray, 0, bytesToSend);

				var packetSent = PacketSent;
				if (packetSent != null)
					packetSent(t);
			}
			catch (Exception ex)
			{
				if (logger != null)
					logger.Error(string.Format("Failed to send message: {0}", ex.ToString()));
			}
		}

		public void ConnectAndKeepAlive()
		{
			Helpers.AbortThread(ref keepAliveThread);

			keepAliveThread = new Thread(() =>
			{
				while (true)
				{
					try
					{
						if (isDisposed)
							return;

						if (!IsConnected)
							TryConnect();
					}
					catch
					{
					}
					Thread.Sleep(1000);
				}
			})
			{
				//This thread shouldn't keep the process alive
				IsBackground = true,
			};

			keepAliveThread.Start();
		}

		private Timer _timer;

		public void KeepConnectionAlive()
		{
			_timer = new Timer(KeepAlive, null, 5000, 5000);
		}

		private void KeepAlive(object o)
		{
			try
			{
				_timer.Change(Timeout.Infinite, Timeout.Infinite);

				if (isDisposed)
				{
					_timer = null;
					return;
				}

				if (!IsConnected)
				{
					TryConnect();
				}
			}
			catch (Exception ex)
			{
				logger?.Error(ex.ToString());
			}
			finally
			{
				_timer?.Change(5000, 5000);
			}
		}

		public void TryConnect()
		{
			try
			{
				switch (communicationType)
				{
					case CommunicationTypeEnum.Tcp:
						{
							try { if (client != null) client.Close(); }
							catch (Exception ex) { if (logger != null) logger.Error(string.Format("Close Connection: {0}", ex.ToString())); }

							client = new TcpClient();

							client.Connect(new IPEndPoint(IPAddress.Parse(Ip), int.Parse(Port)));
							stream = client.GetStream();
							if (tcpWriteTimeoutInterval_mSec.HasValue)
								stream.WriteTimeout = tcpWriteTimeoutInterval_mSec.Value;
						}
						break;

					case CommunicationTypeEnum.NamedPipes:
						{
							try { if (pipeClient != null) pipeClient.Close(); }
							catch (Exception ex) { if (logger != null) logger.Error(string.Format("Close Connection: {0}", ex.ToString())); }
							pipeClient = new NamedPipeClientStream(".", string.Format("PipesOfPiece_{0}", Port), PipeDirection.InOut);
							pipeClient.Connect();
							stream = pipeClient;
						}
						break;
				}
			}
			catch (Exception ex)
			{
				if (logger != null) logger.Error(string.Format("Connection failed, {0}", ex.ToString()));
				Close();
			}
		}

		private void Close()
		{
			try
			{
				if (stream != null)
					stream.Close();
			}
			finally { stream = null; }

			try
			{
				if (client != null)
					client.Close();
			}
			finally { client = null; }

			try
			{
				if (pipeClient != null)
				{
					pipeClient.Close();
					pipeClient.Dispose();
				}
			}
			finally { pipeClient = null; }
		}

		public virtual void Dispose()
		{
			Close();
			try { Helpers.AbortThread(ref keepAliveThread); } catch (Exception ex) { if (logger != null) logger.Error(ex.ToString()); }

		}
	}


    public class ClientWithReliableDisconnectEvents<T> : Client<T> where T : ICommunicationSerialiable, new()
	{
		private readonly byte[] testByteArray = new byte[] { 1 };

		private bool _isConnected = false;

		public delegate void IsConnectedChangedDelegate(bool newIsConnected);

		public event IsConnectedChangedDelegate OnIsConnectedChanged;

		public ClientWithReliableDisconnectEvents(
			String ip,
			String port,
			CommunicationTypeEnum communicationType,
			int maxMessageSizeInBytes,
			ILogger logger = null):base(ip, port, communicationType, maxMessageSizeInBytes, logger)
        {
			
		}

        public override bool IsConnected
        {
			get
			{
				switch (communicationType)
				{
					case CommunicationTypeEnum.Tcp:
						{
							bool isConnected = IsTcpClientConnected();
							if (isConnected != _isConnected)
							{
								_isConnected = isConnected;
								OnIsConnectedChanged?.Invoke(_isConnected);
							}
							return isConnected;
						}


					case CommunicationTypeEnum.NamedPipes:
						return pipeClient != null && pipeClient.IsConnected;

					default:
						return false;
				}
			}
		}

		private bool IsTcpClientConnected()
		{

            if (this.client == null || !this.client.Connected)
			{
				return false;
			}
			else
			{
                try
                {
                    this.client.GetStream().Write(this.testByteArray, 0, testByteArray.Length);
                }
                catch
                {
                    return false;
                }
                return true;
			}

		}
	}



	public class AsynchornousClient<T> : Client<T> where T : ICommunicationSerialiable, new()
	{
		private ulong invokeSendDataCalledCounter = 0;
		private TaskPriority sendDataPriority;
		private SequentialContext sc;

		public AsynchornousClient(String ip, String port, CommunicationTypeEnum communicationType, TaskPriority sendDataPriority, int maxMessageSizeInBytes = 52428800)
			: base(ip, port, communicationType, maxMessageSizeInBytes)
		{
			this.sendDataPriority = sendDataPriority;

			var _scheduler = ParallelService.Instance.CreateDedicatedWorker("Communication: Client Schedulder");
			_scheduler.Initialize();

			sc = new SequentialContext(_scheduler, "SeqCtx: Collected Data Transmitter");
		}

		public override void SendData(T t)
		{
			invokeSendDataCalledCounter++;
			sc.Schedule("SendData", (int)sendDataPriority, base.SendData, t);

			// TODO: remove it in the future
			//if (invokeSendDataCalledCounter % 50 == 0)
			//    Log("CommunicationQueueSize = {0}", sc.GetWorkQueueLength());
		}

		public int GetWorkQueueCount()
		{
			return sc.GetWorkQueueLength();
		}

		public override void Dispose()
		{
			base.Dispose();
			sc.Dispose();
		}
	}

	public class UnitTestClient<T> : Client<T> where T : ICommunicationSerialiable, new()
	{
		private Object locker = new Object();

		public UnitTestClient(String ip, String port, CommunicationTypeEnum communicationType, int maxMessageSizeInBytes = 52428800)
			: base(ip, port, communicationType, maxMessageSizeInBytes) { }

		public void InvokeSendData(T t)
		{
			lock (locker)
			{
				SendData(t);
			}
		}

		public override void Dispose()
		{
			base.Dispose();
		}
	}
	*/
}