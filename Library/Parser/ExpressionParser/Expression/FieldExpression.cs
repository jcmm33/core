#region License
//=============================================================================
// Vici Core - Productivity Library for .NET 3.5 
//
// Copyright (c) 2008-2012 Philippe Leybaert
//
// Permission is hereby granted, free of charge, to any person obtaining a copy 
// of this software and associated documentation files (the "Software"), to deal 
// in the Software without restriction, including without limitation the rights 
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell 
// copies of the Software, and to permit persons to whom the Software is 
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in 
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR 
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, 
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE 
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER 
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING 
// FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS
// IN THE SOFTWARE.
//=============================================================================
#endregion

using System;
using System.Reflection;
using System.Collections.Generic;

namespace Vici.Core.Parser
{
    public class FieldExpression : Expression
    {
        private readonly Expression _target;
        private readonly string _member;

        /// <summary>
        /// An array of generic Types
        /// </summary>
        private readonly Type[] _genericTypes;

        /// <summary>
        /// Create a field expression.
        /// </summary>
        /// <param name="position"></param>
        /// <param name="target"></param>
        /// <param name="member"></param>
        /// <param name="generics">An optional collection of genericTypes</param>
        public FieldExpression(TokenPosition position, Expression target, string member, Type[] genericTypes = null)
            : base(position)
        {
            _target = target;
            _member = member;
            _genericTypes = genericTypes;
        }

        public override ValueExpression Evaluate(IParserContext context)
        {
        	return Evaluate(context, false, null);
        }

		private ValueExpression Evaluate(IParserContext context, bool assign, object newValue)
    	{
    		ValueExpression targetValue = _target.Evaluate(context);
    		object targetObject;
    		Type targetType;

    		if (targetValue.Value is ClassName)
    		{
    			targetType = ((ClassName) targetValue.Value).Type;
    			targetObject = null;
    		}
    		else
    		{
    			targetType = targetValue.Type;
    			targetObject = targetValue.Value;

                if (targetObject == null)
                    return new ValueExpression(TokenPosition, null, targetType);
    		}

            if (targetObject is IDynamicObject)
            {
                object value;
                Type type;

                if (((IDynamicObject)targetObject).TryGetValue(_member, out value, out type))
                    return new ValueExpression(TokenPosition, value,type);
            }

            // Resolve the matching members 
            MemberInfo[] members = FindMemberInHierarchy(targetType, _member, _genericTypes);// targetType.GetMember(_member);

    		if (members.Length == 0)
    		{
                PropertyInfo indexerPropInfo = targetType.Inspector().GetIndexer(new[] { typeof(string) });

                if (indexerPropInfo != null)
                {
                    return new ValueExpression(TokenPosition, indexerPropInfo.GetValue(targetObject, new object[] { _member }), indexerPropInfo.PropertyType);
                }

    			throw new UnknownPropertyException("Unknown property " + _member + " for object " + _target + " (type " + targetType.Name + ")", this);
    		}


            // Use the existing reflected members which have already had the generic(s) if any applied
            if (members.Length >= 1 && members[0] is MethodInfo)
            {
                MethodInfo[] methodInfo = members.ConvertAll<MemberInfo, MethodInfo>((p) => p as MethodInfo);

                if (targetObject == null)
                    return Exp.Value(TokenPosition, new StaticMethod(methodInfo));
                else
                    return Exp.Value(TokenPosition, new InstanceMethod(methodInfo, targetObject));
            }

    		MemberInfo member = members[0];

    		if (members.Length > 1 && targetObject != null) // CoolStorage, ActiveRecord and Dynamic Proxy frameworks sometimes return > 1 member
    		{
    			foreach (MemberInfo mi in members)
    				if (mi.DeclaringType == targetObject.GetType())
    					member = mi;
    		}

			if (assign)
			{
				if (member is FieldInfo)
					((FieldInfo) member).SetValue(targetObject, newValue);

				if (member is PropertyInfo)
					((PropertyInfo) member).SetValue(targetObject, newValue, null);

				// Fall through to get the new property/field value below
			}

			if (member is FieldInfo)
				return new ValueExpression(TokenPosition, ((FieldInfo) member).GetValue(targetObject), ((FieldInfo) member).FieldType);

			if (member is PropertyInfo)
    			return new ValueExpression(TokenPosition, ((PropertyInfo)member).GetValue(targetObject, null), ((PropertyInfo)member).PropertyType);

    		throw new ExpressionEvaluationException(_member + " is not a field or property", this);
    	}

        private static MemberInfo[] FindMemberInHierarchy(Type type, string name)
        {
            Type t = type;

            while (t != null)
            {
                MemberInfo[] members = t.Inspector().GetMember(name);

                if (members.Length > 0)
                    return members;

                t = t.Inspector().BaseType;
            }

            return new MemberInfo[0];
        }

        /// <summary>
        /// Find matching members for a type, matching on a name and specified generic types.
        /// </summary>
        /// <param name="type">The type to search</param>
        /// <param name="name">The name of the member</param>
        /// <param name="genericNames">An array of types</param>
        /// <returns>An array of matching <see cref="MemberInfo"/> instances.</returns>
        private static MemberInfo[] FindMemberInHierarchy(Type type, string name, Type[] genericNames)
        {
            // if we don't have any generics specified, then use the default model
            if (genericNames == null || genericNames.Length == 0)
            {
                return FindMemberInHierarchy(type, name);

            }

            Type t = type;

            MemberInfo[] members = t.Inspector().GetMember(name, genericNames);

            // if we have members return it
            if (members.Length > 0)
            {
                return members;
            }

            // return empty array
            return new MemberInfo[0];
        }

    	public override string ToString()
        {
            return "(" + _target + "." + _member + ")";
        }


    	public ValueExpression Assign(IParserContext context, object value)
    	{
    		return Evaluate(context, true, value);
    	}
    }
}