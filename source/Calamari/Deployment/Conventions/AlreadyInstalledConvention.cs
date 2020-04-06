﻿using System;
using System.Linq;
using Calamari.Common.Variables;
using Calamari.Deployment.Journal;

namespace Calamari.Deployment.Conventions
{
    public class AlreadyInstalledConvention : IInstallConvention
    {
        readonly ILog log;
        readonly IDeploymentJournal journal;

        public AlreadyInstalledConvention(ILog log, IDeploymentJournal journal)
        {
            this.log = log;
            this.journal = journal;
        }

        public void Install(RunningDeployment deployment)
        {
            if (!deployment.Variables.GetFlag(SpecialVariables.Package.SkipIfAlreadyInstalled))
            {
                return;
            }

            var id = deployment.Variables.Get(PackageVariables.PackageId);
            var version = deployment.Variables.Get(PackageVariables.PackageVersion);
            var policySet = deployment.Variables.Get(SpecialVariables.RetentionPolicySet);

            var previous = journal.GetLatestInstallation(policySet, id, version);
            if (previous == null) 
                return;

            if (!previous.WasSuccessful)
            {
                log.Info("The previous attempt to deploy this package was not successful; re-deploying.");
            }
            else
            {
                log.Info("The package has already been installed on this machine, so installation will be skipped.");
                log.SetOutputVariableButDoNotAddToVariables(PackageVariables.Output.InstallationDirectoryPath, previous.ExtractedTo);
                log.SetOutputVariableButDoNotAddToVariables(PackageVariables.Output.DeprecatedInstallationDirectoryPath, previous.ExtractedTo);
                deployment.Variables.Set(SpecialVariables.Action.SkipRemainingConventions, "true");
                deployment.Variables.Set(SpecialVariables.Action.SkipJournal, "true");
            }
        }
    }
}
