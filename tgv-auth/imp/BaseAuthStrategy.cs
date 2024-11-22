using System;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using tgv_common.api;
using tgv_common.imp;

namespace tgv_auth.imp;

public class BaseAuthStrategy : AuthStrategyBase<(string name, string pass)?>
{
    private readonly Encoding _encoding;
    private const string passKeyName = "_password";

    public BaseAuthStrategy(IStore<(string name, string pass)?> store, Logger logger, Encoding? encoding = null)
        : base(store, logger)
    {
        _encoding = encoding ?? Encoding.UTF8;
    }

    protected override (string name, string pass)? GetCredentials(string header)
    {
        try
        {
            // trying to get creds from URL
            if (Uri.IsWellFormedUriString(header, UriKind.Absolute)
                && TryParse(header, ':', out var credentials))
            {
                return credentials;
            }
            
            // trying to parse header value
            var base64 = header.Replace(Scheme + " ", "");
            if (!string.IsNullOrEmpty(base64))
            {
                var bytes = Convert.FromBase64String(base64);
                if (bytes.Any())
                {
                    var text = _encoding.GetString(bytes);
                    if (TryParse(text, '=', out credentials))
                        return credentials;
                }
            }
        }
        catch (Exception e)
        {
            _logger.Debug($"Error during {Scheme} credentials obtaining: {e}");
        }
        

        return null;
    }

    private bool TryParse(string text, char splitter, out (string name, string pass)? result)
    {
        result = null;
        
        var arr = text.Split(splitter);
        if (new[] { 0, 1 }.All(i => !string.IsNullOrEmpty(arr?.ElementAtOrDefault(i))))
        {
            result = (arr![0], arr[1]);
            return true;
        }

        return false;
    }

    protected override string GetUniqueId((string name, string pass)? credentials)
    {
        return credentials!.Value.name;
    }

    public override string Scheme => "Basic";

    public override string Challenge(Context ctx)
    {
        return $"{Scheme} charset={_encoding.WebName}";
    }

    protected override async Task<UserEntry?> GetUserAsync((string name, string pass)? credentials)
    {
        var user = await base.GetUserAsync(credentials);
        if (user != null)
        {
            user.Claims.Add(passKeyName, credentials!.Value.pass);
        }
        return user;
    }

    public override string? ToHeader(ClaimsIdentity identity)
    {
        var passClaim = identity.FindFirst(x => x.Type == passKeyName);
        if (passClaim == null) return null;
        
        var text = $"{identity.Name}={passClaim.Value}";
        var bytes = _encoding.GetBytes(text);
        var base64 = Convert.ToBase64String(bytes);
        return $"{Scheme} {base64}";
    }
}