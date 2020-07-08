// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;

namespace System.Diagnostics
{
    /// <summary>
    /// Class which represents a description of a stack trace
    /// There is no good reason for the methods of this class to be virtual.
    /// </summary>
    public partial class StackTrace
    {
        public const int METHODS_TO_SKIP = 0;

        private int _numOfFrames;
        private int _methodsToSkip;

        /// <summary>
        /// Stack frames comprising this stack trace.
        /// </summary>
        private StackFrame[]? _stackFrames;

        /// <summary>
        /// Constructs a stack trace from the current location.
        /// </summary>
        public StackTrace()
        {
            InitializeForCurrentThread(METHODS_TO_SKIP, false);
        }

        /// <summary>
        /// Constructs a stack trace from the current location.
        /// </summary>
        public StackTrace(bool fNeedFileInfo)
        {
            InitializeForCurrentThread(METHODS_TO_SKIP, fNeedFileInfo);
        }

        /// <summary>
        /// Constructs a stack trace from the current location, in a caller's
        /// frame
        /// </summary>
        public StackTrace(int skipFrames)
        {
            if (skipFrames < 0)
                throw new ArgumentOutOfRangeException(nameof(skipFrames),
                    SR.ArgumentOutOfRange_NeedNonNegNum);

            InitializeForCurrentThread(skipFrames + METHODS_TO_SKIP, false);
        }

        /// <summary>
        /// Constructs a stack trace from the current location, in a caller's
        /// frame
        /// </summary>
        public StackTrace(int skipFrames, bool fNeedFileInfo)
        {
            if (skipFrames < 0)
                throw new ArgumentOutOfRangeException(nameof(skipFrames),
                    SR.ArgumentOutOfRange_NeedNonNegNum);

            InitializeForCurrentThread(skipFrames + METHODS_TO_SKIP, fNeedFileInfo);
        }

        /// <summary>
        /// Constructs a stack trace from the current location.
        /// </summary>
        public StackTrace(Exception e)
        {
            if (e == null)
                throw new ArgumentNullException(nameof(e));

            InitializeForException(e, METHODS_TO_SKIP, false);
        }

        /// <summary>
        /// Constructs a stack trace from the current location.
        /// </summary>
        public StackTrace(Exception e, bool fNeedFileInfo)
        {
            if (e == null)
                throw new ArgumentNullException(nameof(e));

            InitializeForException(e, METHODS_TO_SKIP, fNeedFileInfo);
        }

        /// <summary>
        /// Constructs a stack trace from the current location, in a caller's
        /// frame
        /// </summary>
        public StackTrace(Exception e, int skipFrames)
        {
            if (e == null)
                throw new ArgumentNullException(nameof(e));

            if (skipFrames < 0)
                throw new ArgumentOutOfRangeException(nameof(skipFrames),
                    SR.ArgumentOutOfRange_NeedNonNegNum);

            InitializeForException(e, skipFrames + METHODS_TO_SKIP, false);
        }

        /// <summary>
        /// Constructs a stack trace from the current location, in a caller's
        /// frame
        /// </summary>
        public StackTrace(Exception e, int skipFrames, bool fNeedFileInfo)
        {
            if (e == null)
                throw new ArgumentNullException(nameof(e));

            if (skipFrames < 0)
                throw new ArgumentOutOfRangeException(nameof(skipFrames),
                    SR.ArgumentOutOfRange_NeedNonNegNum);

            InitializeForException(e, skipFrames + METHODS_TO_SKIP, fNeedFileInfo);
        }

        /// <summary>
        /// Constructs a "fake" stack trace, just containing a single frame.
        /// Does not have the overhead of a full stack trace.
        /// </summary>
        public StackTrace(StackFrame frame)
        {
            _stackFrames = new StackFrame[] { frame };
            _numOfFrames = 1;
        }

        /// <summary>
        /// Property to get the number of frames in the stack trace
        /// </summary>
        public virtual int FrameCount => _numOfFrames;

        /// <summary>
        /// Returns a given stack frame.  Stack frames are numbered starting at
        /// zero, which is the last stack frame pushed.
        /// </summary>
        public virtual StackFrame? GetFrame(int index)
        {
            if (_stackFrames != null && index < _numOfFrames && index >= 0)
                return _stackFrames[index + _methodsToSkip];

            return null;
        }

        /// <summary>
        /// Returns an array of all stack frames for this stacktrace.
        /// The array is ordered and sized such that GetFrames()[i] == GetFrame(i)
        /// The nth element of this array is the same as GetFrame(n).
        /// The length of the array is the same as FrameCount.
        /// </summary>
        public virtual StackFrame[] GetFrames()
        {
            if (_stackFrames == null || _numOfFrames <= 0)
                return Array.Empty<StackFrame>();

            // We have to return a subset of the array. Unfortunately this
            // means we have to allocate a new array and copy over.
            StackFrame[] array = new StackFrame[_numOfFrames];
            Array.Copy(_stackFrames, _methodsToSkip, array, 0, _numOfFrames);
            return array;
        }

        /// <summary>
        /// Builds a readable representation of the stack trace
        /// </summary>
        public override string ToString()
        {
            // Include a trailing newline for backwards compatibility
            return ToString(TraceFormat.TrailingNewLine);
        }

        /// <summary>
        /// TraceFormat is used to specify options for how the
        /// string-representation of a StackTrace should be generated.
        /// </summary>
        internal enum TraceFormat
        {
            Normal,
            TrailingNewLine,        // include a trailing new line character
        }

#if !CORERT
        /// <summary>
        /// Builds a readable representation of the stack trace, specifying
        /// the format for backwards compatibility.
        /// </summary>
        internal string ToString(TraceFormat traceFormat)
        {
            var sb = new StringBuilder(256);
            ToString(traceFormat, sb);
            return sb.ToString();
        }

        internal void ToString(TraceFormat traceFormat, StringBuilder sb)
        {
            // Passing a default string for "at" in case SR.UsingResourceKeys() is true
            // as this is a special case and we don't want to have "Word_At" on stack traces.
            string word_At = SR.GetResourceString(nameof(SR.Word_At), defaultString: "at");
            // We also want to pass in a default for inFileLineNumber.
            string inFileLineNum = SR.GetResourceString(nameof(SR.StackTrace_InFileLineNumber), defaultString: "in {0}:line {1}");
            bool fFirstFrame = true;
            for (int iFrameIndex = 0; iFrameIndex < _numOfFrames; iFrameIndex++)
            {
                StackFrame? sf = GetFrame(iFrameIndex);
                MethodBase? mb = sf?.GetMethod();
                if (mb != null && (ShowInStackTrace(mb) ||
                                   (iFrameIndex == _numOfFrames - 1))) // Don't filter last frame
                {
                    // We want a newline at the end of every line except for the last
                    if (fFirstFrame)
                        fFirstFrame = false;
                    else
                        sb.AppendLine();

                    sb.AppendFormat(CultureInfo.InvariantCulture, "   {0} ", word_At);

                    bool isAsync = false;
                    Type? declaringType = mb.DeclaringType;
                    string methodName = mb.Name;
                    bool methodChanged = false;
                    if (declaringType != null && declaringType.IsDefined(typeof(CompilerGeneratedAttribute), inherit: false))
                    {
                        isAsync = typeof(IAsyncStateMachine).IsAssignableFrom(declaringType);
                        if (isAsync || typeof(IEnumerator).IsAssignableFrom(declaringType))
                        {
                            methodChanged = TryResolveStateMachineMethod(ref mb, out declaringType);
                        }
                    }

                    // if there is a type (non global method) print it
                    // ResolveStateMachineMethod may have set declaringType to null
                    if (declaringType != null)
                    {
                        // Append t.FullName, replacing '+' with '.'
                        string fullName = declaringType.FullName!;
                        for (int i = 0; i < fullName.Length; i++)
                        {
                            char ch = fullName[i];
                            sb.Append(ch == '+' ? '.' : ch);
                        }
                        sb.Append('.');
                    }
                    sb.Append(mb.Name);

                    // deal with the generic portion of the method
                    if (mb is MethodInfo mi && mi.IsGenericMethod)
                    {
                        Type[] typars = mi.GetGenericArguments();
                        sb.Append('[');
                        int k = 0;
                        bool fFirstTyParam = true;
                        while (k < typars.Length)
                        {
                            if (!fFirstTyParam)
                                sb.Append(',');
                            else
                                fFirstTyParam = false;

                            sb.Append(typars[k].Name);
                            k++;
                        }
                        sb.Append(']');
                    }

                    ParameterInfo[]? pi = null;
                    try
                    {
                        pi = mb.GetParameters();
                    }
                    catch
                    {
                        // The parameter info cannot be loaded, so we don't
                        // append the parameter list.
                    }
                    if (pi != null)
                    {
                        // arguments printing
                        sb.Append('(');
                        bool fFirstParam = true;
                        for (int j = 0; j < pi.Length; j++)
                        {
                            if (!fFirstParam)
                                sb.Append(", ");
                            else
                                fFirstParam = false;

                            string typeName = "<UnknownType>";
                            if (pi[j].ParameterType != null)
                                typeName = pi[j].ParameterType.Name;
                            sb.Append(typeName);
                            sb.Append(' ');
                            sb.Append(pi[j].Name);
                        }
                        sb.Append(')');
                    }

                    if (methodChanged)
                    {
                        // Append original method name e.g. +MoveNext()
                        sb.Append('+');
                        sb.Append(methodName);
                        sb.Append('(').Append(')');
                    }

                    // source location printing
                    if (sf!.GetILOffset() != -1)
                    {
                        // If we don't have a PDB or PDB-reading is disabled for the module,
                        // then the file name will be null.
                        string? fileName = sf.GetFileName();

                        if (fileName != null)
                        {
                            // tack on " in c:\tmp\MyFile.cs:line 5"
                            sb.Append(' ');
                            sb.AppendFormat(CultureInfo.InvariantCulture, inFileLineNum, fileName, sf.GetFileLineNumber());
                        }
                    }

                    // Skip EDI boundary for async
                    if (sf.IsLastFrameFromForeignExceptionStackTrace && !isAsync)
                    {
                        sb.AppendLine();
                        // Passing default for Exception_EndStackTraceFromPreviousThrow in case SR.UsingResourceKeys is set.
                        sb.Append(SR.GetResourceString(nameof(SR.Exception_EndStackTraceFromPreviousThrow),
                            defaultString: "--- End of stack trace from previous location ---"));
                    }
                }
            }

            if (traceFormat == TraceFormat.TrailingNewLine)
                sb.AppendLine();
        }
#endif // !CORERT

        private static bool ShowInStackTrace(MethodBase mb)
        {
            Debug.Assert(mb != null);

            if ((mb.MethodImplementationFlags & MethodImplAttributes.AggressiveInlining) != 0)
            {
                // Aggressive Inlines won't normally show in the StackTrace; however for Tier0 Jit and
                // cross-assembly AoT/R2R these inlines will be blocked until Tier1 Jit re-Jits
                // them when they will inline. We don't show them in the StackTrace to bring consistency
                // between this first-pass asm and fully optimized asm.
                return false;
            }

            if (mb.IsDefined(typeof(StackTraceHiddenAttribute), inherit: false))
            {
                // Don't show where StackTraceHidden is applied to the method.
                return false;
            }

            Type? declaringType = mb.DeclaringType;
            // Methods don't always have containing types, for example dynamic RefEmit generated methods.
            if (declaringType != null &&
                declaringType.IsDefined(typeof(StackTraceHiddenAttribute), inherit: false))
            {
                // Don't show where StackTraceHidden is applied to the containing Type of the method.
                return false;
            }

            return true;
        }

        private static bool TryResolveStateMachineMethod(ref MethodBase method, out Type declaringType)
        {
            Debug.Assert(method != null);
            Debug.Assert(method.DeclaringType != null);

            declaringType = method.DeclaringType;

            Type? parentType = declaringType.DeclaringType;
            if (parentType == null)
            {
                return false;
            }

            MethodInfo[]? methods = parentType.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly);
            if (methods == null)
            {
                return false;
            }

            foreach (MethodInfo candidateMethod in methods)
            {
                IEnumerable<StateMachineAttribute>? attributes = candidateMethod.GetCustomAttributes<StateMachineAttribute>(inherit: false);
                if (attributes == null)
                {
                    continue;
                }

                bool foundAttribute = false, foundIteratorAttribute = false;
                foreach (StateMachineAttribute asma in attributes)
                {
                    if (asma.StateMachineType == declaringType)
                    {
                        foundAttribute = true;
                        foundIteratorAttribute |= asma is IteratorStateMachineAttribute || asma is AsyncIteratorStateMachineAttribute;
                    }
                }

                if (foundAttribute)
                {
                    // If this is an iterator (sync or async), mark the iterator as changed, so it gets the + annotation
                    // of the original method. Non-iterator async state machines resolve directly to their builder methods
                    // so aren't marked as changed.
                    method = candidateMethod;
                    declaringType = candidateMethod.DeclaringType!;
                    return foundIteratorAttribute;
                }
            }

            return false;
        }
    }
}
