﻿using System.Windows;
using System.Windows.Input;

// install nuget package: https://stackoverflow.com/a/56240223
using Microsoft.Xaml.Behaviors;

namespace win_tasks.behaviors {

    // from https://stackoverflow.com/a/31586475
    public sealed class BubbleScrollEvent : Behavior<UIElement> {
        protected override void OnAttached() {
            base.OnAttached();
            AssociatedObject.PreviewMouseWheel += AssociatedObject_PreviewMouseWheel;
        }

        protected override void OnDetaching() {
            AssociatedObject.PreviewMouseWheel -= AssociatedObject_PreviewMouseWheel;
            base.OnDetaching();
        }

        void AssociatedObject_PreviewMouseWheel(object sender, MouseWheelEventArgs e) {
            if (!e.Handled) {
                e.Handled = true;
                var e2 = new MouseWheelEventArgs(e.MouseDevice, e.Timestamp, e.Delta) { RoutedEvent = UIElement.MouseWheelEvent };
                AssociatedObject.RaiseEvent(e2);
            }
        }
    }
}
