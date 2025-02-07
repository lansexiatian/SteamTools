﻿using ReactiveUI;
using System.Reactive.Linq;
using System.Reactive.Subjects;

namespace System.Application.Services
{
    /// <summary>
    /// 提供对显示在主窗口底部的状态栏的访问。
    /// </summary>
    public class ToastService : ReactiveObject
    {
        #region static members
        public static ToastService Current { get; } = new ToastService();
        #endregion

        private readonly Subject<string> notifier;
        private string persisitentMessage = "";
        private string notificationMessage = "";

        #region Message 变更通知

        /// <summary>
        /// 获取指示当前状态的字符串。
        /// </summary>
        public string Message
        {
            get { return this.notificationMessage ?? this.persisitentMessage; }
            set
            {
                this.notificationMessage = value;
                this.persisitentMessage = value;
                this.RaisePropertyChanged();
            }
        }

        #endregion

        /// <summary>
        /// 显示状态
        /// </summary>
        private bool _IsVisible;
        public bool IsVisible
        {
            get => _IsVisible;
            set => this.RaiseAndSetIfChanged(ref _IsVisible, value);
        }

        private ToastService()
        {
            this.notifier = new Subject<string>();
            this.notifier
                .Do(x =>
                {
                    this.notificationMessage = x;
                    this.RaiseMessagePropertyChanged();
                })
                .Throttle(TimeSpan.FromMilliseconds(5000))
                .Subscribe(_ =>
                {
                    this.notificationMessage = string.Empty;
                    this.RaiseMessagePropertyChanged();
                });

            this.WhenAnyValue(x => x.Message)
                     .Subscribe(x => IsVisible = !string.IsNullOrEmpty(x));
        }

        public void Set()
        {
            CloseBtn_Click();
        }
        public void Set(string message)
        {
            MainThreadDesktop.BeginInvokeOnMainThread(() => this.Message = message);
        }

        public void Notify(string message)
        {
            this.notifier.OnNext(message);
        }

        private void RaiseMessagePropertyChanged()
        {
            this.RaisePropertyChanged(nameof(this.Message));
        }

        public void CloseBtn_Click()
        {
            Set("");
            IsVisible = false;
        }
    }
}
