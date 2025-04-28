using Microsoft.VisualStudio.TestPlatform.Utilities;
using RP.Communication.ServerClient.Interface;
using RP.Prober.Singleton;
using RP.TablePublisherSubscriber;
using System.Diagnostics;
using Tcp.Communication.ByteArray.ServerClient;
using Udp.Communication.ByteArray.ServerClient;
using static PublisherXUnitTests.StateOfTheArtTablePublisherTest;

namespace RestTestApp
{
    public class WeatherForecast
    {
        public DateOnly Date { get; set; }

        public int TemperatureC { get; set; }

        public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);

        public string? Summary { get; set; }
    }

    public class TT
    {
        public bool useTcpComm = true;

        private void TableSubscriber_OnNewUpdateBatch(UpdateBatch<ExternalPerson, SlimExternalPerson, HasId> batch)
        {
            Console.WriteLine(batch.ToString());
        }

        public void PubSub_N_Records()
        {
            int nRecords = 50;

            IClientCommunication client1Comm;
            IServerCommunication serverComm;
            Func<uint> serverGetSentMessagesCount;
            Func<uint> clientGetReceivedMessagesCount;

            if (!useTcpComm)
            {
                serverComm = new UdpByteArrayServer(7000, 8400, 20_000, 10 * 1024 * 1024, 5); // new Logger((msg) => _output.WriteLine(msg)));
                client1Comm = new UdpByteArrayClient("192.168.1.177", 7000, 8400, 10 * 1024 * 1024); // new Logger((msg) => _output.WriteLine(msg)));
                serverGetSentMessagesCount = (serverComm as UdpByteArrayServer).GetTotalSentMessages;
                clientGetReceivedMessagesCount = (client1Comm as UdpByteArrayClient).GetTotalReceivedMessages;
            }
            else
            {
                serverComm = new TcpByteArrayServer(7000, 20_000, 10 * 1024 * 1024, 5);
                client1Comm = new TcpByteArrayClient("127.0.0.1", 7000, 10 * 1024 * 1024);

                serverGetSentMessagesCount = (serverComm as TcpByteArrayServer).GetTotalSentMessages;
                clientGetReceivedMessagesCount = (client1Comm as TcpByteArrayClient).GetTotalReceivedMessages;
            }

            var tableSubscriber = new TableSubscriber<ExternalPerson, SlimExternalPerson, HasId>();

            tableSubscriber.OnNewUpdateBatch += TableSubscriber_OnNewUpdateBatch;

            tableSubscriber.Initiliaze(
                (extPer, sliExt) => { extPer.a = sliExt.a; return extPer; },
                ExternalPerson.Decode,
                SlimExternalPerson.Decode,
                IdHolder.Decode,
                a => new IdHolder() { Id = a.Id },
                a => new IdHolder() { Id = a.Id },
                () => client1Comm,
                10 * 1024 * 1024,
                new Logger((msg) => Console.WriteLine(msg)));

            var tablePublisher = new TablePublisher<InternalPerson, ExternalPerson, SlimExternalPerson, HasId>();
            tablePublisher.Initiliaze(
                "PubSub_N_Records",
                a => new ExternalPerson() { Id = a.Id, a = a.a, b = a.b, c = a.c },
                a => new SlimExternalPerson() { Id = a.Id, a = a.a },
                ExternalPerson.Encode,
                SlimExternalPerson.Encode,
                IdHolder.Encode,
                a => new IdHolder() { Id = a.Id },
                () => serverComm,
                20,
                200,
                10 * 1024 * 1204,
                new Logger((msg) => Console.WriteLine(msg)));

            tablePublisher.ToMonitorTypeConverter =
                (persons) =>
                {
                    var personsList = new List<List<string>>();
                    personsList?.Add(new List<string>() { "Revision", "LastUpdateTime", "Id", "A", "B", "C" });

                    foreach (var person in persons)
                    {
                        var personInfo = new List<string>();
                        if (personInfo != null)
                        {
                            personInfo.Add(person.Item3.ToString());
                            personInfo.Add(person.Item1.LastUpdateTime.ToShortTimeString());
                            personInfo.Add(person.Item1.Id.ToString());

                            if (person.Item2 != null)
                                personInfo.Add(person.Item2.a.ToString());
                            else
                                personInfo.Add(person.Item1.a.ToString());

                            personInfo.Add(person.Item1.b.ToString());
                            personInfo.Add(person.Item1.c.ToString());

                            personsList?.Add(personInfo);
                        }

                    }

                    return personsList!;
                };

            var persons = new List<InternalPerson>();

            for (int i = 0; i < nRecords; i++)
            {
                var newPerson = new InternalPerson()
                {
                    Id = (uint)i,
                    a = i,
                    b = i,
                    c = i,
                    d = i
                };

                for (int j = 0; j < newPerson.array.Length; j++)
                    newPerson.array[j] = (byte)i;

                persons.Add(newPerson);
            }

            foreach (var person in persons)
                tablePublisher.InsertRecord(person.Clone());

            Task.Factory.StartNew(
                () =>
                {
                    while (true)
                    {
                        foreach (var person in persons)
                        {
                            person.a++;
                            person.LastUpdateTime = DateTime.Now;
                            tablePublisher.UpdateRecord(person.Clone());

                            for (int k = 0; k < 10; k++)
                            {
                                person.a++;
                                person.LastUpdateTime = DateTime.Now;
                                tablePublisher.UpdateRecord(person.SlimClone());
                            }
                        }
                    }
                }, TaskCreationOptions.LongRunning);

            Task.Factory.StartNew(
                () =>
                {
                    while (true)
                    {
                        Thread.Sleep(1000);
                        var clientInnerTable = tableSubscriber.GetInnerTable();
                        if (clientInnerTable.Count > 0)
                        {
                            var maxDelay = clientInnerTable.Select(x => DateTime.Now.Subtract(x.LastUpdateTime).TotalSeconds).Max();
                            Console.WriteLine($"Max Delay Sec: {maxDelay}");
                        }
                        else
                        {
                            Console.WriteLine($"Max Delay Sec: No Elements");
                        }
                    }
                }, TaskCreationOptions.LongRunning);

            Thread.Sleep(501_000);

            var sentPerTotalUpdates = persons[0].a;
            var clientInnerTableLast = tableSubscriber.GetInnerTable();
            var receivedPerTotalUpdates = clientInnerTableLast[0].a;

            Console.WriteLine($"Total Updated {sentPerTotalUpdates}, LastReceived {receivedPerTotalUpdates}");

            var cachedTablesNames = ProberCacheClub.ProberCacheClubSingleton.GetCachedTablesNames().Select(x => x.Guid).ToList();
            var cachedTables = ProberCacheClub.ProberCacheClubSingleton.GetCachedTables(cachedTablesNames);

        }

        public void PubSub_N_Records_UpdateAsInsertForLowLatency()
        {
            int nRecords = 50;

            IClientCommunication client1Comm;
            IServerCommunication serverComm;
            Func<uint> serverGetSentMessagesCount;
            Func<uint> clientGetReceivedMessagesCount;

            if (!useTcpComm)
            {
                serverComm = new UdpByteArrayServer(7000, 8400, 20_000, 10 * 1024 * 1024, 5); // new Logger((msg) => _output.WriteLine(msg)));
                client1Comm = new UdpByteArrayClient("192.168.1.177", 7000, 8400, 10 * 1024 * 1024); // new Logger((msg) => _output.WriteLine(msg)));
                serverGetSentMessagesCount = (serverComm as UdpByteArrayServer).GetTotalSentMessages;
                clientGetReceivedMessagesCount = (client1Comm as UdpByteArrayClient).GetTotalReceivedMessages;
            }
            else
            {
                serverComm = new TcpByteArrayServer(8000, 20_000, 10 * 1024 * 1024, 5);
                client1Comm = new TcpByteArrayClient("127.0.0.1", 8000, 10 * 1024 * 1024);

                serverGetSentMessagesCount = (serverComm as TcpByteArrayServer).GetTotalSentMessages;
                clientGetReceivedMessagesCount = (client1Comm as TcpByteArrayClient).GetTotalReceivedMessages;
            }

            var tableSubscriber = new TableSubscriber<ExternalPerson, SlimExternalPerson, HasId>();

            tableSubscriber.OnNewUpdateBatch += TableSubscriber_OnNewUpdateBatch;

            tableSubscriber.Initiliaze(
                (extPer, sliExt) => { extPer.a = sliExt.a; return extPer; },
                ExternalPerson.Decode,
                SlimExternalPerson.Decode,
                IdHolder.Decode,
                a => new IdHolder() { Id = a.Id },
                a => new IdHolder() { Id = a.Id },
            () => client1Comm,
                100 * 1024 * 1024,
                new Logger((msg) => Console.WriteLine($"[Client] - {msg}")));

            var tablePublisher = new TablePublisher<InternalPerson, ExternalPerson, SlimExternalPerson, HasId>();
            tablePublisher.Initiliaze(
                "PubSub_N_Records_UpdateAsInsertForLowLatency",
                a => new ExternalPerson() { Id = a.Id, a = a.a, b = a.b, c = a.c },
                a => new SlimExternalPerson() { Id = a.Id, a = a.a },
                ExternalPerson.Encode,
                SlimExternalPerson.Encode,
                IdHolder.Encode,
                a => new IdHolder() { Id = a.Id },
                () => serverComm,
            20,
            200,
            100 * 1024 * 1204,
                new Logger((msg) => Console.WriteLine($"[Server] - {msg}")));

            tablePublisher.ToMonitorTypeConverter =
    (persons) =>
    {
        var personsList = new List<List<string>>();
        personsList?.Add(new List<string>() { "Revision", "Age", "Id", "A", "B", "C" });

        foreach (var person in persons)
        {
            var personInfo = new List<string>();
            if (personInfo != null)
            {
                personInfo.Add(person.Item3.ToString());
                personInfo.Add(DateTime.Now.Subtract(person.Item1.LastUpdateTime).TotalSeconds.ToString());
                personInfo.Add(person.Item1.Id.ToString());

                if (person.Item2 != null)
                    personInfo.Add(person.Item2.a.ToString());
                else
                    personInfo.Add(person.Item1.a.ToString());

                personInfo.Add(person.Item1.b.ToString());
                personInfo.Add(person.Item1.c.ToString());

                personsList?.Add(personInfo);
            }

        }

        return personsList!;
    };


            var persons = new List<InternalPerson>();

            for (int i = 0; i < nRecords; i++)
            {
                var newPerson = new InternalPerson()
                {
                    Id = (uint)i,
                    a = i,
                    b = i,
                    c = i,
                    d = i
                };

                for (int j = 0; j < newPerson.array.Length; j++)
                    newPerson.array[j] = (byte)i;

                persons.Add(newPerson);
            }

            foreach (var person in persons)
                tablePublisher.InsertRecord(person.Clone());

            Task.Factory.StartNew(
                () =>
                {
                    Stopwatch sw = Stopwatch.StartNew();
                    while (true)
                    {
                        var time1Sec = sw.Elapsed.TotalSeconds;

                        foreach (var person in persons)
                        {
                            person.a++;
                            person.LastUpdateTime = DateTime.Now;
                            tablePublisher.InsertRecord(person.Clone());

                            for (int k = 0; k < 10; k++)
                            {
                                person.a++;
                                person.LastUpdateTime = DateTime.Now;
                                tablePublisher.InsertRecord(person.SlimClone());
                            }
                        }

                        var time2Sec = sw.Elapsed.TotalSeconds;

                        var deltaTime = Math.Max(0, time2Sec - time1Sec);

                        //Thread.Sleep((int) (deltaTime * 1000));
                    }
                }, TaskCreationOptions.LongRunning);

            Task.Factory.StartNew(
                () =>
                {
                    while (true)
                    {
                        Thread.Sleep(1000);
                        var clientInnerTable = tableSubscriber.GetInnerTable();
                        if (clientInnerTable.Count > 0)
                        {
                            var maxDelay = clientInnerTable.Select(x => DateTime.Now.Subtract(x.LastUpdateTime).TotalSeconds).Max();
                            //Console.WriteLine($"Max Delay Sec: {maxDelay} ");
                            Console.WriteLine($"Max Delay Sec: {maxDelay} ");
                        }
                    }
                }, TaskCreationOptions.LongRunning);

            Thread.Sleep(501_000);

            var sentPerTotalUpdates = persons[0].a;
            var clientInnerTableLast = tableSubscriber.GetInnerTable();
            var receivedPerTotalUpdates = clientInnerTableLast[0].a;

            Console.WriteLine($"Total Updated {sentPerTotalUpdates}, LastReceived {receivedPerTotalUpdates}");
            Console.WriteLine($"Total Messages Sent {serverGetSentMessagesCount()}, Total Messages Received {clientGetReceivedMessagesCount()}");
        }
    }
}
