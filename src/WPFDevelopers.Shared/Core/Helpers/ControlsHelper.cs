﻿using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Media;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Interop;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Xml;

namespace WPFDevelopers.Helpers
{
    public class ControlsHelper : DependencyObject
    {
        private static Win32.DeskTopSize size;
        
        public static void WindowShake(Window window = null)
        {
            if (window == null)
                if (Application.Current != null && Application.Current.Windows.Count > 0)
                    window = Application.Current.Windows.OfType<Window>().FirstOrDefault(o => o.IsActive);

            var doubleAnimation = new DoubleAnimation
            {
                From = window.Left,
                To = window.Left + 15,
                Duration = TimeSpan.FromMilliseconds(50),
                AutoReverse = true,
                RepeatBehavior = new RepeatBehavior(3),
                FillBehavior = FillBehavior.Stop
            };
            window.BeginAnimation(Window.LeftProperty, doubleAnimation);
            var wavUri = new Uri(@"pack://application:,,,/WPFDevelopers;component/Resources/Audio/shake.wav");
            var streamResource = Application.GetResourceStream(wavUri);
            var player1 = new SoundPlayer(streamResource.Stream);
            player1.Play();
        }


        public static BitmapFrame CreateResizedImage(ImageSource source, int width, int height, int margin)
        {
            var rect = new Rect(margin, margin, width - margin * 2, height - margin * 2);

            var group = new DrawingGroup();
            RenderOptions.SetBitmapScalingMode(group, BitmapScalingMode.HighQuality);
            group.Children.Add(new ImageDrawing(source, rect));

            var drawingVisual = new DrawingVisual();
            using (var drawingContext = drawingVisual.RenderOpen())
            {
                drawingContext.DrawDrawing(group);
            }

            var resizedImage = new RenderTargetBitmap(
                width, height,
                96, 96,
                PixelFormats.Default);
            resizedImage.Render(drawingVisual);
            return BitmapFrame.Create(resizedImage);
        }
        

        public static BitmapSource Capture()
        {
            IntPtr hBitmap;
            var hDC = Win32.GetDC(Win32.GetDesktopWindow());
            var hMemDC = Win32.CreateCompatibleDC(hDC);
            size.cx = Win32.GetSystemMetrics(0);
            size.cy = Win32.GetSystemMetrics(1);
            hBitmap = Win32.CreateCompatibleBitmap(hDC, size.cx, size.cy);
            if (hBitmap != IntPtr.Zero)
            {
                var hOld = Win32.SelectObject(hMemDC, hBitmap);
                Win32.BitBlt(hMemDC, 0, 0, size.cx, size.cy, hDC, 0, 0,
                    Win32.TernaryRasterOperations.SRCCOPY);
                Win32.SelectObject(hMemDC, hOld);
                Win32.DeleteDC(hMemDC);
                Win32.ReleaseDC(Win32.GetDesktopWindow(), hDC);
                var bsource = Imaging.CreateBitmapSourceFromHBitmap(hBitmap, IntPtr.Zero, Int32Rect.Empty,
                    BitmapSizeOptions.FromEmptyOptions());
                Win32.DeleteObject(hBitmap);
                GC.Collect();
                return bsource;
            }
            return null;
        }
       
        public static AdornerLayer GetAdornerLayer(Visual visual)
        {
            if (visual == null) return null;  
            if (visual is AdornerDecorator decorator)
                return decorator.AdornerLayer;
            if (visual is ScrollContentPresenter presenter)
                return presenter.AdornerLayer;
            if (visual is Window window)
            {
                var visualContent = window.Content as Visual;
                return AdornerLayer.GetAdornerLayer(visualContent ?? visual);
            }
            return AdornerLayer.GetAdornerLayer(visual);
        }

        public static Window GetDefaultWindow()
        {
            Window window = null;
            if (Application.Current != null && Application.Current.Windows.Count > 0)
            {
                window = Application.Current.Windows.OfType<Window>().FirstOrDefault(o => o.IsActive);
                if (window == null)
                    window = Enumerable.FirstOrDefault(Application.Current.Windows.OfType<Window>());
            }
            return window;
        }

        public static Thickness GetPadding(FrameworkElement element)
        {
            Type elementType = element.GetType();
            PropertyInfo paddingProperty = elementType.GetProperty("Padding");
            if (paddingProperty != null)
                return (Thickness)paddingProperty.GetValue(element, null);
            return new Thickness();
        }

        public static T FindVisualChild<T>(DependencyObject parent) where T : DependencyObject
        {
            int childCount = VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < childCount; i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                if (child is T typedChild)
                    return typedChild;
                var result = FindVisualChild<T>(child);
                if (result != null)
                    return result;
            }
            return null;
        }

        public static object GetXmlReader(object Content)
        {
            try
            {
                if (Content is string contentString)
                    Content = new TextBlock { Text = contentString };
                var originalContent = Content as UIElement;
                if (originalContent == null) return null;
                string contentXaml = XamlWriter.Save(originalContent);
                using (StringReader stringReader = new StringReader(contentXaml))
                {
                    using (XmlReader xmlReader = XmlReader.Create(stringReader))
                    {
                        object clonedContent = XamlReader.Load(xmlReader);

                        if (clonedContent is UIElement clonedElement)
                        {
                            return clonedElement;
                        }
                    }
                }
                return null;
            }
            catch (Exception)
            {
                throw;
            }
        }

    }


    #region 是否设计时模式
    public class DesignerHelper
    {
        private static bool? _isInDesignMode;

        public static bool IsInDesignMode
        {
            get
            {
                if (!_isInDesignMode.HasValue)
                {
                    _isInDesignMode = (bool)DependencyPropertyDescriptor.FromProperty(DesignerProperties.IsInDesignModeProperty,
                        typeof(FrameworkElement)).Metadata.DefaultValue;
                }
                return _isInDesignMode.Value;
            }
        }
    }
    #endregion
}