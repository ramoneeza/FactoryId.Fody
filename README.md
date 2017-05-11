## This is an add-in for [Fody](https://github.com/Fody/Fody/) 

![Icon](https://raw.githubusercontent.com/ramoneeza/FactoryId.Fody/master/Icons/package_icon.png)

Simplifies the implementation of [Factory Method Pattern](https://en.wikipedia.org/wiki/Factory_method_pattern)

[Introduction to Fody](http://github.com/Fody/Fody/wiki/SampleUsage)


## The nuget package  [![NuGet Status](http://img.shields.io/nuget/v/FactoryId.Fody.svg?style=flat)](https://www.nuget.org/packages/FactoryId.Fody/)

https://nuget.org/packages/FactoryId.Fody/

    PM> Install-Package FactoryId.Fody


## What it does

 * Looks for all classes with a `Factory Base` attribute.
 * Finds all subclasses looking for a `subclass key` (Auto or manual).
 * Registers all subclasses in a private dictionary based in a `key`.
 * Expose a static `factory method`
 
### Simple Case

All subclasses are registered from Enum Value.


#### Your Code

    public enum MyEnum
	{
		Hola,
		Quetal,
		Adios
	}

	[FactoryIntBaseAuto(typeof(MyEnum))]
	public class Auto
	{
		public static Auto FactoryInt(int key) { return null; } // Will be replaced
	}

	public class AutoHola:Auto { };
	
	public class AutoQuetal : Auto { };
	
	public class AutoAdios : Auto { };

	[FactoryExclude]
	public class AutoNanay : Auto { }; // Will not be registered 


#### What gets compiled

    [FactoryIntBaseAuto(typeof(MyEnum))]
	public class Auto
	{
		private static Dictionary<int, Type> _FactoryDataInt;

		public static Auto FactoryInt(int key)
		{
			if (Auto._FactoryDataInt.ContainsKey(key))
				result = (Auto)Activator.CreateInstance(Auto._FactoryDataInt[key]);
			else
				return result;
		}

		static Auto()
		{
			Auto._FactoryDataInt = new Dictionary<int, Type>();
			Auto._FactoryDataInt.Add(0, typeof(AutoHola));
			Auto._FactoryDataInt.Add(1, typeof(AutoQuetal));
			Auto._FactoryDataInt.Add(2, typeof(AutoAdios));
		}
	}


### Name Class Based

All subclasses are registered using class name. (Key is a string)

#### Your Code

	[FactoryStrBaseAuto]
	public class Auto
	{
		public static Auto FactoryStr(string key) { return null; }
	}

	public class AutoHola:Auto { };
	public class AutoQuetal : Auto { };
	public class AutoAdios : Auto { };
	public class AutoNanay : Auto { };

#### What gets compiled

    [FactoryStrBaseAuto]
	public class Auto
	{
		private static Dictionary<string, Type> _FactoryDataStr = new Dictionary<string, Type>();

		public static Auto FactoryStr(string key)
		{
			if (Auto._FactoryDataStr.ContainsKey(key))
				result = (Auto)Activator.CreateInstance(Auto._FactoryDataStr[key]);
			else
				return result;
		}

		static Auto()
		{
			Auto._FactoryDataStr.Add("Hola", typeof(AutoHola));
			Auto._FactoryDataStr.Add("Quetal", typeof(AutoQuetal));
			Auto._FactoryDataStr.Add("Adios", typeof(AutoAdios));
			Auto._FactoryDataStr.Add("Nanay", typeof(AutoNanay));
		}
	}

### Manual Registration

All subclasses are registered using Attributes

#### Your Code

	[FactoryIntBase]
	[FactoryStrBase]
	public class ClassA
    {
		public static int FactoryIntKey => -1;  // Optional
		public static string FactoryStrKey => null; //Optional

		public static ClassA FactoryInt(int key) { return null; }  // Will be replaced
		public static ClassA FactoryStr(string key) { return null; } //Will be replaced
	}

	[FactoryIntKey(1)]
	[FactoryStrKey("Hola")]
	public class ClassB:ClassA
	{
		// SubClass definition
	}

	[FactoryIntKey(2)]
	[FactoryStrKey("Adios")]
	public class ClassC : ClassA
	{
	// SubClass definition
	}

#### What gets compiled

   	[FactoryIntBase]
   	[FactoryStrBase]
	public class ClassA
	{
		private static Dictionary<int, Type> _FactoryDataInt = new Dictionary<int, Type>();
		private static Dictionary<string, Type> _FactoryDataStr = new Dictionary<string, Type>();

		public static int FactoryIntKey=>-1;
		
		public static string FactoryStrKey=>null;
		
		public static ClassA FactoryInt(int key)
		{
			if (ClassA._FactoryDataInt.ContainsKey(key))
				result = (ClassA)Activator.CreateInstance(ClassA._FactoryDataInt[key]);
				return result;
		}

		public static ClassA FactoryStr(string key)
		{
			if (ClassA._FactoryDataStr.ContainsKey(key))
				result = (ClassA)Activator.CreateInstance(ClassA._FactoryDataStr[key]);
			else
				return result;
		}

		static ClassA()
		{
			ClassA._FactoryDataStr.Add("Hola", typeof(ClassB));
			ClassA._FactoryDataStr.Add("Adios", typeof(ClassC));
			
			ClassA._FactoryDataInt.Add(1, typeof(ClassB));
			ClassA._FactoryDataInt.Add(2, typeof(ClassC));
		}
	}

