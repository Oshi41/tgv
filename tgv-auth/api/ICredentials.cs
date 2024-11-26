﻿using System;
using System.Runtime.Caching;

namespace tgv_auth.api;

/// <summary>
/// Base interface for credentials
/// </summary>
public interface ICredentials : IEquatable<ICredentials>
{
    AuthSchemes Scheme { get; }
}