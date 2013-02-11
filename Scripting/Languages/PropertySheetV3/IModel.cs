﻿//-----------------------------------------------------------------------
// <copyright company="CoApp Project">
//     Copyright (c) 2010-2013 Garrett Serack and CoApp Contributors. 
//     Contributors can be discovered using the 'git log' command.
//     All rights reserved.
// </copyright>
// <license>
//     The software is licensed under the Apache 2.0 License (the "License")
//     You may not use the software except in compliance with the License. 
// </license>
//-----------------------------------------------------------------------

namespace ClrPlus.Scripting.Languages.PropertySheetV3 {
    using System.Collections.Generic;
    using Core.Collections;

    public interface IItem {
        IDictionary<string, RValue> Metadata {get;}
    }

    public interface IProperty : IItem {
        void SetCollection(RValue rvalue);
        void AddToCollection(RValue rvalue);
        void SetValue(RValue rvalue);

        RVSingle Value {get;}
        RVCollection Values { get; } 
    }

    public interface INode : IDictionary<Selector, IItem>, IItem {
        IModel Root { get; }
        INode Parent { get; }
        Selector Selector {get;}
        
        void AddAlias(string aliasName, Selector aliasReference);
        Alias GetAlias(string aliasName);

        IProperty NewProperty();
        INode NewNode(Selector key);
    }

    public interface IModel : INode {
        IOrderedDictionary<string, IModel> Imports {get;}
        dynamic MapTo(object backingObject, object routes);
    }

    public delegate View Route(View context, Selector selector);
}