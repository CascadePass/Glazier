using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Controls;

namespace CascadePass.Glazier.UI
{
    public static class Animator
    {
        #region Settings panel

        public static void ShowSettingsPanel(FrameworkElement panel, Window window, bool useAnimation)
        {
            panel.Visibility = Visibility.Visible;

            if (!useAnimation)
            {
                return;
            }

            var transform = new TranslateTransform();
            panel.RenderTransform = transform;
            transform.X = window.ActualWidth;

            var slideAnimation = new DoubleAnimation
            {
                From = window.ActualWidth,
                To = 0,
                Duration = TimeSpan.FromSeconds(1.2),
                EasingFunction = new QuarticEase { EasingMode = EasingMode.EaseInOut }
            };

            transform.BeginAnimation(TranslateTransform.XProperty, slideAnimation);
        }

        public static void HideSettingsPanel(FrameworkElement panel, Window window, bool useAnimation)
        {
            if (!useAnimation)
            {
                panel.Visibility = Visibility.Collapsed;
                return;
            }

            var slideAnimation = new DoubleAnimation
            {
                From = 0,
                To = -window.ActualWidth,
                Duration = TimeSpan.FromSeconds(0.7),
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn }
            };

            slideAnimation.Completed += (s, e) => panel.Visibility = Visibility.Collapsed;

            var transform = new TranslateTransform();
            panel.RenderTransform = transform;
            transform.BeginAnimation(TranslateTransform.XProperty, slideAnimation);
        }

        #endregion

        #region Input form

        public static void HideInputForm(Border inputFormBorder)
        {
            var animation = new DoubleAnimation
            {
                From = inputFormBorder.ActualHeight,
                To = 0,
                Duration = TimeSpan.FromSeconds(0.5)
            };

            animation.Completed += (s, e) => inputFormBorder.Visibility = Visibility.Collapsed;

            var storyboard = new Storyboard();
            Storyboard.SetTarget(animation, inputFormBorder);
            Storyboard.SetTargetProperty(animation, new PropertyPath("Height"));

            storyboard.Children.Add(animation);
            storyboard.Begin();
        }

        public static void ShowInputForm(Border inputFormBorder, double originalHeight)
        {
            inputFormBorder.Visibility = Visibility.Visible;

            var animation = new DoubleAnimation
            {
                From = 0,
                To = originalHeight,
                Duration = TimeSpan.FromSeconds(0.5)
            };

            var storyboard = new Storyboard();
            Storyboard.SetTarget(animation, inputFormBorder);
            Storyboard.SetTargetProperty(animation, new PropertyPath("Height"));

            storyboard.Children.Add(animation);
            storyboard.Begin();
        }

        #endregion

        #region Expander animations

        public static void OpenExpander(FrameworkElement expanderContent)
        {
            var animation = new DoubleAnimation
            {
                From = 30,
                To = expanderContent.ActualHeight,
                Duration = TimeSpan.FromSeconds(0.5)
            };

            Storyboard storyboard = new();
            storyboard.Children.Add(animation);
            Storyboard.SetTarget(animation, expanderContent);
            Storyboard.SetTargetProperty(animation, new PropertyPath("MaxHeight"));
            storyboard.Begin();
        }

        public static void CloseExpander(FrameworkElement expanderContent, Expander expander)
        {
            var animation = new DoubleAnimation
            {
                From = expanderContent.ActualHeight,
                To = 30,
                Duration = TimeSpan.FromSeconds(0.5)
            };

            animation.Completed += (s, e) => expander.IsExpanded = false; // Collapse AFTER animation

            Storyboard storyboard = new();
            storyboard.Children.Add(animation);
            Storyboard.SetTarget(animation, expanderContent);
            Storyboard.SetTargetProperty(animation, new PropertyPath("MaxHeight"));
            storyboard.Begin();
        }

        #endregion

        #region Algorithm selection

        public static void SelectAlgorithmRadioButton(RadioButton selectedButton, FrameworkElement glazeMethodSelector, TranslateTransform highlightTransform, FrameworkElement selectionHighlight)
        {
            var transform = selectedButton.TransformToVisual(glazeMethodSelector);
            var position = transform.Transform(new Point(0, 0));

            double dpiFactor = VisualTreeHelper.GetDpi(selectedButton).PixelsPerDip;
            double buttonCenterY = position.Y - (selectedButton.ActualHeight / 4) * dpiFactor;

            var animation = new DoubleAnimation
            {
                To = buttonCenterY,
                Duration = TimeSpan.FromSeconds(0.3),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            };

            highlightTransform.BeginAnimation(TranslateTransform.YProperty, animation);

            // Match the indicator height to the selected button's height
            selectionHighlight.Height = selectedButton.ActualHeight;
        }

        #endregion
    }
}
