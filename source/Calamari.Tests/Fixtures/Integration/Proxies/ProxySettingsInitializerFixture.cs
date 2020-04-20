﻿using System;
using System.Net;
using Calamari.Integration.Proxies;
using Calamari.Tests.Helpers;
using FluentAssertions;
using NUnit.Framework;

namespace Calamari.Tests.Fixtures.Integration.Proxies
{
    [TestFixture]
    public class ProxySettingsInitializerFixture
    {
        const string ProxyUserName = "someuser";
        const string ProxyPassword = "some@://password";
        string proxyHost = "proxy-initializer-fixture-good-proxy";
        int proxyPort = 1234;

        [TearDown]
        public void TearDown()
        {
            ResetProxyEnvironmentVariables();
        }

        [Test]
        public void Initialize_BypassProxy()
        {
            SetEnvironmentVariables(false, "", 80, "", "");

            AssertBypassProxy(ProxySettingsInitializer.GetProxySettingsFromEnvironment());
        }

        [Test]
        public void Initialize_UseSystemProxy()
        {
            SetEnvironmentVariables(true, "", 80, "", "");

            AssertSystemProxySettings(ProxySettingsInitializer.GetProxySettingsFromEnvironment(), false);
        }

        [Test]
        public void Initialize_UseSystemProxyWithCredentials()
        {
            SetEnvironmentVariables(true, "", 80, ProxyUserName, ProxyPassword);

            AssertSystemProxySettings(ProxySettingsInitializer.GetProxySettingsFromEnvironment(), true);
        }

        [Test]
        public void Initialize_CustomProxy()
        {
            SetEnvironmentVariables(false, proxyHost, proxyPort, "", "");

            AssertCustomProxy(ProxySettingsInitializer.GetProxySettingsFromEnvironment(), false);
        }

        [Test]
        public void Initialize_CustomProxyWithCredentials()
        {
            SetEnvironmentVariables(false, proxyHost, proxyPort, ProxyUserName, ProxyPassword);

            AssertCustomProxy(ProxySettingsInitializer.GetProxySettingsFromEnvironment(), true);
        }

        void SetEnvironmentVariables(
            bool useDefaultProxy,
            string proxyhost,
            int proxyPort,
            string proxyUsername,
            string proxyPassword)
        {
            Environment.SetEnvironmentVariable(DeploymentEnvironmentVariables.TentacleUseDefaultProxy,
                useDefaultProxy.ToString());
            Environment.SetEnvironmentVariable(DeploymentEnvironmentVariables.TentacleProxyHost, proxyhost);
            Environment.SetEnvironmentVariable(DeploymentEnvironmentVariables.TentacleProxyPort, proxyPort.ToString());
            Environment.SetEnvironmentVariable(DeploymentEnvironmentVariables.TentacleProxyUsername, proxyUsername);
            Environment.SetEnvironmentVariable(DeploymentEnvironmentVariables.TentacleProxyPassword, proxyPassword);

        }

        void ResetProxyEnvironmentVariables()
        {
            Environment.SetEnvironmentVariable(DeploymentEnvironmentVariables.TentacleUseDefaultProxy, string.Empty);
            Environment.SetEnvironmentVariable(DeploymentEnvironmentVariables.TentacleProxyHost, string.Empty);
            Environment.SetEnvironmentVariable(DeploymentEnvironmentVariables.TentacleProxyPort, string.Empty);
            Environment.SetEnvironmentVariable(DeploymentEnvironmentVariables.TentacleProxyUsername, string.Empty);
            Environment.SetEnvironmentVariable(DeploymentEnvironmentVariables.TentacleProxyPassword, string.Empty);
        }

        void AssertCustomProxy(IProxySettings proxySettings, bool hasCredentials)
        {
            var proxy = proxySettings.Should().BeOfType<UseCustomProxySettings>()
                .Subject;

            proxy.Host.Should().Be(proxyHost);
            proxy.Port.Should().Be(proxyPort);

            if (hasCredentials)
            {
                proxy.Username.Should().Be(ProxyUserName);
                proxy.Password.Should().Be(ProxyPassword);
            }
            else
            {
                proxy.Username.Should().BeNull();
                proxy.Password.Should().BeNull();
            }
        }

        static void AssertSystemProxySettings(IProxySettings proxySettings, bool hasCredentials)
        {
            var proxy = proxySettings.Should().BeOfType<UseSystemProxySettings>()
                .Subject;
            
            if (hasCredentials)
            {
                proxy.Username.Should().Be(ProxyUserName);
                proxy.Password.Should().Be(ProxyPassword);
            }
            else
            {
                proxy.Username.Should().BeNull();
                proxy.Password.Should().BeNull();
            }
        }

        void AssertBypassProxy(IProxySettings proxySettings)
        {
            proxySettings.Should().BeOfType<BypassProxySettings>();
        }
    }
}
