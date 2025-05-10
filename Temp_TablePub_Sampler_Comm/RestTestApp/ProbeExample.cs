using RP.Prober.CyclicCacheProbing;
using RP.Prober.Singleton;
using System.ComponentModel.DataAnnotations;
using System.Reflection.PortableExecutable;

namespace RestTestApp
{
    public class Measurment
    {
        public int Counter;

        public static int globalCounter = 0;

        public int Height;
        public int Width;
        public int Size;
        public string Description;

        public Measurment(int height, int width, int size, string description)
        {
            Counter = Interlocked.Increment(ref globalCounter);

            Height = height;
            Width = width;
            Size = size;
            Description = description;
        }

        public static List<string> Headers = new List<string>() { "Counter", "Height", "Width", "Size", "Desctiption" };
    }


    public class MeasurmentsCreator
    {
        private CyclicCacheProbing<Measurment> cyclicCacheProbing;

        public MeasurmentsCreator(string name, HeaderType headertype, int maxCachedValues)
        {
            cyclicCacheProbing = 
                new CyclicCacheProbing<Measurment>(maxCachedValues, name, Measurment.Headers, headertype);

            cyclicCacheProbing.Convert =
                (measure) =>
                {
                    return new List<string>()
                            {                                
                                $"{measure.Counter}",
                                $"{measure.Height}",
                                $"{measure.Width}",
                                $"{measure.Size}",
                                $"{measure.Description}"
                            };
                };
        }

        public void StartGenerateLines()
        {
            Task.Run(
                () =>
                {
                    while (true)
                    {
                        Thread.Sleep(100);
                        GenerateNewLine();
                    }
                });
        }

        private void GenerateNewLine()
        {
            cyclicCacheProbing.EnqueueCyclic(new Measurment(Random.Shared.Next(1, 10), Random.Shared.Next(11, 20), Random.Shared.Next(21, 30), GetRandomWord()));
        }

        private static List<string> words = new List<string> { "apple", "banana", "cherry", "dog", "elephant", "flower" };
        private static string GetRandomWord()
        {
            var words = new List<string> { "apple", "banana", "cherry", "dog", "elephant", "flower" };
            var random = new Random();
            int index = random.Next(words.Count);
            return words[index];
        }
    }

    public class SamplesCreator
    {
        private KeyedCacheProbing<string, Measurment> keyedCacheProbing;

        public SamplesCreator(string name)
        {
            keyedCacheProbing =
                new KeyedCacheProbing<string, Measurment>(name, (new List<string>() { "Key" }.Concat(Measurment.Headers).ToList()), true);

            keyedCacheProbing.Convert =
                (key, measure) =>
                {
                    return new List<string>()
                            {
                                $"{key}",
                                $"{measure.Counter}",
                                $"{measure.Height}",
                                $"{measure.Width}",
                                $"{measure.Size}",
                                $"{measure.Description}"
                            };
                };
        }

        public void StartGenerateLines()
        {
            Task.Run(
                () =>
                {
                    while (true)
                    {
                        Thread.Sleep(100);
                        GenerateNewLine();
                    }
                });
        }

        private void GenerateNewLine()
        {
            keyedCacheProbing.SetKeyValue(GetRandomKey(), new Measurment(Random.Shared.Next(1, 10), Random.Shared.Next(11, 20), Random.Shared.Next(21, 30), GetRandomWord()));
        }

        private static string GetRandomWord()
        {
            var words = new List<string> { "apple", "banana", "cherry", "dog", "elephant", "flower" };
            var random = new Random();
            int index = random.Next(words.Count);
            return words[index];
        }

        private static string GetRandomKey()
        {
            var words = new List<string> { "Flow1", "Flow2", "Flow3", "Flow4", "Flow5", "Flow6" };
            var random = new Random();
            int index = random.Next(words.Count);
            return words[index];
        }
    }
    
    public class MeasurmentsCountersCreator
    {
        private CyclicCacheProbing<List<int>> cyclicCacheProbing;
        private Dictionary<String, int> headerToIndex = new Dictionary<string, int>();
        private List<string> originalHeader = new List<string>();
        public MeasurmentsCountersCreator(string name, List<string> header)
        {
            if (header == null)
                header = new List<string>() { "Cpu", "Memory", "Gc", "Pcu" };

            originalHeader = header;

            for (int i=0; i<header.Count; i++)
                headerToIndex.Add(header[i], i);

            cyclicCacheProbing =
                new CyclicCacheProbing<List<int>>(1, name, header, HeaderType.Column);

            cyclicCacheProbing.Convert =
                (measure) =>
                {
                    return new List<string>()
                            {
                                $"{measure[0]}",
                                $"{measure[1]}",
                                $"{measure[2]}",
                                $"{measure[3]}",
                            };
                };
        }

        public void StartGenerateLines()
        {
            Task.Run(
                () =>
                {
                    while (true)
                    {
                        Thread.Sleep(100);
                        GenerateNewLine();
                    }
                });
        }

        private void GenerateNewLine()
        {
            var kvp = new Dictionary<string, int>();
            foreach (var head in headerToIndex)
            {
                switch(head.Key)
                {
                    case "Cpu":
                        kvp.Add(head.Key, Random.Shared.Next(0, 100));
                        break;
                    case "Memory":
                        kvp.Add(head.Key, Random.Shared.Next(5000, 8000));
                        break;
                    case "Gc":
                        kvp.Add(head.Key, Random.Shared.Next(20, 50));
                        break;
                    case "Pcu":
                        kvp.Add(head.Key, Random.Shared.Next(20000, 50000));
                        break;
                }

            }

            var res = new List<int>();
            foreach (var v in kvp)
                res.Add(v.Value);

            cyclicCacheProbing.EnqueueCyclic(res);
        }
    }

    public class MyTable
    {
        public List<List<object>> Values = new List<List<object>>();
        public MyTable()
        {
            Values.Add(new List<object>() { "Table1"});
            Values.Add(new List<object>() { "Values", "Values" });
            Values.Add(new List<object>() { 10, 20, 30, 40, 50, 60, 70, 80, 90});
            Values.Add(new List<object>() { 10, 20, 30, 40, 50, 60, 70, 80, 90 });
            Values.Add(new List<object>() { 10, 20, 30, 40, 50, 60, 70, 80, 90 });
            Values.Add(new List<object>() { 10, 20, 30, 40, 50, 60, 70, 80, 90 });
            Values.Add(new List<object>() { 10, 20, 30, 90 });
            Values.Add(new List<object>() { 10, 20, 30, 80, 90 });
            Values.Add(new List<object>() { 10, 20, 30, 40, 50, 60, 70, 80, 90 });
            Values.Add(new List<object>() { "Table2" });
            Values.Add(new List<object>() { "Values", "Values" });
            Values.Add(new List<object>() { 10, 20, 30, 40, 50, 60, 70, 80, 90 });
            Values.Add(new List<object>() { 10, 20, 30, 40, 50, 60, 70, 80, 90 });
            Values.Add(new List<object>() { 10, 20, 30, 40, 50, 60, 70, 80, 90 });
            Values.Add(new List<object>() { 10, 20, 30, 40, 50, 60, 70, 80, 90 });
            Values.Add(new List<object>() { 10, 20, 30, 90 });
            Values.Add(new List<object>() { 10, 20, 30, 80, 90 });
            Values.Add(new List<object>() { 10, 20, 30, 40, 50, 60, 70, 80, 90 });
        }
    }

    public class TableExampleCreator
    {
        private TableCacheProbing<MyTable> tableCacheProbing;

        public TableExampleCreator(string name)
        {
            tableCacheProbing = new TableCacheProbing<MyTable>(name);

            tableCacheProbing.Convert =
                (table) =>
                {
                    var res = new List<List<string>>();

                    foreach (var line in table.Values)
                    {
                        var l = new List<string>();
                        foreach (var el in line)
                            l.Add(el.ToString());
                        
                        res.Add(l);
                    }

                    return res;
                };

            tableCacheProbing.SetTableData(new MyTable());
        }
    }
}
