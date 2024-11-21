namespace tgv.core;

/// <summary>
/// Base HTTP handler
/// </summary>
public delegate Task Handle(Context Context, Action next, Exception? e = null);