﻿using System;
using System.Collections.Generic;
using System.Linq;
using Skrypt.Engine;
using Skrypt.Library;
using Skrypt.Parsing;

namespace Skrypt.Execution
{
    public class Executor
    {
        private readonly SkryptEngine _engine;

        public Executor(SkryptEngine e)
        {
            _engine = e;
        }

        public Variable GetVariable (string name, ScopeContext scopeContext) {
            Variable foundVar = null;

            if (scopeContext.Variables.ContainsKey(name)) {
                foundVar = scopeContext.Variables[name];
            } else if (scopeContext.ParentScope != null) {
                foundVar = GetVariable(name, scopeContext.ParentScope);
            }

            return foundVar;
        }

        public SkryptObject GetType(string name, ScopeContext scopeContext) {
            SkryptObject foundVar = null;

            if (scopeContext.Types.ContainsKey(name)) {
                foundVar = scopeContext.Types[name];
            }
            else if (scopeContext.ParentScope != null) {
                foundVar = GetType(name, scopeContext.ParentScope);
            }

            return foundVar;
        }

        private bool CheckCondition(Node node, ScopeContext scopeContext)
        {
            var conditionResult = false;

            conditionResult = _engine.Executor.ExecuteExpression(node, scopeContext).ToBoolean();

            return conditionResult;
        }

        public void ExecuteWhileStatement(Node node, ScopeContext scopeContext)
        {
            while (true)
            {
                var conditionResult = CheckCondition(node.SubNodes[0].SubNodes[0], scopeContext);

                if (!conditionResult) break;

                ExecuteBlock(node.SubNodes[1], scopeContext);
            }
        }

        public void ExecuteIfStatement(Node node, ScopeContext scopeContext)
        {
            var conditionResult = CheckCondition(node.SubNodes[0].SubNodes[0], scopeContext);

            if (conditionResult)
            {
                ExecuteBlock(node.SubNodes[1], scopeContext);
                return;
            }

            if (node.SubNodes.Count > 2)
                for (var i = 2; i < node.SubNodes.Count; i++)
                {
                    var elseNode = node.SubNodes[i];

                    if (elseNode.Body == "elseif")
                    {
                        conditionResult = _engine.Executor
                            .ExecuteExpression(elseNode.SubNodes[0].SubNodes[0], scopeContext).ToBoolean();

                        if (conditionResult)
                        {
                            ExecuteBlock(elseNode.SubNodes[1], scopeContext);
                            return;
                        }
                    }
                    else
                    {
                        ExecuteBlock(elseNode, scopeContext);
                    }
                }
        }

        public SkryptObject ExecuteClassDeclaration (Node node, ScopeContext scopeContext, SkryptObject ParentClass = null) {
            string ClassName = node.Body;

            if (ParentClass != null) {
                ClassName = ParentClass.Name + "." + ClassName;
            } 

            SkryptObject Object = new SkryptObject { Name = ClassName };
            SkryptType TypeObject = new SkryptType { Name = ClassName };

            for (int i = 0; i < node.SubNodes.Count; i++) {
                Node PropertyNode = node.SubNodes[i];
                var Properties = PropertyNode.SubNodes[0].SubNodes;
                SkryptProperty Property = new SkryptProperty();

                if (PropertyNode.SubNodes[1].Body == "assign") {
                    Property.Name = PropertyNode.SubNodes[1].SubNodes[0].Body;
                    Property.Value = ExecuteExpression(PropertyNode.SubNodes[1].SubNodes[1], scopeContext);
                } else if (PropertyNode.SubNodes[1].TokenType == "MethodDeclaration") {
                    Property.Name = PropertyNode.SubNodes[1].Body;
                    Property.Value = ExecuteMethodDeclaration(PropertyNode.SubNodes[1], scopeContext);

                    if (Property.Name == "Constructor") {
                        Property.Accessibility = Access.Private;
                        Object.Properties.Add(Property);

                        Object.Properties.Add(new SkryptProperty {
                            Name = "TypeName",
                            Value = new Library.Native.System.String(ClassName)
                        });

                        scopeContext.AddType(ClassName,TypeObject);
                    }
                } else if (PropertyNode.SubNodes[1].TokenType == "ClassDeclaration") {
                    Property.Name = PropertyNode.SubNodes[1].Body;
                    Property.Value = ExecuteClassDeclaration(PropertyNode.SubNodes[1], scopeContext);
                }

                if (Properties.Find(x => x.Body == "static") != null) {
                    Object.Properties.Add(Property);
                } else {
                    TypeObject.Properties.Add(Property);
                }

                foreach (var p in Properties) {
                    switch (p.Body) {
                        case "private":
                            Property.Accessibility = Access.Private;
                            break;
                        case "public":
                            Property.Accessibility = Access.Public;
                            break;
                        case "constant":
                            Property.IsConstant = true;
                            break;
                    }
                }
            }

            return Object;
        }

        public UserMethod ExecuteMethodDeclaration (Node node, ScopeContext scopeContext) {
            foreach (KeyValuePair<string, Variable> pair in scopeContext.Variables.Where((p) => p.Value.Value.Name == "method")) {
                if (pair.Value.Name == node.Body) {
                    _engine.ThrowError("A method with this signature already exists in this context!", node.Token);
                }
            }

            UserMethod result = new UserMethod {
                Name = "method",
                Signature = node.Body,
                BlockNode = node.SubNodes[0],
                CallName = node.Body
            };

            foreach (Node snode in node.SubNodes[1].SubNodes) {
                result.Parameters.Add(snode.Body);
            }

            return result;
        }

        public ScopeContext ExecuteBlock (Node node, ScopeContext scopeContext, SubContext subContext = null) {
            ScopeContext scope = new ScopeContext();

            if (scopeContext != null)
            {
                scope.SubContext = scopeContext.SubContext;
                scope.ParentScope = scopeContext;
            }

            if (subContext != null) scope.SubContext = subContext;

            foreach (var subNode in node.SubNodes)
                if (subNode.TokenType == "Statement")
                {
                    switch (subNode.Body)
                    {
                        case "while":
                            ExecuteWhileStatement(subNode, scope);
                            break;
                        case "if":
                            ExecuteIfStatement(subNode, scope);
                            break;
                    }

                    if (scope.SubContext.ReturnObject != null) return scope;
                }
                else if (subNode.TokenType == "MethodDeclaration") {
                    var result = ExecuteMethodDeclaration(subNode, scope);

                    scope.AddVariable(result.CallName, result);
                }
                else if (subNode.TokenType == "ClassDeclaration") {
                    var Object = ExecuteClassDeclaration(subNode, scope);

                    scope.AddVariable(Object.Name, Object);
                }
                else
                {
                    var result = _engine.Executor.ExecuteExpression(subNode, scope);

                    if (scope.SubContext.ReturnObject != null) return scope;
                }

            return scope;
        }

        public SkryptProperty GetProperty(SkryptObject Object, string toFind)
        {
            var find = Object.Properties.Find(x => x.Name == toFind);

            if (find == null) _engine.ThrowError("Object does not contain property '" + toFind + "'!");

            if (find.Accessibility == Access.Private) {
                _engine.ThrowError("Property '" + toFind + "' is inaccessable due to its protection level.");
            }

            return find;
        }

        public SkryptProperty ExecuteAccess(SkryptObject Object, Node node, ScopeContext scopeContext)
        {
            if (node.SubNodes.Count == 0) return GetProperty(Object, node.Body);

            var property = GetProperty(Object, node.SubNodes[0].Body);

            if (node.SubNodes[1].Body == "access")
                return ExecuteAccess(property.Value, node.SubNodes[1], scopeContext);
            return property;
        }

        public SkryptObject ExecuteExpression(Node node, ScopeContext scopeContext)
        {
            var op = Operator.AllOperators.Find(o => o.OperationName == node.Body);

            if (op != null)
            {
                if (op.OperationName == "return")
                {
                    if (!scopeContext.SubContext.InMethod)
                        _engine.ThrowError("Can't use return operator outside method!", node.SubNodes[0].Token);

                    SkryptObject result = null;

                    result = node.SubNodes.Count == 1
                        ? ExecuteExpression(node.SubNodes[0], scopeContext)
                        : new Library.Native.System.Void();

                    scopeContext.SubContext.ReturnObject = result;
                    return result;
                }

                if (op.OperationName == "access")
                {
                    var target = ExecuteExpression(node.SubNodes[1], scopeContext);
                    var result = ExecuteAccess(target, node.SubNodes[0], scopeContext).Value;

                    if (scopeContext.SubContext.GettingCaller) scopeContext.SubContext.Caller = target;

                    return result;
                }

                if (op.OperationName == "assign")
                {
                    var result = ExecuteExpression(node.SubNodes[1], scopeContext);

                    if (result.GetType().IsSubclassOf(typeof(SkryptType)))
                        if (((SkryptType) result).CreateCopyOnAssignment)
                            result = result.Clone();

                    if (node.SubNodes[0].SubNodes.Count == 0 && node.SubNodes[0].TokenType == "Identifier")
                    {
                        var variable = GetVariable(node.SubNodes[0].Body, scopeContext);

                        if (variable != null) {
                            if (variable.IsConstant)
                                _engine.ThrowError("Variable is marked as constant and can thus not be modified.");

                            variable.Value = result;
                        }
                        else {
                            scopeContext.AddVariable(node.SubNodes[0].Body, result);
                        }
                    }
                    else if (node.SubNodes[0].Body == "access")
                    {
                        var target = ExecuteExpression(node.SubNodes[0].SubNodes[1], scopeContext);
                        var accessResult = ExecuteAccess(target, node.SubNodes[0].SubNodes[0], scopeContext);

                        if (accessResult.IsConstant)
                            _engine.ThrowError("Property is marked as constant and can thus not be modified.");

                        accessResult.Value = result;
                    }
                    else if (node.SubNodes[0].Body == "Index")
                    {
                        ExecuteIndexSet(result, node.SubNodes[0], scopeContext);
                    }
                    else
                    {
                        _engine.ThrowError("Left hand side needs to be a variable or property!", node.SubNodes[0].Token);
                    }

                    return result;
                }

                if (op.Members == 2)
                {
                    var leftResult = ExecuteExpression(node.SubNodes[0], scopeContext);
                    var rightResult = ExecuteExpression(node.SubNodes[1], scopeContext);

                    dynamic left = Convert.ChangeType(leftResult, leftResult.GetType());
                    dynamic right = Convert.ChangeType(rightResult, rightResult.GetType());

                    Operation opLeft = left.GetOperation(op.OperationName, leftResult.GetType(), rightResult.GetType(),
                        left.Operations);
                    Operation opRight = right.GetOperation(op.OperationName, leftResult.GetType(),
                        rightResult.GetType(), right.Operations);

                    OperationDelegate operation = null;

                    if (opLeft != null)
                        operation = opLeft.OperationDelegate;
                    else if (opRight != null)
                        operation = opRight.OperationDelegate;
                    else
                        _engine.ThrowError("No such operation as " + left.Name + " " + op.Operation + " " + right.Name,
                            node.SubNodes[0].Token);

                    var result = (SkryptType) operation(new[] {leftResult, rightResult});

                    result.SetPropertiesTo(GetType(result.TypeName, scopeContext));
                  
                    return result;
                }

                if (op.Members == 1)
                {
                    var leftResult = ExecuteExpression(node.SubNodes[0], scopeContext);

                    dynamic left = Convert.ChangeType(leftResult, leftResult.GetType());

                    Operation opLeft = left.GetOperation(op.OperationName, leftResult.GetType(), null, left.Operations);

                    OperationDelegate operation = null;

                    if (opLeft != null)
                        operation = opLeft.OperationDelegate;
                    else
                        _engine.ThrowError("No such operation as " + left.Name + " " + op.Operation,
                            node.SubNodes[0].Token);

                    var result = (SkryptType) operation(new[] {leftResult});

                    result.SetPropertiesTo(GetType(result.TypeName, scopeContext));

                    return result;
                }
            }
            else if (node.TokenType == "ArrayLiteral")
            {
                var array = new Library.Native.System.Array();

                for (var i = 0; i < node.SubNodes.Count; i++)
                {
                    var subNode = node.SubNodes[i];

                    var result = ExecuteExpression(subNode, scopeContext);

                    if (result.Name == "void") _engine.ThrowError("Can't add void to array!", node.SubNodes[0].Token);

                    array.Value.Add(result);
                }

                array.SetPropertiesTo(GetType(array.TypeName, scopeContext));

              return array;
            }
            else if (node.SubNodes.Count == 0)
            {
                switch (node.TokenType)
                {
                    case "NumericLiteral":
                        var newNumeric = new Library.Native.System.Numeric(double.Parse(node.Body));
                        newNumeric.SetPropertiesTo(GetType(newNumeric.TypeName, scopeContext));

                        return newNumeric;
                    case "StringLiteral":
                        var newString = new Library.Native.System.String(node.Body);
                        newString.SetPropertiesTo(GetType(newString.TypeName, scopeContext));

                        return newString;
                    case "BooleanLiteral":
                        var newBool = new Library.Native.System.Boolean(node.Body == "true" ? true : false);
                        newBool.SetPropertiesTo(GetType(newBool.TypeName, scopeContext));

                        return newBool;
                    case "NullLiteral":
                        return new Library.Native.System.Null();
                }
            }
            else if (node.TokenType == "FunctionLiteral")
            {
                var result = new UserMethod
                {
                    Name = "method",
                    Signature = node.Body,
                    BlockNode = node.SubNodes[0],
                    CallName = node.Body.Split('_')[0]
                };

                foreach (var snode in node.SubNodes[1].SubNodes) result.Parameters.Add(snode.Body);

                return result;
            }
          
            if (node.TokenType == "Identifier")
            {
                var foundVariable = GetVariable(node.Body, scopeContext);

                if (foundVariable != null)
                    return foundVariable.Value;
                _engine.ThrowError("Variable '" + node.Body + "' does not exist in the current context!",
                    node.Token);
            }

            if (node.TokenType == "Index") return ExecuteIndex(node, scopeContext);

            if (node.TokenType == "Call")
            {
                var arguments = new List<SkryptObject>();
                var methodContext = new ScopeContext
                {
                    ParentScope = scopeContext
                };

                foreach (var subNode in node.SubNodes[1].SubNodes)
                {
                    var result = ExecuteExpression(subNode, scopeContext);

                    if (result.Name == "void")
                        _engine.ThrowError("Can't pass void into arguments!", node.SubNodes[0].Token);

                    arguments.Add(result);
                }

                var findCallerContext = new ScopeContext
                {
                    ParentScope = scopeContext
                };

                findCallerContext.SubContext.GettingCaller = true;
                var foundMethod = ExecuteExpression(node.SubNodes[0].SubNodes[0], findCallerContext);
                var Object = findCallerContext.SubContext.Caller;

                if (Object != null) {
                    for (int i = 0; i < Object.Properties.Count; i++) {
                        var p = Object.Properties[i];
                        methodContext.AddVariable(p.Name, p.Value, p.IsConstant);
                    }
                }

                bool isConstructor = false;

                if (!foundMethod.GetType().IsSubclassOf(typeof(SkryptMethod))) {
                    var type = foundMethod.Name;
                    var find = foundMethod.Properties.Find((x) => x.Name == "Constructor");

                    if (find != null) {
                        var typeName = foundMethod.Properties.Find((x) => x.Name == "TypeName").Value.ToString();

                        foundMethod = find.Value;
                        Object = new SkryptObject();
                        Object.SetPropertiesTo(GetType(typeName, scopeContext));

                        isConstructor = true;
                    } else {
                        _engine.ThrowError("Object does not have a constructor and can thus not be instanced!");
                    }
                }

                SkryptObject MethodResult = null;

                if (foundMethod.GetType() == typeof(UserMethod)) {
                    UserMethod method = (UserMethod)foundMethod;

                    for (var i = 0; i < method.Parameters.Count; i++)
                    {
                        var parName = method.Parameters[i];
                        SkryptObject input;

                        input = i < arguments.Count ? arguments[i] : new Library.Native.System.Null();

                        methodContext.Variables[parName] = new Variable
                        {
                            Name = parName,
                            Value = input,
                            Scope = methodContext
                        };
                    }

                    MethodResult = method.Execute(_engine, Object, arguments.ToArray(), methodContext);
                } else if (foundMethod.GetType() == typeof(SharpMethod)) {
                    MethodResult = ((SharpMethod)foundMethod).Execute(_engine, Object, arguments.ToArray(), methodContext);
                } else {
                    _engine.ThrowError("Cannot call value, as it is not a function!", node.SubNodes[0].SubNodes[0].Token);
                }

                if (isConstructor) {
                    return Object;
                } else {
                    return MethodResult;
                }
            }

            return null;
        }

        public SkryptObject ExecuteIndexSet(SkryptObject value, Node node, ScopeContext scopeContext)
        {
            var arguments = new List<SkryptObject>();

            foreach (var subNode in node.SubNodes[1].SubNodes)
            {
                var result = ExecuteExpression(subNode, scopeContext);

                if (result.Name == "void") _engine.ThrowError("Can't pass void into arguments!", node.SubNodes[0].Token);

                arguments.Add(result);
            }

            var Object = ExecuteExpression(node.SubNodes[0].SubNodes[0], scopeContext);

            dynamic left = Convert.ChangeType(Object, Object.GetType());

            Operation opLeft = left.GetOperation("indexset", Object.GetType(), arguments[0].GetType(), left.Operations);

            OperationDelegate operation = null;

            if (opLeft != null)
                operation = opLeft.OperationDelegate;
            else
                _engine.ThrowError("No such operation as index set " + left.Name + "!", node.SubNodes[0].Token);

            var inputArray = new List<SkryptObject>(arguments);

            inputArray.Insert(0, value);
            inputArray.Insert(0, Object);

            return operation(inputArray.ToArray());
        }

        public SkryptObject ExecuteIndex(Node node, ScopeContext scopeContext)
        {
            var arguments = new List<SkryptObject>();

            foreach (var subNode in node.SubNodes[1].SubNodes)
            {
                var result = ExecuteExpression(subNode, scopeContext);

                if (result.Name == "void") _engine.ThrowError("Can't pass void into arguments!", node.SubNodes[0].Token);

                arguments.Add(result);
            }

            var Object = ExecuteExpression(node.SubNodes[0].SubNodes[0], scopeContext);

            dynamic left = Convert.ChangeType(Object, Object.GetType());

            Operation opLeft = left.GetOperation("index", Object.GetType(), arguments[0].GetType(), left.Operations);

            OperationDelegate operation = null;

            if (opLeft != null)
                operation = opLeft.OperationDelegate;
            else
                _engine.ThrowError("No such operation as index " + left.Name + "!", node.SubNodes[0].Token);

            var inputArray = new List<SkryptObject>(arguments);

            inputArray.Insert(0, Object);

            //SkryptProperty property = new SkryptProperty {
            //    Value = Operation(inputArray.ToArray())
            //};

            return operation(inputArray.ToArray());
        }

        //public SkryptObject Invoke (string Name, params object[] arguments) {
        //    string signature = Name;
        //    string searchString = Name;
        //    ScopeContext methodContext = new ScopeContext {
        //        ParentScope = engine.GlobalScope
        //    };

        //    SkryptObject[] parameters = new SkryptObject[arguments.Length];

        //    for (int i = 0; i < arguments.Length; i++) {
        //        object arg = arguments[i];

        //        if (arg.GetType() == typeof(int) || arg.GetType() == typeof(float) || arg.GetType() == typeof(double)) {
        //            parameters[i] = new Numeric(Convert.ToDouble(arg));
        //        } else if (arg.GetType() == typeof(string)) {
        //            parameters[i] = new SkryptString { value = (string)arg };
        //        } else if (arg.GetType() == typeof(bool)) {
        //            parameters[i] = new SkryptBoolean { value = (bool)arg };
        //        }

        //        i++;
        //    }

        //    foreach (SkryptObject parameter in parameters) {
        //        if (parameter.Name == "void") {
        //            throw new SkryptException("Can't pass void into arguments!");
        //        }

        //        signature += "_" + parameter.Name;
        //    }

        //    foreach (Node method in engine.MethodNodes) {
        //        if (method.Body == signature) {
        //            searchString = signature;

        //            for (int i = 0; i < method.SubNodes[0].SubNodes.Count; i++) {
        //                Node par = method.SubNodes[0].SubNodes[i];
        //                methodContext.Variables[par.Body].Value = parameters[i];
        //            }
        //        }
        //    }

        //    if (engine.Methods.Exists((m) => m.Name == searchString)) {
        //        SkryptObject MethodResult = engine.Methods.Find((m) => m.Name == searchString).Execute(engine, parameters, methodContext);

        //        return MethodResult;
        //    }
        //    else {
        //        throw new SkryptException("Method '" + Name + "(" + String.Join(",", signature.Split('_').Skip(1).ToArray()) + ")' does not exist!");
        //    }
        //}
    }
}