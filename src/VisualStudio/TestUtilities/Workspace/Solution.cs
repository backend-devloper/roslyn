﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.IO;

using DteSolution = EnvDTE80.Solution2;

namespace Roslyn.VisualStudio.Test.Utilities
{
    /// <summary>Provides a means of interacting with the current solution loaded by the host process.</summary>
    public class Solution
    {
        private static readonly IDictionary<ProjectTemplate, string> ProjectTemplateName = new Dictionary<ProjectTemplate, string> {
            [ProjectTemplate.ClassLibrary] = "ClassLibrary.zip",
            [ProjectTemplate.ConsoleApplication] = "ConsoleApplication.zip",
            [ProjectTemplate.Website] = "EmptyWeb.zip",
            [ProjectTemplate.WinFormsApplication] = "WindowsApplication.zip",
            [ProjectTemplate.WpfApplication] = "WpfApplication.zip",
            [ProjectTemplate.WebApplication] = "WebApplicationProject40"
        };

        private static readonly IDictionary<ProjectLanguage, string> ProjectLanguageName = new Dictionary<ProjectLanguage, string> {
            [ProjectLanguage.CSharp] = "CSharp",
            [ProjectLanguage.VisualBasic] = "VisualBasic"
        };

        private readonly DteSolution _dteSolution;
        private readonly string _fileName;              // Cache the filename as `_dteSolution` won't expose it unless the solution has been saved

        internal Solution(DteSolution dteSolution, string fileName)
        {
            _dteSolution = dteSolution;
            _fileName = fileName;
        }

        public DteSolution DteSolution
            => _dteSolution;

        public string FileName
        {
            get
            {
                var solutionFullName = _dteSolution.FullName;
                return string.IsNullOrEmpty(solutionFullName) ? _fileName : solutionFullName;
            }
        }

        public Project AddProject(string projectName, ProjectTemplate projectTemplate, ProjectLanguage projectLanguage)
        {
            var solutionFolder = Path.GetDirectoryName(FileName);
            var projectPath = Path.Combine(solutionFolder, projectName);
            var projectTemplatePath = GetProjectTemplatePath(projectTemplate, projectLanguage);

            var dteProject = _dteSolution.AddFromTemplate(projectTemplatePath, projectPath, projectName, Exclusive: false);
            return new Project(dteProject, this, projectLanguage);
        }

        // TODO: Adjust language name based on whether we are using a web template
        private string GetProjectTemplatePath(ProjectTemplate projectTemplate, ProjectLanguage projectLanguage)
            => _dteSolution.GetProjectTemplate(ProjectTemplateName[projectTemplate], ProjectLanguageName[projectLanguage]);
    }
}
