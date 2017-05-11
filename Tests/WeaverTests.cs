using System;
using System.IO;
using System.Reflection;
using Mono.Cecil;
using NUnit.Framework;

[TestFixture]
public class WeaverTests
{
    Assembly assembly;
    string newAssemblyPath;
    string assemblyPath;

    [TestFixtureSetUp]
    public void Setup()
    {
		var codebase =Uri.UnescapeDataString((new UriBuilder(Path.GetDirectoryName(Assembly.GetExecutingAssembly().CodeBase))).Path);
        var projectPath = Path.GetFullPath(Path.Combine(codebase, @"..\..\..\AssemblyToProcess\AssemblyToProcess.csproj"));
        assemblyPath = Path.Combine(Path.GetDirectoryName(projectPath), @"bin\Debug\AssemblyToProcess.dll");
#if (!DEBUG)
        assemblyPath = assemblyPath.Replace("Debug", "Release");
#endif

        newAssemblyPath = assemblyPath.Replace(".dll", "2.dll");
        File.Copy(assemblyPath, newAssemblyPath, true);

        var moduleDefinition = ModuleDefinition.ReadModule(assemblyPath);
        var weavingTask = new ModuleWeaver
        {
            ModuleDefinition = moduleDefinition
        };

        weavingTask.Execute();
        moduleDefinition.Write(newAssemblyPath);

        assembly = Assembly.LoadFile(newAssemblyPath);
    }

    [Test]
    public void ValidateStaticFactoryIntKeyIsInjected()
    {
        var type = assembly.GetType("AssemblyToProcess.ClassB");
		var property = type.GetProperty("FactoryIntKey", BindingFlags.Public | BindingFlags.Static);
		Assert.IsNotNull(property);
    }
	[Test]
	public void ValidateStaticFactoryIntKeyValue()
	{
		var type = assembly.GetType("AssemblyToProcess.ClassB");
		var property = type.GetProperty("FactoryIntKey", BindingFlags.Public | BindingFlags.Static);
		var value = (int)property.GetValue(null, null);
		Assert.IsTrue(value==1);
	}
	[Test]
	public void ValidateStaticFactoryStrKeyIsInjected()
	{
		var type = assembly.GetType("AssemblyToProcess.ClassB");
		var property = type.GetProperty("FactoryStrKey", BindingFlags.Public | BindingFlags.Static);
		Assert.IsNotNull(property);
	}
	[Test]
	public void ValidateStaticFactoryStrKeyValue()
	{
		var type = assembly.GetType("AssemblyToProcess.ClassB");
		var property = type.GetProperty("FactoryStrKey", BindingFlags.Public | BindingFlags.Static);
		var value = (string)property.GetValue(null, null);
		Assert.IsTrue(value == "Hola");
	}
#if (DEBUG)
	[Test]
    public void PeVerify()
    {
        Verifier.Verify(assemblyPath,newAssemblyPath);
    }
#endif
}