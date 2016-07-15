using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

public class SubClass
{
	public readonly TypeDefinition ClassDefinition;
	public object Value { get; set; }
	public readonly TypeDefinition Base;

	public static SubClass FactoryValue(TypeDefinition subclass, TypeDefinition @base, object value)
	{
		return new SubClass(subclass,@base,value);
	}
	public static SubClass FactoryAttr(TypeDefinition subclass, TypeDefinition @base, CustomAttribute value)
		{
			var c = new SubClass(subclass,@base);
			var v = value.ConstructorArguments[0].Value;
			c.Value = v;
			return c;
		}
	
	private SubClass(TypeDefinition subclass, TypeDefinition @base, object value=null)
	{
		ClassDefinition = subclass;
		Base = @base;
		Value = value;
	}
}
public abstract class CreateFactory
{
	public ModuleWeaver Parent { get; }
	public ModuleDefinition ModuleDefinition => Parent.ModuleDefinition;
	public abstract string StrType { get; }
	public abstract Type FactoryType{get;}
	public readonly TypeReference FactoryTypeReference;
	public abstract string BaseAttr { get; }
	public string FactoryData => $"_FactoryData{StrType}";
	public string FactoryMethod => $"Factory{StrType}";
	public string PropertyKey=> $"Factory{StrType}Key";

	public const string Namespace = "FactoryId";
	public readonly Type TypeDictionary;
	public readonly TypeReference DicType;
	public readonly MethodReference DicConstructor;
	public readonly MethodReference DicAdd;
	public readonly MethodReference DicContainsKey;
	public readonly MethodReference DicItem;
	public readonly MethodReference TypeFromHandle;
	public readonly MethodReference TypeCreateInstance;

	public CreateFactory(ModuleWeaver parent)
	{
		Parent = parent;
		FactoryTypeReference = ModuleDefinition.ImportReference(FactoryType);
		TypeDictionary = Type.GetType($"System.Collections.Generic.Dictionary`2[{FactoryType.FullName},System.Type]");
		DicType = ModuleDefinition.ImportReference(TypeDictionary);
		DicConstructor =ModuleDefinition.ImportReference(TypeDictionary.GetConstructor(Type.EmptyTypes));
		DicAdd =ModuleDefinition.ImportReference(TypeDictionary.GetMethod("Add"));
		DicContainsKey = ModuleDefinition.ImportReference(TypeDictionary.GetMethod("ContainsKey"));
		DicItem = ModuleDefinition.ImportReference(TypeDictionary.GetProperty("Item").GetGetMethod());
		TypeFromHandle = ModuleDefinition.ImportReference(typeof(System.Type).GetMethod("GetTypeFromHandle"));
		TypeCreateInstance = ModuleDefinition.ImportReference(typeof(System.Activator).GetMethod("CreateInstance",new Type[] { typeof(Type)}));
	}

	protected FieldDefinition AddOrGetFactoryDataField(TypeDefinition basetype)=> basetype.AddOrGetField(FactoryData, true, true, DicType);
	protected MethodDefinition AddOrGetFactoryMethod(TypeDefinition basetype)
	{
		var fielddef = basetype.Fields.FirstOrDefault(f => (f.Name == FactoryData) && (f.IsStatic));
		if (fielddef == null) return null;
		var factorymember = basetype.AddOrGetMethod_1(FactoryMethod, true, false, "key", FactoryTypeReference, basetype);
		factorymember.Body.Instructions.Clear();
		factorymember.Body.Variables.Clear();
		factorymember.Body.InitLocals = true;
		var tempVar = new VariableDefinition(Parent.ModuleDefinition.TypeSystem.Boolean);
		factorymember.Body.Variables.Add(tempVar);
		tempVar = new VariableDefinition(basetype);
		factorymember.Body.Variables.Add(tempVar);

		var il=factorymember.Body.GetILProcessor();
		// Insert ret
		il.Append(Instruction.Create(OpCodes.Ldloc_1));
		il.Append(Instruction.Create(OpCodes.Ret));
		var first = factorymember.Body.Instructions[0];
		var inst = new InstructionCollection();
		inst.AddBlock(OpCodes.Nop,
						OpCodes.Ldnull,
						OpCodes.Stloc_1,
						OpCodes.Ldsfld, fielddef,
						OpCodes.Ldarg_0,
						OpCodes.Callvirt, DicContainsKey,
						OpCodes.Stloc_0,
						OpCodes.Ldloc_0,
						OpCodes.Brfalse_S, first,
						OpCodes.Nop,
						OpCodes.Ldsfld, fielddef,
						OpCodes.Ldarg_0,
						OpCodes.Callvirt, DicItem,
						OpCodes.Call, TypeCreateInstance,
						OpCodes.Castclass, basetype,
						OpCodes.Stloc_1,
						OpCodes.Br_S, first,
						OpCodes.Nop);
		il.InsertBlockBefore(first, inst);
		return factorymember;
	}
	protected MethodDefinition AddOrGetStaticConstructor(TypeDefinition basetype)
	{
		var ctor = basetype.GetStaticConstructor();
		if (ctor == null) ctor=AddEmptyStaticConstructor(basetype);
		return ctor;
	}
	private MethodDefinition AddEmptyStaticConstructor(TypeDefinition basetype)
	{
		var methodAttributes = MethodAttributes.Private | MethodAttributes.Static | MethodAttributes.HideBySig | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName;
		var method = new MethodDefinition(".cctor", methodAttributes, Parent.ModuleDefinition.TypeSystem.Void);
		method.Body.Instructions.Add(Instruction.Create(OpCodes.Nop));
		method.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));
		basetype.Methods.Add(method);
		return method;
	}
	protected IEnumerable<Instruction> InitFactory(FieldDefinition field)
	{
		var res = new InstructionCollection();
		res.AddBlock(OpCodes.Nop,
					 OpCodes.Newobj, DicConstructor,
					 OpCodes.Stsfld, field);
		return res;
	}
	protected abstract object GetSubClassValue(TypeDefinition subclass, TypeDefinition basetype);
	protected abstract Instruction InjectSubClassValue(object value);
	protected IEnumerable<Instruction> RegisterValue(FieldDefinition field,SubClass subclass)
	{
		if (subclass.Value == null) return null;
		var res = new InstructionCollection();
		res.Add(OpCodes.Ldsfld, field);
		res.Add(InjectSubClassValue(subclass.Value));
		res.AddBlock(OpCodes.Ldtoken, subclass.ClassDefinition,
					 OpCodes.Call, TypeFromHandle,
					 OpCodes.Callvirt, DicAdd);
		return res;
	}
	

	public IEnumerable<TypeDefinition> GetBaseTypes()=>ModuleDefinition.GetTypesWithCustomAttr(BaseAttr);
	public abstract List<SubClass> GetSubClasses(TypeDefinition @base);

	public virtual void CreateAllFactories()
	{
		var bases = GetBaseTypes();
		foreach(var b in bases)
		{
			var subclasses = GetSubClasses(b);
			CreateOneFactory(b, subclasses);
			foreach(var s in subclasses)
				WeaverSubClass(s);
		}
	}
	public abstract void CreateOneFactory(TypeDefinition basetype, List<SubClass> registersubclasses);

	protected void WeaverSubClass(SubClass subclass)
	{
		if (subclass.Value == null) return;
		var prop = new PropertyDefinition(PropertyKey, PropertyAttributes.None, FactoryTypeReference);
		prop.HasThis = false;
		var gm = new MethodDefinition("get_" + PropertyKey,
					MethodAttributes.Static | MethodAttributes.Public |
					MethodAttributes.SpecialName | MethodAttributes.HideBySig,
					FactoryTypeReference
					);
		subclass.ClassDefinition.Methods.Add(gm);
		gm.Body.Instructions.Clear();
		gm.Body.Variables.Clear();
		gm.Body.InitLocals = true;
		gm.Body.Variables.Add(new VariableDefinition(FactoryTypeReference));
		var il = gm.Body.GetILProcessor();
		var inst = new InstructionCollection();
		inst.Add(InjectSubClassValue(subclass.Value));
		inst.AddBlock(
			OpCodes.Stloc_0,
			OpCodes.Ldloc_0,
			OpCodes.Ret
			);
		il.AppendBlock(inst);
		prop.GetMethod = gm;
		prop.SetMethod = null;
		subclass.ClassDefinition.Properties.Add(prop);
	}
}
public abstract class CreateAutoFactory : CreateFactory
{
	public override string BaseAttr  => $"{Namespace}.Factory{StrType}BaseAutoAttribute";
	public string PrefixAttr => $"{Namespace}.FactoryPrefixAttribute";
	public string ExcludeAttr => $"{Namespace}.FactoryExcludeAttribute";

	private Dictionary<TypeDefinition, string> CachePrefix = new Dictionary<TypeDefinition, string>();
	public string GetPrefix(TypeDefinition basetype)
	{
		string prefix = "";
		if (!CachePrefix.TryGetValue(basetype,out prefix))
		{
			var attr = basetype.CustomAttributes.FirstOrDefault(c => c.AttributeType.FullName == PrefixAttr);
			if ((attr != null) && (attr.HasConstructorArguments))
				prefix = (string)attr.ConstructorArguments[0].Value;
			else
				prefix = basetype.Name;
			CachePrefix[basetype] = prefix;
		}
		return prefix;
	}
	public CreateAutoFactory(ModuleWeaver parent) : base(parent)
	{
	}
	
	public override void CreateOneFactory(TypeDefinition basetype,List<SubClass> registersubclasses)
	{
		
		var field = AddOrGetFactoryDataField(basetype);
		var ctor = AddOrGetStaticConstructor(basetype);
		var first = ctor.Body.Instructions[0];
		var il = ctor.Body.GetILProcessor();
		var inst = InitFactory(field);
		il.InsertBlockBefore(first, inst);
		foreach(var s in registersubclasses)
		{
			s.Value = GetSubClassValue(s.ClassDefinition, basetype);
			var instadd = RegisterValue(field,s);
			if (instadd!=null) il.InsertBlockBefore(first, instadd);
		}
		var method = AddOrGetFactoryMethod(basetype);
		
	}
	protected override object GetSubClassValue(TypeDefinition subclass, TypeDefinition basetype)
	{
		if (subclass.CustomAttributes.Any(c => c.AttributeType.Name == ExcludeAttr)) return null;
		var prefix = GetPrefix(basetype);
		var name = subclass.Name;
		if (!name.StartsWith(prefix)) return null;
		name = name.Substring(prefix.Length);
		if (string.IsNullOrWhiteSpace(name)) return null;
		return name;
	}
}
public class CreateAutoIntFactory : CreateAutoFactory
{
	private class EnumValues : Dictionary<string, int>{
		public Type EnumType { get; }
		public EnumValues(TypeDefinition enumtype):base()
		{
			var enumvalues = enumtype.EnumValues();
			foreach (var kv in enumvalues)
				Add(kv.Key, kv.Value);
		}
		public static int? GetValue(EnumValues v,string strv)
		{
			if ((v == null)||!v.ContainsKey(strv))
			{
				int i = 0;
				if (!int.TryParse(strv, out i))
					return null;
				else
					return i;
			}
			return v[strv];
		}
	}
	private Dictionary<TypeDefinition, EnumValues> CacheEnums = new Dictionary<TypeDefinition, EnumValues>();

	public CreateAutoIntFactory(ModuleWeaver parent) : base(parent)
	{
	}

	public int? GetEnumValue(TypeDefinition basetype, string stringvalue)
	{
		EnumValues values = null;
		if (!CacheEnums.TryGetValue(basetype, out values))
		{
			var attr = basetype.CustomAttributes.FirstOrDefault(c => c.AttributeType.FullName == BaseAttr);
			if ((attr != null) && (attr.HasConstructorArguments))
			{
				var elenum = attr.ConstructorArguments[0].Value as TypeDefinition;
				if (elenum != null) values = new EnumValues(elenum);
				CacheEnums[basetype] = values;
			}
			else
				CacheEnums[basetype] = null;
		}
		return EnumValues.GetValue(values, stringvalue);
	}
	public override string StrType => "Int";

	public override Type FactoryType => typeof(Int32);
	
	protected override object GetSubClassValue(TypeDefinition subclass,TypeDefinition basetype)
	{
		var str = (string)base.GetSubClassValue(subclass, basetype);
		if (string.IsNullOrEmpty(str)) return null;
		return GetEnumValue(basetype, str);
	}

	protected override Instruction InjectSubClassValue(object value)=>Instruction.Create(OpCodes.Ldc_I4, (int)value);

	public override List<SubClass> GetSubClasses(TypeDefinition @base)
	{
		var prefix = GetPrefix(@base);
		var ts= ModuleDefinition.Types.SubClassOf(@base);
		var res = new List<SubClass>();
		foreach (var t in ts)
		{
			if (!t.Name.StartsWith(prefix)) continue;
			res.Add(SubClass.FactoryValue(t, @base, null));
		}
		return res;
	}
}
public class CreateAutoStrFactory : CreateAutoFactory
{
	public CreateAutoStrFactory(ModuleWeaver parent) : base(parent)
	{
	}

	public override Type FactoryType => typeof(string);
	public override string StrType => "Str";

	public override List<SubClass> GetSubClasses(TypeDefinition @base)
	{
		var prefix = GetPrefix(@base);
		var ts = ModuleDefinition.Types.SubClassOf(@base);
		var res = new List<SubClass>();
		foreach (var t in ts)
		{
			if (!t.Name.StartsWith(prefix)) continue;
			res.Add(SubClass.FactoryValue(t, @base, null));
		}
		return res;
	}

	protected override Instruction InjectSubClassValue(object value)=>Instruction.Create(OpCodes.Ldstr, (string)value);
}

public abstract class CreateAttrFactory:CreateFactory
{
	public override string BaseAttr => $"{Namespace}.Factory{StrType}BaseAttribute";
	public string KeyAttr => $"{Namespace}.Factory{StrType}KeyAttribute";
	public CreateAttrFactory(ModuleWeaver parent) : base(parent)
	{
	}
	public override void CreateOneFactory(TypeDefinition basetype, List<SubClass> registersubclasses)
	{

		var field = AddOrGetFactoryDataField(basetype);
		var ctor = AddOrGetStaticConstructor(basetype);
		var first = ctor.Body.Instructions[0];
		var il = ctor.Body.GetILProcessor();
		var inst = InitFactory(field);
		il.InsertBlockBefore(first, inst);
		foreach (var s in registersubclasses)
		{
			s.Value = GetSubClassValue(s.ClassDefinition, basetype);
			if (s.Value == null) continue;
			var instadd = RegisterValue(field, s);
			if (instadd != null) il.InsertBlockBefore(first, instadd);
		}
		AddOrGetFactoryMethod(basetype);

	}
	protected override object GetSubClassValue(TypeDefinition subclass, TypeDefinition basetype)
	{
		var attrvalue = subclass.GetCustomAttribute(KeyAttr);
		if (attrvalue == null) return null;
		return attrvalue.ConstructorArguments[0].Value;
	}
	public override List<SubClass> GetSubClasses(TypeDefinition @base)
	{
		var ts = ModuleDefinition.GetTypesWithCustomAttr(KeyAttr).SubClassOf(@base);
		var res = new List<SubClass>();
		foreach (var t in ts)
		{
			res.Add(SubClass.FactoryValue(t, @base, null));
		}
		return res;
	}
}
public class CreateIntFactory : CreateAttrFactory
{
	public CreateIntFactory(ModuleWeaver parent) : base(parent)	{}
	public override string StrType => "Int";

	public override Type FactoryType => typeof(Int32);
	protected override Instruction InjectSubClassValue(object value)=>Instruction.Create(OpCodes.Ldc_I4, (int)value);
}
public class CreateStrFactory : CreateAttrFactory
{
	public CreateStrFactory(ModuleWeaver parent) : base(parent)	{}
	public override string StrType => "Str";

	public override Type FactoryType => typeof(string);
	protected override Instruction InjectSubClassValue(object value)=>Instruction.Create(OpCodes.Ldstr, (string)value);
}
