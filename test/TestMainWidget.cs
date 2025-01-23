using System.Collections;
using System.Collections.Generic;
using UIWidgetsGallery.gallery;
using Unity.UIWidgets.engine;
using Unity.UIWidgets.widgets;
using UnityEngine;


namespace TestWidget
{
    public class TestMainWidget : UIWidgetsPanel
    {
        protected override Widget createWidget()
        {
            return new TestWidgetOne();
        }
    }


}
