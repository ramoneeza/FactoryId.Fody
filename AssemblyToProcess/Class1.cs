
using System;
using FactoryId;
using System.Collections.Generic;
namespace AssemblyToProcess
{
	
	[FactoryIntBase]
	[FactoryStrBase]
	public class ClassA
    {
		public static int FactoryIntKey => -1;
		public static string FactoryStrKey => null;

		public static ClassA FactoryInt(int key) { return null; }
		public static ClassA FactoryStr(string key) { return null; }


	}

	[FactoryIntKey(1)]
	[FactoryStrKey("Hola")]
	public class ClassB:ClassA
	{
		
	}
	[FactoryIntKey(2)]
	[FactoryStrKey("Adios")]
	public class ClassC : ClassA
	{

	}
	public enum MyEnum
	{
		Hola,
		Quetal,
		Adios
	}

	[FactoryIntBaseAuto(typeof(MyEnum))]
	[FactoryStrBaseAuto]
	public class Auto
	{
		public static Auto FactoryInt(int key) { return null; }
		public static Auto FactoryStr(string key) { return null; }
	}
	public class AutoHola:Auto { };
	public class AutoQuetal : Auto { };
	public class AutoAdios : Auto { };

	[FactoryExclude]
	public class AutoNanay : Auto { };


	public class FAKE
	{
		private static Dictionary<int, Type> _Factory = new Dictionary<int, Type>();
		public static Auto Factory(int key)
		{
			if (_Factory.ContainsKey(key)) return (Auto)Activator.CreateInstance(_Factory[key]);
			return null;
		}
	}
}
