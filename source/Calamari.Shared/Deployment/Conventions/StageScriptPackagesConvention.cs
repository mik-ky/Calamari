﻿using System.IO;
using System.Linq;
using Calamari.Commands.Support;
using Calamari.Common.Variables;
using Calamari.Integration.FileSystem;
using Calamari.Integration.Packages;

namespace Calamari.Deployment.Conventions
{
    public class StageScriptPackagesConvention : IInstallConvention
    {
        private readonly string packagePathContainingScript;
        private readonly ICalamariFileSystem fileSystem;
        private readonly IPackageExtractor extractor;
        private readonly bool forceExtract;

        public StageScriptPackagesConvention(string packagePathContainingScript, ICalamariFileSystem fileSystem, IPackageExtractor extractor, bool forceExtract = false)
        {
            this.packagePathContainingScript = packagePathContainingScript;
            this.fileSystem = fileSystem;
            this.extractor = extractor;
            this.forceExtract = forceExtract;
        }
        
        public void Install(RunningDeployment deployment)
        {
            // If the script is contained in a package, then extract the containing package in the working directory 
            if (!string.IsNullOrWhiteSpace(packagePathContainingScript))
            {
                ExtractPackage(packagePathContainingScript, deployment.CurrentDirectory);
                deployment.Variables.Set(Common.Variables.SpecialVariables.OriginalPackageDirectoryPath, deployment.CurrentDirectory);
            }
            
            // Stage any referenced packages (i.e. packages that don't contain the script) 
            // The may or may not be extracted.
            StagePackageReferences(deployment);
        }

        void StagePackageReferences(RunningDeployment deployment)
        {
            var variables = deployment.Variables;
            
            // No need to check for "default" package since it gets extracted in the current directory in previous step.
            var packageReferenceNames = variables.GetIndexes(PackageVariables.PackageCollection)
                .Where(i => !string.IsNullOrEmpty(i));

            foreach (var packageReferenceName in packageReferenceNames)
            {
                Log.Verbose($"Considering '{packageReferenceName}' for extraction");
                var sanitizedPackageReferenceName = fileSystem.RemoveInvalidFileNameChars(packageReferenceName);
                
                var packageOriginalPath = variables.Get(PackageVariables.OriginalPathForPackageReferenceName(packageReferenceName));
                
                if (string.IsNullOrWhiteSpace(packageOriginalPath))
                {
                    Log.Info($"Package '{packageReferenceName}' was not acquired");
                    continue;
                }
                
                packageOriginalPath = Path.GetFullPath(variables.Get(PackageVariables.OriginalPathForPackageReferenceName(packageReferenceName)));

                // In the case of container images, the original path is not a file-path.  We won't try and extract or move it.
                if (!fileSystem.FileExists(packageOriginalPath))
                {
                    Log.Verbose($"Package '{packageReferenceName}' was not found at '{packageOriginalPath}', skipping extraction");
                    continue;
                }

                var shouldExtract = variables.GetFlag(PackageVariables.ExtractForPackageReferenceName(packageReferenceName));

                if (forceExtract || shouldExtract)
                {
                    var extractionPath = Path.Combine(deployment.CurrentDirectory, sanitizedPackageReferenceName);
                    ExtractPackage(packageOriginalPath, extractionPath);
                    Log.SetOutputVariable(SpecialVariables.Packages.ExtractedPath(packageReferenceName), extractionPath, variables);
                }
                else
                {
                    var localPackageFileName = sanitizedPackageReferenceName + Path.GetExtension(packageOriginalPath);
                    var destinationPackagePath = Path.Combine(deployment.CurrentDirectory, localPackageFileName);
                    Log.Info($"Copying package: '{packageOriginalPath}' -> '{destinationPackagePath}'");
                    fileSystem.CopyFile(packageOriginalPath, destinationPackagePath);
                    Log.SetOutputVariable(SpecialVariables.Packages.PackageFilePath(packageReferenceName), destinationPackagePath, variables);
                    Log.SetOutputVariable(SpecialVariables.Packages.PackageFileName(packageReferenceName), localPackageFileName, variables);
                }
            }
        }

        void ExtractPackage(string packageFile, string extractionDirectory)
        {
           Log.Info($"Extracting package '{packageFile}' to '{extractionDirectory}'"); 
            
            if (!File.Exists(packageFile))
                throw new CommandException("Could not find package file: " + packageFile);
            
            extractor.Extract(packageFile, extractionDirectory);
        }
    }
}