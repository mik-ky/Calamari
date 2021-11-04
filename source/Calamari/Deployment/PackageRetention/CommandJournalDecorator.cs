﻿using Calamari.Commands.Support;
using Calamari.Common.Plumbing.Deployment.PackageRetention;
using Calamari.Common.Plumbing.Extensions;
using Calamari.Common.Plumbing.Logging;
using Calamari.Common.Plumbing.Variables;

namespace Calamari.Deployment.PackageRetention
{
    public class CommandJournalDecorator: ICommandWithArgs
    {
        readonly ILog log;
        readonly ICommandWithArgs command;
        readonly IJournal journal;
        readonly bool retentionEnabled = false;
        
        PackageIdentity Package { get; }
        ServerTaskID DeploymentTaskID {get;}

        public CommandJournalDecorator(ILog log, ICommandWithArgs command, IVariables variables, IJournal journal)
        {
            this.log = log;
            this.command = command;
            this.journal = journal;

            DeploymentTaskID = new ServerTaskID(variables);
            Package = new PackageIdentity(variables);

            retentionEnabled = variables.IsPackageRetentionEnabled();

#if DEBUG
            log.Verbose($"Decorating {command.GetType().Name} with command journal.");
#endif
        }

        public int Execute(string[] commandLineArguments)
        {
            if (retentionEnabled) journal.RegisterPackageUse(Package, DeploymentTaskID);
            return command.Execute(commandLineArguments);
        }
    }
}