﻿using System;

namespace Calamari.Common.Plumbing.Deployment.PackageRetention
{
    public interface IManagePackageCache
    {
        void RegisterPackageUse(PackageIdentity package, ServerTaskId deploymentTaskId, long packageSizeBytes);
        void RemoveAllLocks(ServerTaskId serverTaskId);
        void ApplyRetention(string packageDirectory, int cacheSizeInMegaBytes);
        void ExpireStaleLocks(TimeSpan timeBeforeExpiration);
    }
}