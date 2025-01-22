using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.UIWidgets.widgets;
using System;
using UnityEditor.Experimental.GraphView;
using System.Linq;
using Unity.VisualScripting;
using System.Text.RegularExpressions;


namespace UIWidgetsFluro
{
    /// A [RouteTreeNote] type
    public enum RouteTreeNodeType
    {
        component,
        parameter,
    }

    /// A matched [AppRoute]
    public class AppRouteMatch
    {
        public AppRoute route;
        public Dictionary<string, List<string>> parameters = new();

        public AppRouteMatch(AppRoute route)
        {
            this.route = route;
        }
    }

    /// A node on [RouteTree]
    public class RouteTreeNode
    {
        public string part;
        public RouteTreeNodeType? type;
        public RouteTreeNode parent;
        public List<AppRoute> routes = new();
        public List<RouteTreeNode> nodes = new();

        public RouteTreeNode(string part, RouteTreeNodeType? type)
        {
            this.part = part;
            this.type = type;
        }

        public bool IsParameter()
        {
            return type == RouteTreeNodeType.parameter;
        }
    }




    /// A matched [RouteTreeNode]
    public class RouteTreeNodeMatch
    {
        public RouteTreeNode node;
        public Dictionary<string, List<string>> parameters;

        public RouteTreeNodeMatch(RouteTreeNode node)
        {
            this.node = node;
            this.parameters = new Dictionary<string, List<string>>();
        }

        public RouteTreeNodeMatch(RouteTreeNodeMatch match, RouteTreeNode node)
        {
            this.node = node;
            this.parameters = new Dictionary<string, List<string>>();

            if (match != null)
            {
                foreach (var kvp in match.parameters)
                {
                    parameters[kvp.Key] = new List<string>(kvp.Value);
                }
            }
        }
    }




    public class RouteTree
    {
        private readonly List<RouteTreeNode> _nodes = new List<RouteTreeNode>();
        private bool _hasDefaultRoute = false;

        /// <summary>
        /// Add a route to the route tree
        /// </summary>
        /// <param name="route">The route to add</param>
        public void AddRoute(AppRoute route)
        {
            string path = route.route;
            // is root/default route, just add it
            if (path == Navigator.defaultRouteName)
            {
                if (_hasDefaultRoute)
                {
                    // throw an error because the internal consistency of the router
                    // could be affected
                    throw new InvalidOperationException("Default route was already defined");
                }

                var node = new RouteTreeNode(path, RouteTreeNodeType.component);
                node.routes.Add(route);
                _nodes.Add(node);
                _hasDefaultRoute = true;
                return;
            }

            if (path.StartsWith("/"))
            {
                path = path.Substring(1);
            }

            var pathComponents = path.Split('/');

            RouteTreeNode parent = null;

            for (int i = 0; i < pathComponents.Length; i++)
            {
                string component = pathComponents[i];
                RouteTreeNode node = NodeForComponent(component, parent);

                if (node == null)
                {
                    RouteTreeNodeType type = TypeForComponent(component);
                    node = new RouteTreeNode(component, type);
                    node.parent = parent;
                    

                    if (parent == null)
                    {
                        _nodes.Add(node);
                    }
                    else
                    {
                        parent.nodes.Add(node);
                    }
                }

                if (i == pathComponents.Length - 1)
                {
                    node.routes.Add(route);
                }

                parent = node;
            }
        }

        /// <summary>
        /// Match a route in the route tree
        /// </summary>
        /// <param name="path">The path to match</param>
        /// <returns>The matched route or null if no match</returns>
        public AppRouteMatch MatchRoute(string path)
        {
            string usePath = path;

            if (usePath.StartsWith("/"))
            {
                usePath = path.Substring(1);
            }

            var components = usePath.Split("/");

            if (path == Navigator.defaultRouteName)
            {
                components = new[] { "/" };
            }

            var nodeMatches = new Dictionary<RouteTreeNode, RouteTreeNodeMatch>();
            var nodesToCheck = _nodes;

            foreach (var checkComponent in components)
            {
                var currentMatches = new Dictionary<RouteTreeNode, RouteTreeNodeMatch>();
                var nextNodes = new List<RouteTreeNode>();

                string pathPart = checkComponent;
                Dictionary<string, List<string>> queryMap = null;

                if (checkComponent.Contains("?"))
                {
                    var splitParam = checkComponent.Split("?");
                    pathPart = splitParam[0];
                    queryMap = ParseQueryString(splitParam[1]);
                }

                foreach (var node in nodesToCheck)
                {
                    bool isMatch = (node.part == pathPart || node.IsParameter());

                    if (isMatch)
                    {
                        RouteTreeNodeMatch parentMatch =  nodeMatches[node.parent];
                        var match = new RouteTreeNodeMatch(parentMatch, node);
                        if (node.IsParameter())
                        {
                            string paramKey = node.part.Substring(1);
                            match.parameters[paramKey] = new List<string> { pathPart };
                        }
                        if (queryMap != null)
                        {
                            match.parameters.AddRange(queryMap);
                        }
                        currentMatches[node] = match;
                        nextNodes.AddRange(node.nodes);
                    }
                }

                nodeMatches = currentMatches;
                nodesToCheck = nextNodes;

                if (currentMatches.Values.Count == 0)
                {
                    return null;
                }
            }

            var matches = nodeMatches.Values.ToList();

            if (matches.Any())
            {
                var match = matches.First();
                var nodeToUse = match.node;
                var routes = nodeToUse.routes;

                if (routes.Any())
                {
                    var routeMatch = new AppRouteMatch(routes.First());
                    routeMatch.parameters = match.parameters;
                    return routeMatch;
                }
            }

            return null;
        }

        /// <summary>
        /// Print the route tree
        /// </summary>
        public void PrintTree()
        {
            PrintSubTree();
        }

        /// <summary>
        /// Print a subtree of the route tree
        /// </summary>
        /// <param name="parent">The parent node</param>
        /// <param name="level">The current level</param>
        private void PrintSubTree(RouteTreeNode? parent = null, int level = 0)

        {
            var nodes = parent != null ? parent.nodes : _nodes;

            foreach (var node in nodes)
            {
                var indent = "";

                for (var i = 0; i < level; i++)
                {
                    indent += "    ";
                }

                Debug.Log($"{indent}{node.part}: total routes={node.routes.Count}");

                if (node.nodes.Any())
                {
                    PrintSubTree(parent: node, level:level + 1);
                }
            }
        }

        /// <summary>
        /// Find a node for a component
        /// </summary>
        /// <param name="component">The component to find</param>
        /// <param name="parent">The parent node</param>
        /// <returns>The found node or null</returns>
        private RouteTreeNode NodeForComponent(string component, RouteTreeNode parent)
        {
            var nodes = parent != null ? parent.nodes : _nodes;

            foreach (var node in nodes)
            {
                if (node.part == component)
                {
                    return node;
                }
            }

            return null;
        }

        /// <summary>
        /// Determine the type of a component
        /// </summary>
        /// <param name="component">The component to check</param>
        /// <returns>The type of the component</returns>
        private RouteTreeNodeType TypeForComponent(string component)
        {
            return IsParameterComponent(component) ? RouteTreeNodeType.parameter : RouteTreeNodeType.component;
        }

        /// <summary>
        /// Check if a component is a parameter
        /// </summary>
        /// <param name="component">The component to check</param>
        /// <returns>True if the component is a parameter, otherwise false</returns>
        private bool IsParameterComponent(string component)
        {
            return component.StartsWith(":");
        }

        /// <summary>
        /// Parse a query string into a dictionary
        /// </summary>
        /// <param name="query">The query string</param>
        /// <returns>A dictionary of query parameters</returns>
        public Dictionary<string, List<string>> ParseQueryString(string query)
        {
            var paramsDict = new Dictionary<string, List<string>>();

         

            var search = new Regex(@"([^&=]+)=?([^&]*)");
           
            if (query.StartsWith("?"))
            {
                query = query.Substring(1);
            }
            var decode = new Func<string, string>(s => Uri.UnescapeDataString(s.Replace('+', ' ')));

            foreach (Match match in search.Matches(query))
            {
                var key = decode(match.Groups[1].Value);
                var value = decode(match.Groups[2].Value);

                if (paramsDict.ContainsKey(key))
                {
                    paramsDict[key].Add(value);
                }
                else
                {
                    paramsDict[key] = new List<string> { value };
                }
            }

            return paramsDict;
        }
    }

}