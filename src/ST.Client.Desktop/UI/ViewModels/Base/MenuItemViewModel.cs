﻿using ReactiveUI;
using System.Collections.Generic;
using System.Windows.Input;
using System.Application.UI.Resx;

namespace System.Application.UI.ViewModels
{
    public class MenuItemViewModel : ReactiveObject
    {
        private readonly string? name;
        public MenuItemViewModel(string? ResourceName = null)
        {
            name = ResourceName;
        }

        public virtual string? IconKey { get; set; }

        public string? Header => string.IsNullOrEmpty(name) ? "-" : AppResources.ResourceManager.GetString(name,AppResources.Culture);

        public ICommand? Command { get; set; }
        public object? CommandParameter { get; set; }

        private IList<MenuItemViewModel>? _Items;
        public IList<MenuItemViewModel>? Items
        {
            get => _Items;
            set => this.RaiseAndSetIfChanged(ref _Items, value);
        }
    }
}
