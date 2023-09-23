using System.Windows;
using System.Windows.Input;

// install nuget package: https://stackoverflow.com/a/56240223
using Microsoft.Xaml.Behaviors;

namespace win_tasks.behaviors {

    // from https://stackoverflow.com/a/31586475
    public sealed class BubbleMouseUpEvent : Behavior<UIElement> {
        protected override void OnAttached() {
            base.OnAttached();
            AssociatedObject.MouseUp += AssociatedObject_MouseUp;
        }

        protected override void OnDetaching() {
            AssociatedObject.MouseUp -= AssociatedObject_MouseUp;
            base.OnDetaching();
        }

        void AssociatedObject_MouseUp(object sender, MouseButtonEventArgs e) {
            if (!e.Handled) {
                e.Handled = true;
                var e2 = new MouseButtonEventArgs(e.MouseDevice, e.Timestamp, e.ChangedButton, e.StylusDevice) { RoutedEvent = UIElement.MouseUpEvent };
                AssociatedObject.RaiseEvent(e2);
            }
        }
    }
}
