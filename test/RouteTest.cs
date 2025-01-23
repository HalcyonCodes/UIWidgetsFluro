using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UIWidgetsFluro;
using Unity.UIWidgets.widgets;
using System;
using TestWidget;

namespace RouteTest
{
    public class Applicationc
    {
        public static FluroRouter router;

        public static Handler homeHandler = new Handler(
            handlerFunc: new HandlerFunc((BuildContext context, Dictionary<string, List<string>> parameters) =>
            {
                return new TestWidgetTwo();
            })
        );

    }

    public class Routes
    {
        static string root = "/";
        static string test = "/HomePage";
        
        public static void configureRotes(FluroRouter router)
        {
            router.notFoundHandler = new Handler(
            handlerFunc: new HandlerFunc((BuildContext context, Dictionary<string, List<string>> parameters) =>
            {
                return new TestWidgetOne();
            }));

            router.Define(test, Applicationc.homeHandler);
        }

    }
    
}