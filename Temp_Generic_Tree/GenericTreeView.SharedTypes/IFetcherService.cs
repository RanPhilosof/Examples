using MyNamespace;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GenericTreeView.SharedTypes
{
    public interface IFetcherService
    {
        object GetRootObject();
        public List<object> GetObjects();
        //public Type GetObjectType();
    }

    public class FetcherService1 : IFetcherService
    {
        //public Type GetObjectType()
        //{
        //    return typeof(Person1);
        //}

        public object GetRootObject()
        {
            var p = new ComplexProto();
            //p.Level7 = new Level7();

            //var p = new Person1() { Id = 10, Email = "Dsads", Name = "dsadas" }; //new Person();           
            //p.PhoneNumber = "dsdass";
            //p.Hobbies.AddRange(new Google.Protobuf.Collections.RepeatedField<string> { "dssa", "dasdas" });
            //
            //p.FavoriteFood = new Food();

            return p;
        }

        public List<object> GetObjects()
        {
            return new List<object>();
        }
    }

    public struct Type1
    {
        public double XP { get; set; }
        public double YP { get; set; }

        public Type2 T2 { get; set; }
    }

    public struct Type2
    {
        public double ZP { get; set; }
    }

    public class TT
    {
        public Type1 T1 { get; set; }
    }

    public enum Things
    {
        A,
        B,
        C
    }

    public class Person
    {
        public string[] sArr1 { get; set; } = new string[] { "10", "20", "30" };

        public Type1 T1 { get; set; } = new Type1() { XP = 11, YP = 33, T2 = new Type2() { ZP = 55 } };
        public TT tt { get; set; } = new TT() { T1 = new Type1 { XP = 1, YP = 3, T2 = new Type2() { ZP = 5 } } };
        public string AName { get; set; } = "RanPhilosof";
        public double[,] arr2 { get; set; } = new double[,] { { 10, 20, 22 }, { 20, 30, 32 }, { 30, 40, 42 } };
        public double[][] arr3 { get; set; } = new double[][] { new double[] { 10, 20, 22 }, new double[] { 20, 30, 32 }, new double[] { 30, 40, 42 } };
        public double[] arr { get; set; } = new double[] { 10, 20, 30 };
        public string[] sArr { get; set; } = new string[] { "10", "20", "30" };

        public int Age { get; set; }
        public string Name { get; set; } = string.Empty;
        public double Height { get; set; }

        public List<int> LastSalaries2 { get; set; } = new List<int>() { 1, 2 };
        public List<int> LastSalaries { get; set; } = new List<int>();

        public List<List<int>> LastSalaries3 { get; set; } = new List<List<int>>() { new List<int> { 5, 5, 5, }, new List<int> { 5, 6, 7 } };

        //public Dictionary<int, string> ToolBox { get; set; } = new Dictionary<int, string>() { { 1, "ads" }, { 2, "dasd" } };

        public II MyII1 { get; set; } = new IImp1();
        public II MyII2 { get; set; } = new IImp2();
    }

    public class Jobs
    {
        public int Age { get; set; }
        public string Name { get; set; } = string.Empty;
        public double Height { get; set; }
        public Address Adresses { get; set; } = new Address();
    }

    public class Address
    {
        public string Street { get; set; } = string.Empty;
        public DateTime Time { get; set; } = DateTime.Now;
    }

    public interface II { string MyInterfaceImpName { get; set; } }

    public class IImp1 : II
    {
        public string MyInterfaceImpName { get; set; } = "Imp1";
    }

    public class IImp2 : II
    {
        public string MyInterfaceImpName { get; set; } = "Imp1";

        public List<double> Doubles { get; set; } = new List<double>() { 1.0, 3.0, 5.0 };

        public II MyII1 { get; set; } = new IImp1();
    }
}
