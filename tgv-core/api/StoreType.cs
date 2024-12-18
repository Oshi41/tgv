namespace tgv_core.api;

public enum StoreType
{
    /// <summary>
    /// This item assign to context only and destoy right after context discarded
    /// </summary>
    Assign2Context,
    
    /// <summary>
    /// Item context stored all the application lifetime
    /// </summary>
    Assign2Application,
    
    /// <summary>
    /// Custom validating callback
    /// </summary>
    Custom,
}