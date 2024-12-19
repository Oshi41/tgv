using System;
using System.ComponentModel.DataAnnotations;
using tgv_auth.api;

namespace tgv_auth.imp.basic;

public class BasicCredentials(string username, string password) : ICredentials
{
    public readonly string Username = username;
    public readonly string Password = password;

    public AuthSchemes Scheme => AuthSchemes.Basic;

    protected bool Equals(BasicCredentials other)
    {
        return Username == other.Username && Password == other.Password;
    }

    public bool Equals(ICredentials other)
    {
        return Equals(other as object);
    }

    public override bool Equals(object? obj)
    {
        if (obj is null) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != GetType()) return false;
        return Equals((BasicCredentials)obj);
    }

    public override int GetHashCode()
    {
        unchecked
        {
            return (Username.GetHashCode() * 397) ^ Password.GetHashCode();
        }
    }

    public int CompareTo(object obj)
    {
        return Equals(obj) 
            ? 0 
            : 1;
    }
}