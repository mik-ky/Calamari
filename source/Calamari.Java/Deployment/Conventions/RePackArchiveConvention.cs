﻿using System.IO;
using Calamari.Deployment;
using Calamari.Deployment.Conventions;
using Calamari.Integration.FileSystem;
using Calamari.Integration.Packages;
using Calamari.Integration.Processes;
using Calamari.Java.Integration.Packages;

namespace Calamari.Java.Deployment.Conventions
{
    public class RePackArchiveConvention : IInstallConvention
    {
        readonly ICalamariFileSystem fileSystem;
        readonly IPackageExtractor packageExtractor;
        readonly JarTool jarTool;

        public RePackArchiveConvention(ICalamariFileSystem fileSystem, IPackageExtractor packageExtractor, ICommandLineRunner commandLineRunner)
        {
            this.fileSystem = fileSystem;
            this.packageExtractor = packageExtractor;
            this.jarTool = new JarTool(commandLineRunner, fileSystem); 
        }

        public void Install(RunningDeployment deployment)
        {
            if (deployment.Variables.GetFlag(SpecialVariables.Action.Java.JavaArchiveExtractionDisabled, false))
                return;

            var packageMetadata = packageExtractor.GetMetadata(deployment.PackageFilePath);
            var applicationDirectory = ApplicationDirectory.GetApplicationDirectory(packageMetadata, deployment.Variables, fileSystem);
            var targetFilePath = Path.Combine(applicationDirectory,
                $"{packageMetadata.Id}.{packageMetadata.Version}{Path.GetExtension(deployment.PackageFilePath)}");

            Log.Info($"Re-packaging archive: '{targetFilePath}'");
            var stagingDirectory = deployment.CurrentDirectory;

            jarTool.CreateJar(stagingDirectory, targetFilePath);
        }
    }
}