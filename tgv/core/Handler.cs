namespace tgv.core;

/// <summary>
/// Base HTTP handler
/// </summary>
public delegate Task Handle(Context context, Action next, Exception? e = null);