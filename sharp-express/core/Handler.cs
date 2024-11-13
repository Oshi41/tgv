namespace sharp_express.core;

/// <summary>
/// Base HTTP handler
/// </summary>
public delegate Task Handle(IContext context, Action next, Exception? e = null);