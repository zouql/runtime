// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;

namespace System.Configuration.Internal
{
    public interface IInternalConfigSystem
    {
        bool SupportsUserConfig { get; }

        // Returns the config object for the specified key.
        object GetSection(string configKey);

        // Refreshes the configuration section.
        void RefreshConfig(string sectionName);
    }
}
