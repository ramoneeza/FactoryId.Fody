using Mono.Cecil.Cil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Mono.Cecil
{
	public static class CecilHelper
	{
		public static FieldDefinition AddOrGetField(this TypeDefinition basetype,string fieldname,FieldAttributes attributes,TypeReference fieldtype)
		{
			var old_field = basetype.Fields.FirstOrDefault(f => (f.Name == fieldname)&&(f.Attributes.HasFlag(attributes)));
			if (old_field != null) return old_field;
			var new_field = new FieldDefinition(fieldname,attributes, fieldtype);
			basetype.Fields.Add(new_field);
			return new_field;
		}
		public static FieldDefinition AddOrGetField(this TypeDefinition basetype, string fieldname,bool @static,bool @private, TypeReference fieldtype)
		{
			var attr = (@static ? FieldAttributes.Static : 0) | (@private ? FieldAttributes.Private : FieldAttributes.Public);
			return AddOrGetField(basetype, fieldname, attr, fieldtype);
		}
		public static MethodDefinition AddOrGetMethod_0(this TypeDefinition basetype, string methodname, bool @static, bool @private, TypeReference returntype)
		{
			var old_m = basetype.Methods.FirstOrDefault(f => (f.Name == methodname) && !f.HasParameters);
			if (old_m != null) return old_m;
			var new_m = new MethodDefinition(methodname, (@static ? MethodAttributes.Static : 0) | (@private ? MethodAttributes.Private : MethodAttributes.Public), basetype);
			basetype.Methods.Add(new_m);
			return new_m;
		}
		public static MethodDefinition AddOrGetMethod_1(this TypeDefinition basetype, string methodname, bool @static, bool @private,string arg0name,TypeReference arg0, TypeReference returntype)
		{
			var old_m = basetype.Methods.FirstOrDefault(f => (f.Name == methodname) &&  (f.Parameters.Count==1)&&(f.Parameters[0].ParameterType.FullName==arg0.FullName));
			if (old_m != null) return old_m;
			var new_m = new MethodDefinition(methodname, (@static ? MethodAttributes.Static : 0) | (@private ? MethodAttributes.Private : MethodAttributes.Public), basetype);
			new_m.Parameters.Add(new ParameterDefinition(arg0name, ParameterAttributes.None, arg0));
			basetype.Methods.Add(new_m);
			return new_m;
		}
		public static void InsertBlockBefore(this ILProcessor il,Instruction target,IEnumerable<Instruction> instructions)
		{
			foreach (var i in instructions)
				il.InsertBefore(target, i);
		}
		public static void AppendBlock(this ILProcessor il, IEnumerable<Instruction> instructions)
		{
			foreach (var i in instructions)
				il.Append(i);
		}
		public static IEnumerable<KeyValuePair<string,int>> EnumValues(this TypeDefinition t)
		{
			if (!t.IsEnum) throw new InvalidCastException("Not an Enum TypeDefinition");
			var enumvalues = t.Fields.Where(f => f.IsStatic);
			foreach (var f in enumvalues)
			{
				var i = (int)f.Constant;
				var str = f.FullName;
				str = str.Substring(str.LastIndexOf("::") + 2);
				yield return new KeyValuePair<string, int>(str, i);
			}
		}
	

		public static IEnumerable<TypeDefinition> GetTypesWithCustomAttr(this ModuleDefinition md,string attr)
		{
			return md.Types.WithCustomAttr(attr);
		}
		public static IEnumerable<TypeDefinition> WithCustomAttr(this IEnumerable<TypeDefinition> list,string attr)
		{
			return list.Where(t => t.HasCustomAttribute(attr));
		}
		public static IEnumerable<TypeDefinition> SubClassOf(this IEnumerable<TypeDefinition> list, string basename)
		{
			return list.Where(t => t.IsSubClassOf(basename));
		}
		public static IEnumerable<TypeDefinition> SubClassOf(this IEnumerable<TypeDefinition> list, TypeDefinition @base)
		{
			return list.Where(t => t.IsSubClassOf(@base));
		}
		public static bool IsSubClassOf(this TypeDefinition t, TypeDefinition @base)
		{
			return t.IsSubClassOf(@base.FullName);
		}
		public static bool IsSubClassOf(this TypeDefinition t, string basename)
		{
			if (!t.IsClass) return false;
			if (t.FullName == basename) return false;
			if (t.BaseType == null) return false;
			var directbase = t.BaseType.Resolve();
			while (directbase?.FullName != basename)
			{
				if ((directbase == null) || (directbase.FullName == "System.Object")) return false;
				directbase = directbase.BaseType?.Resolve();
			}
			return directbase.FullName == basename;
		}
		public static bool HasCustomAttribute(this TypeDefinition t,string attr)
		{
			return t.CustomAttributes.Any(c => c.AttributeType.FullName == attr);
		}
		public static CustomAttribute GetCustomAttribute(this TypeDefinition t, string attr)
		{
			return t.CustomAttributes.FirstOrDefault(c => c.AttributeType.FullName == attr);
		}
	}

	public class InstructionCollection : List<Instruction>
	{
		public void Add(OpCode opcode) => base.Add(Instruction.Create(opcode));
		public void Add(OpCode opcode, ParameterDefinition parameter) => base.Add(Instruction.Create(opcode, parameter));
		public void Add(OpCode opcode, VariableDefinition variable) => base.Add(Instruction.Create(opcode, variable));
		public void Add(OpCode opcode, Instruction[] targets) => base.Add(Instruction.Create(opcode, targets));
		public void Add(OpCode opcode, Instruction target) => base.Add(Instruction.Create(opcode, target));
		public void Add(OpCode opcode, double value) => base.Add(Instruction.Create(opcode, value));
		public void Add(OpCode opcode, long value) => base.Add(Instruction.Create(opcode, value));
		public void Add(OpCode opcode, int value) => base.Add(Instruction.Create(opcode, value));
		public void Add(OpCode opcode, byte value) => base.Add(Instruction.Create(opcode, value));
		public void Add(OpCode opcode, sbyte value) => base.Add(Instruction.Create(opcode, value));
		public void Add(OpCode opcode, string value) => base.Add(Instruction.Create(opcode, value));
		public void Add(OpCode opcode, FieldReference field) => base.Add(Instruction.Create(opcode, field));
		public void Add(OpCode opcode, MethodReference method) => base.Add(Instruction.Create(opcode, method));
		public void Add(OpCode opcode, CallSite site) => base.Add(Instruction.Create(opcode, site));
		public void Add(OpCode opcode, TypeReference type) => base.Add(Instruction.Create(opcode, type));
		public void Add(OpCode opcode, float value) => base.Add(Instruction.Create(opcode, value));
		public void AddGen(OpCode opcode, object parameter)
		{
			if (parameter == null)
			{
				Add(opcode);
				return;
			}
			var t = parameter.GetType().Name;
			switch (t)
			{
				case "ParameterDefinition": Add(opcode, (ParameterDefinition)parameter); break;
				case "VariableDefinition": Add(opcode, (VariableDefinition)parameter); break;
				case "Instruction": Add(opcode, (Instruction)parameter); break;
				case "double": Add(opcode, (double)parameter); break;
				case "long": Add(opcode, (long)parameter); break;
				case "int": Add(opcode, (int)parameter); break;
				case "byte": Add(opcode, (byte)parameter); break;
				case "sbyte": Add(opcode, (sbyte)parameter); break;
				case "string": Add(opcode, (string)parameter); break;
				case "FieldReference": Add(opcode, (FieldReference)parameter); break;
				case "FieldDefinition": Add(opcode, (FieldReference)parameter); break;
				case "MethodReference": Add(opcode, (MethodReference)parameter); break;
				case "MethodDefinition": Add(opcode, (MethodReference)parameter); break;
				case "CallSite": Add(opcode, (CallSite)parameter); break;
				case "TypeReference": Add(opcode, (TypeReference)parameter); break;
				case "TypeDefinition": Add(opcode, (TypeReference)parameter); break;
				case "float": Add(opcode, (float)parameter); break;
				default: throw new InvalidCastException("Bad parameter");
			}
		}
		public void AddBlock(params object[] opcodes)
		{
			var i = 0;
			while (i < opcodes.Length)
			{
				OpCode op = (OpCode)opcodes[i];
				var arg = (i==opcodes.Length-1)?null:opcodes[i + 1];
				if (arg is OpCode)
				{
					Add(op);
					i++;
				}
				else
				{
					AddGen(op, arg);
					i += 2;
				}
			}
		}

	}
}
