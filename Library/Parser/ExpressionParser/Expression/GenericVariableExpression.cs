using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Vici.Core.Parser
{

    /// <summary>
    /// Represents a Generic Variable
    /// </summary>
    public class GenericVariableExpression : Expression
    {
        /// <summary>
        /// The name of the variable
        /// </summary>
        private readonly string _varName;

        /// <summary>
        /// The list of generic types
        /// </summary>
        private readonly Type[] _genericTypes;


        /// <summary>
        /// Create an instance
        /// </summary>
        /// <param name="position">The token position</param>
        /// <param name="varName">The variable name</param>
        public GenericVariableExpression(TokenPosition position, string varName)
            : base(position)
        {
            // now then, the varName is of the form...actual variable name < ,,,,>
            int genericStart = varName.IndexOf('<');

            _varName = varName.Left(varName.IndexOf('<'));

            // now we need to strip the ','s out of the 
            string[] genericNames = varName.Substring(genericStart + 1, (varName.Length - 1) - (genericStart + 1)).Split(',');

            // now have a list of types as strings, we need to construct a list of Types
            List<Type> resolvedTypes = new List<Type>();

            foreach (string genericName in genericNames)
            {
                resolvedTypes.Add(GetTypeFromSimpleName(genericName));
            }

            _genericTypes = resolvedTypes.ToArray();
        }

        /// <summary>
        /// For a type name, return the associated Type.
        /// </summary>
        /// <param name="typeName">The name of the type</param>
        /// <returns>A <see cref="Type"/></returns>
        /// <remarks>Accomodates those types which the compiler knows about e.g. int and uses a fully qualified name for that type.</remarks>
        private Type GetTypeFromSimpleName(string typeName)
        {
            if (typeName == null)
                throw new ArgumentNullException("typeName");

            bool isArray = false, isNullable = false;

            if (typeName.IndexOf("[]") != -1)
            {
                isArray = true;
                typeName = typeName.Remove(typeName.IndexOf("[]"), 2);
            }

            if (typeName.IndexOf("?") != -1)
            {
                isNullable = true;
                typeName = typeName.Remove(typeName.IndexOf("?"), 1);
            }

            typeName = typeName.ToLower();

            string parsedTypeName = null;
            switch (typeName)
            {
                case "bool":
                case "boolean":
                    parsedTypeName = "System.Boolean";
                    break;
                case "byte":
                    parsedTypeName = "System.Byte";
                    break;
                case "char":
                    parsedTypeName = "System.Char";
                    break;
                case "datetime":
                    parsedTypeName = "System.DateTime";
                    break;
                case "datetimeoffset":
                    parsedTypeName = "System.DateTimeOffset";
                    break;
                case "decimal":
                    parsedTypeName = "System.Decimal";
                    break;
                case "double":
                    parsedTypeName = "System.Double";
                    break;
                case "float":
                    parsedTypeName = "System.Single";
                    break;
                case "int16":
                case "short":
                    parsedTypeName = "System.Int16";
                    break;
                case "int32":
                case "int":
                    parsedTypeName = "System.Int32";
                    break;
                case "int64":
                case "long":
                    parsedTypeName = "System.Int64";
                    break;
                case "object":
                    parsedTypeName = "System.Object";
                    break;
                case "sbyte":
                    parsedTypeName = "System.SByte";
                    break;
                case "string":
                    parsedTypeName = "System.String";
                    break;
                case "timespan":
                    parsedTypeName = "System.TimeSpan";
                    break;
                case "uint16":
                case "ushort":
                    parsedTypeName = "System.UInt16";
                    break;
                case "uint32":
                case "uint":
                    parsedTypeName = "System.UInt32";
                    break;
                case "uint64":
                case "ulong":
                    parsedTypeName = "System.UInt64";
                    break;
            }

            if (parsedTypeName != null)
            {
                if (isArray)
                    parsedTypeName = parsedTypeName + "[]";

                if (isNullable)
                    parsedTypeName = String.Concat("System.Nullable`1[", parsedTypeName, "]");
            }
            else
                parsedTypeName = typeName;

            return Type.GetType(parsedTypeName);
        }

        /// <summary>
        /// Get the variable name
        /// </summary>
        public string VarName
        {
            get { return _varName; }
        }

        /// <summary>
        /// Get the array of generic types.
        /// </summary>
        public Type[] Generics
        {
            get { return _genericTypes; }
        }

        public override ValueExpression Evaluate(IParserContext context)
        {
            // we aren't a variable, so such be treated as such
            //if (!context.Get(VarName, out value, out type))
            //    return new ValueExpression(TokenPosition, null,typeof(object));

            if (_genericTypes.Length != 0)
            {
                throw new ArgumentException("this is an error");
            }
            else
            {
                object value;
                Type type;

                if (!context.Get(VarName, out value, out type))
                    return new ValueExpression(TokenPosition, null, typeof(object));

                return new ValueExpression(TokenPosition, value, type);
            }

        }

        public override string ToString()
        {
            return _varName;
        }

    }
}
