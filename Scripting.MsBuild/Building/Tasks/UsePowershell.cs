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

namespace ClrPlus.Scripting.MsBuild.Building.Tasks {
    using Microsoft.Build.Framework;

    public class UsePowershell : ITask {
        /*
        private void EvaluateAllUsingTasks() {
            Expander expander = new Expander(this.evaluatedProperties, this.evaluatedItemsByName);
            foreach (UsingTask task in this.usingTasks) {
                this.taskRegistry.RegisterTask(task, expander, this.ParentEngine.LoggingServices, this.projectBuildEventContext);
            }
        }
        */

        public bool Execute() {
            throw new System.NotImplementedException();
        }

        public IBuildEngine BuildEngine {
            get {
                throw new System.NotImplementedException();
            }
            set {
                throw new System.NotImplementedException();
            }
        }

        public ITaskHost HostObject {
            get {
                throw new System.NotImplementedException();
            }
            set {
                throw new System.NotImplementedException();
            }
        }
    }
}