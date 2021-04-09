﻿using System;
using Calamari.Commands.Support;
using Calamari.Common.Commands;
using Calamari.Common.Features.Substitutions;
using Calamari.Common.Plumbing.Variables;

namespace Calamari.Commands
{
    [Command("substitute-in-files")]
    public class SubstituteInFilesCommand : Command
    {
        readonly ISubstituteInFiles substituteInFiles;
        readonly string targetPath;

        public SubstituteInFilesCommand(IVariables variables, ISubstituteInFiles substituteInFiles)
        {
            targetPath = variables.Get(PackageVariables.Output.InstallationDirectoryPath, String.Empty);
            this.substituteInFiles = substituteInFiles;
        }

        public override int Execute(string[] commandLineArguments)
        {
            substituteInFiles.SubstituteBasedSettingsInSuppliedVariables(targetPath);
            return 0;
        }
    }
}