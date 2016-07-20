
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

#pragma warning disable RECS0154 // Parameter is never used
		public static ClassA FactoryInt(int key) { return null; }
		public static ClassA FactoryStr(string key) { return null; }
#pragma warning restore RECS0154 // Parameter is never used

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
#pragma warning disable RECS0154 // Parameter is never used
		public static Auto FactoryInt(int key) { return null; }
		public static Auto FactoryStr(string key) { return null; }
#pragma warning restore RECS0154 // Parameter is never used
	}
	public class AutoHola:Auto { };
	public class AutoQuetal : Auto { };
	public class AutoAdios : Auto { };

	[FactoryExclude]
	public class AutoNanay : Auto { };

}
