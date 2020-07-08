// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.IO.IsolatedStorage
{
    // Used to try and start/finish with a clean state
    public class StoreTestsFixture : IDisposable
    {
        public StoreTestsFixture()
        {
            TestHelper.WipeStores();
        }

        public void Dispose()
        {
            TestHelper.WipeStores();
        }
    }
}
