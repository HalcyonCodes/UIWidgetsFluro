using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.UIWidgets.widgets;
using System;
using UnityEditor.Experimental.GraphView;
using System.Linq;
using Unity.VisualScripting;
using System.Text.RegularExpressions;
using Unity.UIWidgets.material;
using Unity.UIWidgets.cupertino;
using System.Threading.Tasks;
using UnityEngine.Windows;
using Unity.UIWidgets.animation;
using Unity.UIWidgets.ui;
using RSG;

namespace UIWidgetsFluro
{
    public class FluroRouter
    {
        /// The static / singleton instance of FluroRouter
        public static readonly FluroRouter AppRouter = new();

        /// The tree structure that stores the defined routes
        private readonly RouteTree _routeTree = new();


        /// Generic handler for when a route has not been defined
        public Handler notFoundHandler;

        /// <summary>
        /// The default transition duration to use throughout Fluro
        /// </summary>
        public static readonly TimeSpan defaultTransitionDuration = TimeSpan.FromMilliseconds(250);

        /// Creates a PageRoute definition for the passed RouteHandler. You can optionally provide a default transition type.
        public void Define(string routePath, Handler handler, TransitionType? transitionType = null, TimeSpan? transitionDuration = null, RouteTransitionsBuilder transitionBuilder = null)
        {
            transitionDuration = transitionDuration ?? defaultTransitionDuration;
            _routeTree.AddRoute(new AppRoute(routePath, handler, transitionType, transitionDuration, transitionBuilder));
        }


        /// Finds a defined AppRoute for the path value. If no AppRoute definition was found, then function will return null.
        public AppRouteMatch Match(string path)
        {
            return _routeTree.MatchRoute(path);
        }


        /// Similar to Navigator.pop
        public void Pop<T>(BuildContext context, T result = default)
        {
            Navigator.of(context).pop(result);
        }


        /// Similar to Navigator.push but with a few extra features.

        public IPromise<object> NavigateTo(
            BuildContext context, 
            string path, 
            bool replace = false,
            bool clearStack = false, 
            bool maintainState = true,
            bool rootNavigator = false, 
            TransitionType? transition = null, 
            TimeSpan? transitionDuration = null, 
            RouteTransitionsBuilder transitionBuilder = null,
            RouteSettings routeSettings = null)
        {
            var routeMatch = MatchRoute(
                context,
                path,
                transitionType: transition,
                transitionBuilder: transitionBuilder,
                transitionDuration: transitionDuration,
                maintainState: maintainState,
                routeSettings: routeSettings);

            Debug.Log(routeMatch);


            Route route = routeMatch.route;
            IPendingPromise<string> completer = new Promise<string>();

            // 获取与Promise关联的未来结果
            IPromise<object> future = null;

            if (routeMatch.matchType == RouteMatchType.nonVisual)
            {
                completer.Resolve("Non visual route type.");
            }
            else { 
                if (routeMatch.route == null && notFoundHandler != null)
                {
                    route = NotFoundRoute(context, path, maintainState: maintainState);
                }
                
                if(route != null)
                {
                    
                    NavigatorState navigator = Navigator.of(context, rootNavigator: rootNavigator);
                    if (clearStack)
                    {
                        future = navigator.pushAndRemoveUntil(route, (check) => false);
                    }
                    else
                    {
                        future = replace ? navigator.pushReplacement(route) : navigator.push(route);
                    }
                    completer.Resolve("");
                }
                else
                {
                   string error = "No registered route was found to handle '$path'.";
                    Debug.Log(error);
                    completer.Resolve(error);
                }
            }

            return future;
        }

        /// Attempt to match a route to the provided path.
        public RouteMatch MatchRoute
            (BuildContext context, 
            string path, 
            TransitionType? transitionType = null,
            RouteTransitionsBuilder transitionBuilder = null, 
            TimeSpan? transitionDuration = null,
            RouteSettings routeSettings = null, 
            bool maintainState = true)
        {
            
            var settingsToUse = routeSettings ?? new RouteSettings (name: path);

            var match = _routeTree.MatchRoute(path);
            var route = match.route;
            

            if (transitionDuration == null && route.transitionDuration != null)
            {
                transitionDuration = route.transitionDuration;
            }

            Handler handler = route != null ? route.handler : notFoundHandler;
            TransitionType? transition = transitionType;

            if (transitionType == null) { 
                transition = route != null ? route.transitionType : TransitionType.native;
            }

            if (route == null && notFoundHandler == null)
            {
                return new RouteMatch(
                     null, //test
                     matchType: RouteMatchType.noMatch, 
                     errorMessage:"No matching route was found");
            }


            var parameters = match.parameters ?? new Dictionary<string, List<string>>();

            if (handler.type == HandlerType.function)
            {
                handler.handlerFunc(context, parameters);
                return new RouteMatch(
                    null,
                    matchType: RouteMatchType.nonVisual
                    );
            }


            //Route Creator = (RouteSettings routeSettings, Dictionary<string, List<string>> parameters) =>
            RouteCreator Creator = (
                RouteSettings routeSettings,
                Dictionary<string, List<string>> parameters,
                RouteTransitionsBuilder dTransitionsBuilder
                ) =>
            {
                bool isNativeTransition = (transition == TransitionType.native ||
          transition == TransitionType.nativeModal);

                if (isNativeTransition)
                {
                    return new MaterialPageRoute(
                      settings: routeSettings,
                      fullscreenDialog: transition == TransitionType.nativeModal,
                      maintainState: maintainState,
                      builder: (BuildContext context) =>
                      {
                          return handler.handlerFunc(context, parameters) ??
                              SizedBox.shrink();
                      }
        );
                }
                else if (transition == TransitionType.material ||
                    transition == TransitionType.materialFullScreenDialog)
                {
                    return new MaterialPageRoute(
                      settings: routeSettings,
                      fullscreenDialog:
                          transition == TransitionType.materialFullScreenDialog,
                      maintainState: maintainState,
                      builder: (BuildContext context) =>
                      {
                          return handler.handlerFunc(context, parameters) ??
                              SizedBox.shrink();
                      }
        );
                }
                else if (transition == TransitionType.cupertino ||
                    transition == TransitionType.cupertinoFullScreenDialog)
                {
                    return new CupertinoPageRoute(
                      settings: routeSettings,
                      fullscreenDialog:
                          transition == TransitionType.cupertinoFullScreenDialog,
                      maintainState: maintainState,
                      builder: (BuildContext context) =>
                      {
                          return handler.handlerFunc(context, parameters) ??
                              SizedBox.shrink();
                      }
        );
                }
                else
                {
                    RouteTransitionsBuilder routeTransitionsBuilder;

                    if (transition == TransitionType.custom)
                    {
                        routeTransitionsBuilder =
                            dTransitionsBuilder ?? route.transitionBuilder;
                        
                    }
                    else
                    {
                        routeTransitionsBuilder = _standardTransitionsBuilder(transition);
                    }

                    return new PageRouteBuilder(
                      settings: routeSettings,
                      maintainState: maintainState,
                      pageBuilder: (BuildContext context, Animation<float> animation,
                          Animation<float> secondaryAnimation) =>
                      {
                          return handler.handlerFunc(context, parameters) ??
                              SizedBox.shrink();
                      },
          transitionDuration: transition == TransitionType.none
              ? TimeSpan.Zero
              : (transitionDuration ??
                  route?.transitionDuration ??
                  defaultTransitionDuration),
          /*reverseTransitionDuration: transition == TransitionType.none
              ? TimeSpan.Zero
              : (transitionDuration ??
                  route?.transitionDuration ??
                  defaultTransitionDuration),*/
          transitionsBuilder: transition == TransitionType.none
              ? (_, __, ___, child) => child
              : routeTransitionsBuilder!
        );
                }
            };

            return new RouteMatch(
                route : Creator(settingsToUse, parameters,transitionBuilder),
                matchType: RouteMatchType.visual);
        }


        public delegate Route RouteCreator(
            RouteSettings routeSettings, 
            Dictionary<string, List<string>> parameters,
            RouteTransitionsBuilder dTransitionBuilder);

        //
        RouteTransitionsBuilder _standardTransitionsBuilder(
          TransitionType? transitionType)
        {
            return (
              BuildContext context,
              Animation<float> animation,
              Animation<float> secondaryAnimation,
              Widget child

            ) =>
            {
                if (transitionType == TransitionType.fadeIn)
                {
                    return new FadeTransition(opacity: animation, child: child);
                }
                else
                {
                    var topLeft = new Offset(0.0f, 0.0f);
                    var topRight = new Offset(1.0f, 0.0f);
                    var bottomLeft = new Offset(0.0f, 1.0f);

                    var startOffset = bottomLeft;
                    var endOffset = topLeft;

                    if (transitionType == TransitionType.inFromLeft)
                    {
                        startOffset = new Offset(-1.0f, 0.0f);
                        endOffset = topLeft;
                    }
                    else if (transitionType == TransitionType.inFromRight)
                    {
                        startOffset = topRight;
                        endOffset = topLeft;
                    }
                    else if (transitionType == TransitionType.inFromBottom)
                    {
                        startOffset = bottomLeft;
                        endOffset = topLeft;
                    }
                    else if (transitionType == TransitionType.inFromTop)
                    {
                        startOffset = new Offset(0.0f, -1.0f);
                        endOffset = topLeft;
                    }

                    return new SlideTransition(
                      /*Tween<Offset>(
                      begin: startOffset,
                      end: endOffset,
                    ).animate(animation)*/
                      position: null,

                      child: child

                    );
                }
            };
        }



        /// Creates a not found route.
        private Route NotFoundRoute(
            BuildContext context, 
            string path, 
            bool maintainState)
        {
            RouteCreator Creator = (
                RouteSettings routeSettings,
                Dictionary<string, List<string>> parameters,
                RouteTransitionsBuilder dTransitionsBuilder
                ) =>
            {
                return new MaterialPageRoute(
                    settings: routeSettings,
        maintainState: maintainState != null ? maintainState : true,
        builder: (BuildContext context) =>
        {
            return notFoundHandler?.handlerFunc(context, parameters) ??
                SizedBox.shrink();
        }

                 );
            };

                return Creator(new RouteSettings(name:path), new Dictionary<string, List<string>>(), null);
        }



        /// Route generation method. This function can be used as a way to create routes on-the-fly
        /// if any defined handler is found. It can also be used with the [MaterialApp.onGenerateRoute]
        /// property as callback to create routes that can be used with the [Navigator] class.
        public Route Generator(RouteSettings routeSettings)
        {
            RouteMatch match = MatchRoute(
              null,
              routeSettings.name,
              routeSettings: routeSettings
        
            );

            return match.route;
        }

        //used with the [MaterialApp.onGenerateInitailRoute]
        public List<Route> GeneratorInitail(String name)
        {
            RouteMatch match = MatchRoute(
              null,
              name
            );
            List<Route> result = new() { };
            result.Add(match.route!);
            return result;
        }


        /// <summary>
        /// Prints the route tree so you can analyze it.
        /// </summary>
        public void PrintTree()
        {
            _routeTree.PrintTree();
        }
    }
}
