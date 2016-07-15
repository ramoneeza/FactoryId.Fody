using System;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Rocks;
using Mono.Cecil.Cil;
using System.Collections.Generic;

public partial class ModuleWeaver
{
    // Will log an informational message to MSBuild
    public Action<string> LogInfo { get; set; }

    // An instance of Mono.Cecil.ModuleDefinition for processing
    public ModuleDefinition ModuleDefinition { get; set; }

    TypeSystem typeSystem;

    // Init logging delegates to make testing easier
    public ModuleWeaver()
    {
        LogInfo = m => { };
    }
	const string IntAttr = "FactoryId.FactoryIntKeyAttribute";
	const string IntBaseAttr = "FactoryId.FactoryIntBaseAttribute";
	const string StrAttr = "FactoryId.FactoryStrKeyAttribute";
	const string StrBaseAttr = "FactoryId.FactoryStrBaseAttribute";
	const string IntProp = "FactoryIntKey";
	const string StrProp = "FactoryStrKey";
	const string IntFData = "_FactoryDataInt";
	const string StrFData = "_FactoryDataStr";

	const string IntBaseAutoAttr = "FactoryId.FactoryIntBaseAutoAttribute";
	const string StrBaseAutoAttr = "FactoryId.FactoryStrBaseAutoAttribute";
	const string FactoryExclude = "FactoryId.FactoryExcludeAttribute";

	public void Execute()
    {
        typeSystem = ModuleDefinition.TypeSystem;

		CreateAutoFactory factoryauto = new CreateAutoIntFactory(this);
		factoryauto.CreateAllFactories();
		factoryauto = new CreateAutoStrFactory(this);
		factoryauto.CreateAllFactories();

		CreateAttrFactory factoryattr = new CreateIntFactory(this);
		factoryattr.CreateAllFactories();
		factoryattr = new CreateStrFactory(this);
		factoryattr.CreateAllFactories();


        LogInfo("Ok");
    }
	

}