﻿// XAML Map Control - https://github.com/ClemensFischer/XAML-Map-Control
// Copyright © 2024 Clemens Fischer
// Licensed under the Microsoft Public License (Ms-PL)

#if WPF
using System.Windows;
using System.Windows.Data;
#elif UWP
using Windows.UI.Xaml;
using Windows.UI.Xaml.Data;
#elif WINUI
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;
#endif

namespace MapControl
{
    internal static class BindingHelper
    {
        public static Binding CreateBinding(this FrameworkElement source, string property)
        {
            return new Binding
            {
                Source = source,
                Path = new PropertyPath(property)
            };
        }
    }
}
