using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.UIWidgets.widgets;
using UIWidgetsFluro;
using Unity.UIWidgets.material;
using Unity.UIWidgets.painting;
using RouteTest;
using Unity.UIWidgets.cupertino;


namespace TestWidget
{
    /*public class TestWidgetOne : StatefulWidget
    {
        public override State createState()
        {
            return new _TestWidgetOneState();
        }
    }*/

    //public class _TestWidgetOneState : State<TestWidgetOne
    public class TestWidgetOne : StatelessWidget
    {
       
        public override Widget build(BuildContext context)
        {
            Widget home = new InkWell(
                onTap: () => { 
                    Debug.Log("press");
                    var q = Navigator.of(context, rootNavigator: true);
                    Navigator.of(context, rootNavigator: true).pop();
                    //Applicationc.router.NavigateTo(context, "/HomePage", transition: TransitionType.fadeIn);
                    /* Navigator.push(
               context,
               new MaterialPageRoute(builder: (context) => new TestWidgetTwo()));*/
                    //Navigator.pushNamed(context, "/home");

                },

                child: new Container(
                    height: 100f, width: 100f,
                    color: Colors.blue
                    ));
            //=== test ===
            //FluroRouter router = new FluroRouter();
            //Routes.configureRotes(router);
            //Applicationc.router = router;
            //===

            return new MaterialApp(
                title: "TestWidget1",
                color: Colors.grey,
                 //routes: this._buildRoutes(),
                 //onGenerateRoute: Applicationc.router.Generator,
                 initialRoute: "/",
      routes: new Dictionary<string, WidgetBuilder>
        {
            { "/home", (BuildContext context) => new TestWidgetTwo() },
            
        },
                home: new Scaffold(
                    body: home
                    )
            );
        }
    }

    //====
    public class TestWidgetTwo : StatefulWidget
    {
        public override State createState()
        {
            return new _TestWidgetTwoState();
        }
    }

    public class _TestWidgetTwoState : State<TestWidgetTwo>
    {
        public override Widget build(BuildContext context)
        {
            Widget home = new InkWell(
                onTap: () => { },
                child: new Container(
                    height: 100, width: 100,
                    color: Colors.black38
                    ));
            return new Scaffold(
                body: home
            );  
            
        }
    }

}

