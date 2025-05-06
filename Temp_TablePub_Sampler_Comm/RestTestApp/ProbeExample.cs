using RP.Prober.CyclicCacheProbing;
using RP.Prober.Singleton;

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
}
