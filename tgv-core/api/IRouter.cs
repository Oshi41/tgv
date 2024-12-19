using System;
using System.Collections.Generic;

namespace tgv_core.api;

/// <summary>
/// Defines a contract for an HTTP router, which is responsible for handling HTTP request routing
/// and providing a fluent interface for attaching middleware and endpoint handlers.
/// It allows registration of handlers for various HTTP methods and path-specific or global handlers,
/// as well as post-processing and error-handling capabilities.
/// </summary>
public interface IRouter : IMatch, IEnumerable<IMatch>
{
    /// <summary>
    /// Registers one or more HTTP handlers to be used in the routing process. This method allows
    /// the addition of middleware or endpoint handlers that are applied to requests without
    /// path-specific constraints.
    /// </summary>
    /// <param name="handlers">An array of <c>HttpHandler</c> delegates representing the handlers to be used.</param>
    /// <returns>An instance of <c>IRouter</c> for method chaining.</returns>
    IRouter Use(params HttpHandler[] handlers);

    /// <summary>
    /// Register on or more Handler extensions to the routing process. This method allows
    /// addition middleware to create payload assotiated with current HTTP request.
    /// </summary>
    /// <param name="extensions">An array of <see cref="ExtensionFactory"/> of context extension</param>
    /// <typeparam name="T">Payload type</typeparam>
    /// <typeparam name="T1">Payload ID type</typeparam>
    /// <returns>An instance of <c>IRouter</c> for method chaining</returns>
    IRouter Use<T, T1>(params ExtensionFactory<T, T1>[] extensions)
        where T : class
        where T1 : IEquatable<T1>;

    /// <summary>
    /// Registers one or more HTTP handlers to be executed after each request. This method allows
    /// for the addition of handlers that are applied to requests after the main processing is complete,
    /// enabling tasks such as logging or response modification.
    /// </summary>
    /// <param name="handlers">An array of <c>HttpHandler</c> delegates representing the handlers to be used.</param>
    /// <returns>An instance of <c>IRouter</c> for method chaining.</returns>
    IRouter After(params HttpHandler[] handlers);

    /// <summary>
    /// Registers HTTP handlers for a specific path to be used in the routing process. This method allows
    /// the addition of middleware or endpoint handlers that are applied to requests matching the specified path.
    /// </summary>
    /// <param name="path">The path pattern where the handlers will be applied.</param>
    /// <param name="handlers">An array of <c>HttpHandler</c> delegates representing the handlers to be used.</param>
    /// <returns>An instance of <c>IRouter</c> for method chaining.</returns>
    IRouter Use(string path, params HttpHandler[] handlers);

    /// <summary>
    /// Register on or more Handler extensions to the routing process. This method allows
    /// addition middleware to create payload assotiated with current HTTP request.
    /// </summary>
    /// <param name="path">The path pattern where the handlers will be applied</param>
    /// <param name="extensions">An array of <see cref="ExtensionFactory"/> of context extension</param>
    /// <typeparam name="T">Payload type</typeparam>
    /// <typeparam name="T1"></typeparam>
    /// <returns>An instance of <c>IRouter</c> for method chaining</returns>
    IRouter Use<T, T1>(string path, params ExtensionFactory<T, T1>[] extensions)
        where T : class
        where T1 : IEquatable<T1>;

    /// <summary>
    /// Registers one or more HTTP handlers to be executed after routing. This method allows the
    /// addition of middleware or endpoint handlers that will be invoked after initial route processing.
    /// </summary>
    /// <param name="path">The path pattern where the handlers will be applied.</param>
    /// <param name="handlers">An array of <c>HttpHandler</c> delegates representing the handlers to invoke.</param>
    /// <returns>An instance of <c>IRouter</c> for method chaining.</returns>
    IRouter After(string path, params HttpHandler[] handlers);

    /// <summary>
    /// Integrates an existing <c>IRouter</c> into the current routing process. This allows the combination
    /// of multiple routers into a single routing pipeline, facilitating modularization and separation of routing logic.
    /// </summary>
    /// <param name="router">The instance of <c>IRouter</c> to be integrated into the current routing setup.</param>
    /// <returns>An instance of <c>IRouter</c> for method chaining.</returns>
    IRouter Use(IRouter router);

    /// <summary>
    /// Registers one or more HTTP GET handlers to be used in the routing process for a specified path.
    /// </summary>
    /// <param name="path">The path pattern where the handlers will be applied.</param>
    /// <param name="handlers">An array of <c>HttpHandler</c> delegates representing the handlers to be used.</param>
    /// <returns>An instance of <c>IRouter</c> for method chaining.</returns>
    IRouter Get(string path, params HttpHandler[] handlers);

    /// <summary>
    /// Registers one or more HTTP POST handlers to be used in the routing process for a specified path.
    /// </summary>
    /// <param name="path">The path pattern where the handlers will be applied.</param>
    /// <param name="handlers">An array of <c>HttpHandler</c> delegates representing the handlers to be executed for POST requests.</param>
    /// <returns>An instance of <c>IRouter</c> for method chaining.</returns>
    IRouter Post(string path, params HttpHandler[] handlers);

    /// <summary>
    /// Registers one or more HTTP DELETE handlers to be used in the routing process for a specified path.
    /// </summary>
    /// <param name="path">The path pattern where the handlers will be applied.</param>
    /// <param name="handlers">An array of <c>HttpHandler</c> delegates that represent the handlers to be used for DELETE requests.</param>
    /// <returns>An instance of <c>IRouter</c> to allow for method chaining.</returns>
    IRouter Delete(string path, params HttpHandler[] handlers);

    /// <summary>
    /// Registers one or more HTTP PATCH handlers to be used in the routing process for a specified path.
    /// </summary>
    /// <param name="path">The path pattern where the handlers will be applied.</param>
    /// <param name="handlers">An array of <c>HttpHandler</c> delegates representing the handlers to be applied to the PATCH requests.</param>
    /// <returns>An instance of <c>IRouter</c> for method chaining.</returns>
    IRouter Patch(string path, params HttpHandler[] handlers);

    /// <summary>
    /// Registers one or more HTTP PUT handlers to be used in the routing process for a specified path.
    /// </summary>
    /// <param name="path">The path pattern where the handlers will be applied.</param>
    /// <param name="handlers">An array of <c>HttpHandler</c> delegates representing the handlers to be used for the PUT request.</param>
    /// <returns>An instance of <c>IRouter</c> for method chaining.</returns>
    IRouter Put(string path, params HttpHandler[] handlers);

    /// <summary>
    /// Registers one or more HTTP HEAD handlers to be used in the routing process for a specified path.
    /// </summary>
    /// <param name="path">The path pattern where the handlers will be applied.</param>
    /// <param name="handlers">An array of <c>HttpHandler</c> delegates containing the logic to execute when a HEAD request matches the specified path.</param>
    /// <returns>An instance of <c>IRouter</c> for method chaining.</returns>
    IRouter Head(string path, params HttpHandler[] handlers);

    /// <summary>
    /// Registers one or more error handlers may occured during requests for the specified path.
    /// </summary>
    /// <param name="path">The route path for which the handlers will be applied.</param>
    /// <param name="handlers">An array of <c>HttpHandler</c> delegates to be executed for error handling on the specified path.</param>
    /// <returns>An instance of <c>IRouter</c> for method chaining.</returns>
    IRouter Error(string path, params HttpHandler[] handlers);

    /// <summary>
    /// Registers HTTP handlers for the OPTIONS HTTP method for a specific routing path.
    /// </summary>
    /// <param name="path">The path pattern where the handlers will be applied.</param>
    /// <param name="handlers">An array of <c>HttpHandler</c> delegates representing the handlers to process the OPTIONS requests.</param>
    /// <returns>An instance of <c>IRouter</c> to facilitate method chaining.</returns>
    IRouter Options(string path, params HttpHandler[] handlers);

    /// <summary>
    /// Registers HTTP handlers for the CONNECT method to be applied during the routing process.
    /// </summary>
    /// <param name="path">The path pattern where the handlers will be applied.</param>
    /// <param name="handlers">An array of <c>HttpHandler</c> delegates representing the handlers to be used for the CONNECT method.</param>
    /// <returns>An instance of <c>IRouter</c> for method chaining.</returns>
    IRouter Connect(string path, params HttpHandler[] handlers);

    /// <summary>
    /// Registers HTTP handlers for the TRACE method and associates them with a specific path.
    /// This method is used for requests that require the TRACE HTTP method, facilitating diagnostics and request path verification.
    /// </summary>
    /// <param name="path">The path pattern where the handlers will be applied.</param>
    /// <param name="handlers">An array of <c>HttpHandler</c> delegates representing the handlers to be executed for TRACE requests.</param>
    /// <returns>An instance of <c>IRouter</c> for method chaining, allowing further configuration of the router.</returns>
    IRouter Trace(string path, params HttpHandler[] handlers);
}