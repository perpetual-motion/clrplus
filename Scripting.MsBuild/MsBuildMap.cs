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

using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ClrPlus.Scripting.MsBuild {
    using System.Collections;
    using System.Xml;
    using Core.Collections;
    using Core.Extensions;
    using Core.Utility;
    using Languages.PropertySheetV3;
    using Languages.PropertySheetV3.Mapping;
    using Microsoft.Build.Construction;
    using Microsoft.Build.Evaluation;
    using Platform;

    public static class MsBuildMap {
        private static XDictionary<Project, ProjectPlus> _projects = new XDictionary<Project, ProjectPlus>();

        public static ProjectPlus Lookup(this Project project) {
           if (!_projects.ContainsKey(project)) {
               _projects.Add(project, new ProjectPlus(project));
           }
           return _projects[project];
        }
        
        public static bool HasProject(this Project project) {
            return _projects.ContainsKey(project);
        }

        public static ProjectTargetElement AddInitTarget(this Project project, string name) {
            project.Lookup().InitialTargets.Add(name);
            return LookupTarget(project, name);
        }

        public static DictionaryRoute<Tp, Tk, Tv> XYZ<Tp, Tk, Tv>( DictionaryRoute<Tp, Tk, Tv> aRoute ) {
            DictionaryRoute<Tp, Tk, Tv> route;
            route = 
                parent => {
                    var result = aRoute(parent);
                    route = (p) => result;
                    return result;
                };
            return route;
        }

        public class ProjectPlus {
            private Project _project;
            internal View View;
            internal string Name;
            internal StringPropertyList InitialTargets;

            internal ProjectPlus(Project project) {
                InitialTargets = new StringPropertyList(() => project.Xml.InitialTargets, v => project.Xml.InitialTargets = v, target => LookupTarget(project, target, null));
            }
        }

        public static void MapProject(this PropertySheet propertySheet, string location, Project project ) {

            propertySheet.Route(
                location.MapTo(() => project,
                  "InitialTargets".MapTo( (IList)project.Lookup().InitialTargets),
                  "ItemDefinitionGroup".MapTo(() => LookupItemDefinitionGroup(project, ""), ItemDefinitionGroupChildren().ToArray()),
                  "Target".MapTo(new DelegateDictionary<string,ProjectTargetElement>(
                      () => project.Targets.Keys,
                      key => LookupTarget( project, key, "" ),
                      (name, value) => LookupTarget( project, name, "" ),
                      key => project.Targets.Remove(key )
                      ),
                      "CHILDREN".MapChildTo<ProjectTargetElement>( (target,child)  => GetTargetItem(target,child ) )),
                   
                /*
                "PropertyGroup".MapTo(),
                "Import".MapTo(),
                "ImportGroup".MapTo(),
                "ItemGroup".MapTo(),
                */
                    "condition".MapTo(() => Condition.Create(project), key => Configurations.NormalizeConditionKey(project,key) ,

                        "Target".MapTo<string, string, ProjectTargetElement>(condition => new DelegateDictionary<string, ProjectTargetElement>(
                              () => project.Targets.Keys,
                              key => LookupTarget(project, key, condition),
                              (name, value) => LookupTarget(project, name, ""),
                              key => project.Targets.Remove(key)
                              ), "CHILDREN".MapChildTo<ProjectTargetElement>( (target,child)  => GetTargetItem(target,child ) )),



                        "ItemDefinitionGroup".MapTo<string>(condition => LookupItemDefinitionGroup(project, condition), ItemDefinitionGroupChildren().ToArray())

                        ))

                );
        }

        public static XmlElement XmlElement(this ProjectElement projectElement) {
            return projectElement.AccessPrivate().XmlElement;
        }

        public static void MapConfigurations(this PropertySheet propertySheet, string location, Project project) {
            Configurations.Add((propertySheet.View as View).GetProperty(location), project);
        }

        private static ProjectElement GetTargetItem(ProjectTargetElement target, View view) {
            // get the member name and data from the view, and create/lookup the item.
            // return the item.
            switch (view.MemberName ) {
                case "PropertyGroup":
                    break;
                case "ItemGroup":
                    break;
                default:
                    var tsk = target.AddTask(view.MemberName);
                    
                    foreach (var n in view.PropertyNames) {
                        tsk.SetParameter( n, view.GetProperty(n) );
                    }
                    return tsk;
            } 
            return null;
        }

        private static IList GetTaskList(ProjectTargetElement target) {
            // get the member name and data from the view, and create/lookup the item.
            // return the item.
            return null;
        }

        private static IEnumerable<ToRoute> ItemDefinitionGroupChildren() {
            yield return ItemDefinitionRoute("PostBuildEvent");
            //Command
            //Message

            yield return ItemDefinitionRoute("Midl");
            //TypeLibraryName

            yield return ItemDefinitionRoute("ResourceCompile");
            //Culture
            //ResourcOutputFileName
            //AdditionalIncludeDirectories
            //PreprocessorDefinitions

            yield return ItemDefinitionRoute("BcsMake");
            //SuppressStartupBanner
            //OutputFile

            yield return ItemDefinitionRoute("ClCompile",
                MetadataListRoute("PreprocessorDefinitions", "%(PreprocessorDefinitions)"),
                MetadataListRoute("AdditionalIncludeDirectories", "%(AdditionalIncludeDirectories)")


                );

            yield return ItemDefinitionRoute("Link",
                MetadataListRoute("AdditionalDependencies", "%(AdditionalDependencies)")


                );
        }

        private static ToRoute ItemDefinitionRoute(string name, params ToRoute[] children) {
            return name.MapTo<ProjectItemDefinitionGroupElement>(idg => {
                foreach(var i in idg.Children) {
                    var pide = (i as ProjectItemDefinitionElement);
                    if(pide != null) {
                        if(pide.ItemType == name) {
                            return pide;
                        }
                    }
                }

                var c = idg.AddItemDefinition(name);
                return c;
            }, children);
        }

        private static ToRoute MetadataRoute(string metadataName, string defaultValue = null) {
            return metadataName.MapTo<ProjectItemDefinitionElement>(pide => {
                foreach(var m in pide.Metadata) {
                    var metadata = m;
                    if(metadata.Name == metadataName) {
                        return new Accessor(() => metadata.Value, (v) => metadata.Value = v.ToString());
                    }
                }
                var n = pide.AddMetadata(metadataName, defaultValue ?? "");
                return new Accessor(() => n.Value, (v) => n.Value = v.ToString());
            });
        }

        private static ToRoute MetadataListRoute(string metadataName, string defaultValue = null) {
            return metadataName.MapTo<ProjectItemDefinitionElement>(pide => {
                foreach(var m in pide.Metadata) {
                    var metadata = m;
                    if(metadata.Name == metadataName) {
                        return (IList)new StringPropertyList(() => metadata.Value, v => metadata.Value = v);
                    }
                }
                var n = pide.AddMetadata(metadataName, defaultValue ?? "");
                return (IList)new StringPropertyList(() => n.Value, v => n.Value = v);
            });
        }

        private static ProjectItemDefinitionGroupElement LookupItemDefinitionGroup(Project p, string condition) {
            // look it up or create it.
            if(string.IsNullOrEmpty(condition)) {
                var result = p.Xml.ItemDefinitionGroups.FirstOrDefault(each => string.IsNullOrEmpty(each.Label));
                if(result != null) {
                    return result;
                }
            }
            else {
                var result = p.Xml.ItemDefinitionGroups.FirstOrDefault(each => condition == each.Label);
                if(result != null) {
                    return result;
                }
            }

            var idg = p.Xml.AddItemDefinitionGroup();

            if(!string.IsNullOrEmpty(condition)) {
                idg.Label = condition;
                idg.Condition = Configurations.GenerateCondition(p,condition);
            }

            return idg;
        }


        internal static ProjectTargetElement LookupTarget(this Project p, string name,  string condition = null) {
            if (string.IsNullOrEmpty(condition)) {
                var result = p.Xml.Targets.FirstOrDefault(each => name == each.Name && string.IsNullOrEmpty(each.Condition));
                if (result != null) {
                    return result;
                }
                return p.Xml.AddTarget(name);
            }

            var modifiedname = "{0}_{1}".format(name, condition).MakeSafeFileName();

            var conditionedResult = p.Xml.Targets.FirstOrDefault(each => modifiedname  == each.Name );
            if(conditionedResult != null) {
                return conditionedResult;
            }

            var target = p.Xml.AddTarget(modifiedname );
            
            target.Label = condition;
            target.Condition = Configurations.GenerateCondition(p, condition);
            target.AfterTargets = name;
            
            // ensure a non-conditioned gets created that we can chain to.
            LookupTarget(p, name, null);
            
            return target;
        }

        private static IEnumerable<ToRoute> TargetChildren() {
            yield break;
        }
    }
}
