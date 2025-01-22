using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.UIWidgets.widgets;
using System;

namespace UIWidgetsFluro
{
    public enum HandlerType
    {
        route,
        function,
    }

    /// The type of transition to use when pushing/popping a route.
    ///
    /// [TransitionType.custom] must also provide a transition when used.
    public enum TransitionType
    {
        native,
        nativeModal,
        inFromLeft,
        inFromTop,
        inFromRight,
        inFromBottom,
        fadeIn,
        custom,
        material,
        materialFullScreenDialog,
        cupertino,
        cupertinoFullScreenDialog,
        none,
    }

    /// The match type of the route.
    public enum RouteMatchType
    {
        visual,
        nonVisual,
        noMatch,
    }


    /// Builds out a screen based on string path [parameters] and context.
    ///
    /// Note: you can access [RouteSettings] with the [context.settings] extension
    public delegate Widget HandlerFunc(
    BuildContext context,
    Dictionary<string, List<string>> parameters
    );

    /// The handler to register with [FluroRouter.define]
    public class Handler
    {
        public HandlerType type;
        public HandlerFunc handlerFunc;

        public Handler( HandlerFunc handlerFunc, HandlerType type = HandlerType.route)
        {
            this.handlerFunc = handlerFunc;
            this.type = type;
        }
    }

    /// A function that creates new routes.
    public delegate Route RouteCreator(
    RouteSettings route,
    Dictionary<string, List<string>> parameters
    );

    /// A route that is added to the router tree.
    public class AppRoute
    {
        public string route;
        public Handler handler; // 使用 Delegate 类型来表示动态函数
        public TransitionType? transitionType;
        public TimeSpan? transitionDuration;
        public RouteTransitionsBuilder transitionBuilder; // 假设 TransitionBuilder 的签名

        public AppRoute(
            string route,
            Handler handler,
            TransitionType? transitionType = null,
            TimeSpan? transitionDuration = null,
            RouteTransitionsBuilder transitionBuilder = null
        )
        {
            this.route = route;
            this.handler = handler;
            this.transitionType = transitionType;
            this.transitionDuration = transitionDuration;
            this.transitionBuilder = transitionBuilder;
        }
    }

    /// The route that was matched.
    public class RouteMatch
    {
        public Route route;
        public RouteMatchType matchType;
        public String errorMessage;
        public RouteMatch(
            Route route,
            RouteMatchType matchType = RouteMatchType.noMatch,
            String errorMessage = "Unable to match route. Please check the logs."
         )
        {
            this.errorMessage = errorMessage;
            this.route = route;
            this.matchType = matchType;
        }
    }

    /// When the route is not found.
    public class RouteNotFoundException : Exception
    {
        public string Path { get; }

        public RouteNotFoundException(string message, string path) : base(message)
        {
            Path = path;
        }

        public override string ToString()
        {
            return $"No registered route was found to handle '{Path}'";
        }
    }
} 


