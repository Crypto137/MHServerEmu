﻿using MHServerEmu.Core.Config;

namespace MHServerEmu.Auth
{
    public class AuthConfig : ConfigContainer
    {
        public string Address { get; private set; } = "localhost";
        public string Port { get; private set; } = "8080";
        public bool EnableWebApi { get; private set; } = true;
        public bool EnableDashboard { get; private set; } = true;
        public string DashboardFileDirectory { get; private set; } = "Dashboard";
        public string DashboardUrlDirectory { get; private set; } = "/";
    }
}
