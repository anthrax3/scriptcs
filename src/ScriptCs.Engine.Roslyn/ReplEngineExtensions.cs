﻿using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.Scripting;
using ScriptCs.Contracts;

namespace ScriptCs.Engine.Roslyn
{
    public static class ReplEngineExtensions
    {
        public static ICollection<string> GetLocalVariables(this IReplEngine replEngine, string sessionKey,
            ScriptPackSession scriptPackSession)
        {
            if (scriptPackSession != null && scriptPackSession.State.ContainsKey(sessionKey))
            {
                var sessionState = (SessionState<ScriptState>)scriptPackSession.State[sessionKey];
                return sessionState.Session.Variables.Select(x => $"{x.Type} {x.Name}").Distinct().ToArray();
            }

            return new string[0];
        }
    }
}