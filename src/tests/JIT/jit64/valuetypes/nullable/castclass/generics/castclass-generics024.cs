// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// <Area> Nullable - CastClass </Area>
// <Title> Nullable type with castclass expr  </Title>
// <Description>  
// checking type of NotEmptyStructA using cast expr
// </Description> 
// <RelatedBugs> </RelatedBugs>  
//<Expects Status=success></Expects>
// <Code> 


using System.Runtime.InteropServices;
using System;

internal class NullableTest
{
    private static bool BoxUnboxToNQ<T>(T o)
    {
        return Helper.Compare((NotEmptyStructA)(ValueType)(object)o, Helper.Create(default(NotEmptyStructA)));
    }

    private static bool BoxUnboxToQ<T>(T o)
    {
        return Helper.Compare((NotEmptyStructA?)(ValueType)(object)o, Helper.Create(default(NotEmptyStructA)));
    }

    private static int Main()
    {
        NotEmptyStructA? s = Helper.Create(default(NotEmptyStructA));

        if (BoxUnboxToNQ(s) && BoxUnboxToQ(s))
            return ExitCode.Passed;
        else
            return ExitCode.Failed;
    }
}


